// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    ///   Runtime-async orchestrator for <see cref="Microsoft.Scripting.Ast.AsyncExpression"/>.
    /// </summary>
    public static class AsyncHelpers {
        /// <summary>
        ///  Drives a language function body, awaiting each yielded <see cref="Task"/> and feeding the result back through <paramref
        ///  name="valueSlot"/>. Faulted awaits are routed through <paramref name="exceptionSlot"/> so the body's try/except can observe
        ///  them at the resumption point — this is how <c>async for</c>'s <c>StopAsyncIteration</c> catch works.
        /// </summary>
        /// <param name="states">The body's state machine — an enumerator of yielded awaitables.</param>
        /// <param name="valueSlot">
        ///   While the body is running, this is where the driver writes each awaited result before resuming the body (read by the body at
        ///   the resume point of each <c>await</c>). After the body completes, this is where the body has stored its final return value,
        ///   which becomes the returned Task's result. The two uses do not overlap: the body has consumed (or discarded) any per-await
        ///   value before its final assignment runs.
        /// </param>
        /// <param name="exceptionSlot">
        ///   Faulted/cancelled await is parked here so the body's rewriter can rethrow it at the resume point via <see
        ///   cref="ExceptionDispatchInfo"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   Caller's cancellation token. Sampled at each loop iteration and linked to each
        ///   awaited task; see remarks for the cancellation model.
        /// </param>
        /// <param name="cancellationException">
        ///   Optional override for the exception surfaced when <paramref name="cancellationToken"/>
        ///   fires. If non-null and its <c>Value</c> is non-null at the moment cancellation is
        ///   observed, that exception is delivered into <paramref name="exceptionSlot"/> instead
        ///   of a fresh <see cref="OperationCanceledException"/>. It lets a host inject an arbitrary
        ///   exception (e.g. for Python's <c>coro.throw(exc)</c>) by setting the box's value and
        ///   then cancelling the token. Default null preserves the plain-cancellation behavior.
        /// </param>
        /// <remarks>
        ///   This method is itself an <c>async Task</c> whose IL is produced by Roslyn (on .NET 11+, under feature
        ///   <c>runtime-async=on</c>), so each <c>await</c> below becomes a real .NET 11+ runtime-async
        ///   opcode. The state machine of the language function body is delegated to a <c>GeneratorRewriter</c>-produced <see
        ///   cref="IEnumerator{T}"/> of yielded tasks. On older runtimes, the same method is compiled by Roslyn without runtime-async
        ///   support, so it compiles to a Roslyn-generated async state machine.
        ///
        ///   <para>Continuation scheduling is determined by the caller's <see cref="SynchronizationContext"/> / <see cref="TaskScheduler"/>
        ///   — the awaits here do not call <c>ConfigureAwait(false)</c>, so a host that installs a single-threaded context (e.g. an
        ///   asyncio-style event loop) pins every resumption to its loop thread.</para>
        ///
        ///   <para>Cancellation is cooperative and follows asyncio's model: <paramref name="cancellationToken"/> is sampled at each loop
        ///   iteration (so a stretch of synchronously-resolved awaits is still cancellable) and is linked to each suspended task so a
        ///   cancellation request unblocks an in-flight await even when the awaited task does not honor the token itself. In either case
        ///   the resulting <see cref="OperationCanceledException"/> is routed through <paramref name="exceptionSlot"/> so it surfaces at
        ///   the body's next resume point — exactly the place a body-level <c>try/except</c> around the <c>await</c> expects to observe it.
        ///   If the body lets the exception propagate, it bubbles out of <see cref="DriveAsync"/> and the returned Task transitions to <see
        ///   cref="TaskStatus.Canceled"/> because the OCE's token matches.</para>
        /// 
        ///   <para>This method is not obsolete but is not part of the public API surface; do not call it directly from source-level code.
        ///   It is made public only for internal use by the DLR.</para>
        /// </remarks>
        [Obsolete("do not call this method directly from source-level code", error: true)]
        public static async Task<object?> DriveAsync(IEnumerator<object?> states,
                                                     StrongBox<object?> valueSlot,
                                                     StrongBox<Exception?> exceptionSlot,
                                                     CancellationToken cancellationToken ,
                                                     StrongBox<Exception?>? cancellationException = null) {

            while (states.MoveNext()) {
                object? yielded = states.Current;

                // Surface cancellation at the just-yielded suspension point so the body's try/except around
                // `await` can observe it (matches asyncio's "CancelledError raised at the await" model).
                // Caught here as well as in the WaitAsync branch so that a stretch of synchronously-resolved
                // yields is still cancellable. If the host pre-populated cancellationException, deliver that
                // instead of a fresh OCE — lets coro.throw(arbitrary) inject any exception type.
                if (cancellationToken.IsCancellationRequested) {
                    valueSlot.Value = null;
                    exceptionSlot.Value = cancellationException?.Value
                                          ?? new OperationCanceledException(cancellationToken);
                    continue;
                }

                if (yielded is Task task) {
                    try {
                        // Task.WaitAsync is BCL on net6+, polyfilled by Meziantou.Polyfill on net4x/netstandard2.0.
                        // No ConfigureAwait(false): honor caller's SyncContext / TaskScheduler.
                        await task.WaitAsync(cancellationToken);
                        valueSlot.Value = ExtractTaskResult(task);
                        exceptionSlot.Value = null;
                    } catch (Exception ex) {
                        // The body's rewriter inserts a check after each yield that rethrows this on the resumed thread,
                        // so a try/except inside `await` can catch it (including the cancellation OCE from WaitAsync).
                        valueSlot.Value = null;
                        exceptionSlot.Value = ex;
                    }
                } else {
                    // Synchronously-produced value forwards straight through.
                    valueSlot.Value = yielded;
                    exceptionSlot.Value = null;
                }
            }
            // Body has completed: its final assignment has just written into valueSlot
            // (see AsyncExpression.BuildReduction), so the per-await role of the slot
            // is over and the same slot now carries the function's return value.
            return valueSlot.Value;
        }


        private static object? ExtractTaskResult(Task task) {
            // Fast-track for the most common case (e.g. IronPython): all awaitables are normalized to Task<object?>.
            if (task is Task<object?> to) return to.Result;

            Type t = task.GetType();

            // Non-generic Task subclass: no Result property exists. Covers Task.CompletedTask, Task.Delay's DelayPromise,
            // Task.WhenAll's non-generic overload, etc. IsGenericType is a flag check on Type — far cheaper than GetProperty.
            if (!t.IsGenericType) return null;

            // The runtime type may be a Task<TResult> subclass (e.g. AsyncStateMachineBox<TStateMachine,TResult>, or RuntimeAsyncTask<T>);
            // so find Task<TResult>.Result through inheritance hierarchy.
            // This may be incorrect in the unlikely (and bad) case if the subclass shadows Result (e.g. new T2 Result {...} - not in BCL/CLR)
            var prop = t.GetProperty("Result");

            // Non-generic Task subclass that still wasn't caught above (defensive — shouldn't happen given IsGenericType).
            if (prop is null) return null;

            // Roslyn-emitted or CLR async Task uses an internal VoidTaskResult type argument; surface that as null.
            if (!prop.PropertyType.IsVisible) return null;

            // Task<TResult>.Result, may be null, which is OK (and is null if task has not completed yet; not happening here)
            return prop.GetValue(task);
        }
    }
}
