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
    ///   Runtime-async orchestrator for <see cref="Microsoft.Scripting.Ast.AsyncBodyExpression"/>.
    /// </summary>
    public static class AsyncRunner {
        /// <summary>
        /// Drives a language function body, awaiting each yielded <see cref="Task"/>
        /// and feeding the result back through <paramref name="resultSlot"/>.
        /// Faulted awaits are routed through <paramref name="exceptionSlot"/> so
        /// the body's try/except can observe them at the resumption point — this
        /// is how <c>async for</c>'s <c>StopAsyncIteration</c> catch works.
        /// </summary>
        /// <remarks>
        /// This method is itself an <c>async Task</c> whose IL is produced by
        /// Roslyn on .NET 11 under <c>&lt;Features&gt;runtime-async=on&lt;/Features&gt;</c>,
        /// so each <c>await</c> below becomes a real .NET 11 runtime-async opcode.
        /// The state machine of the language function body is delegated to a
        /// <c>GeneratorRewriter</c>-produced <see cref="IEnumerator{T}"/> of yielded
        /// tasks.
        ///
        /// <para>Continuation scheduling is determined by the caller's
        /// <see cref="SynchronizationContext"/> / <see cref="TaskScheduler"/> — the
        /// awaits here do not call <c>ConfigureAwait(false)</c>, so a host that
        /// installs a single-threaded context (e.g. an asyncio-style event loop)
        /// pins every resumption to its loop thread.</para>
        ///
        /// <para>Cancellation is cooperative. <paramref name="cancellationToken"/>
        /// is sampled at each loop iteration (so a stretch of synchronously-resolved
        /// awaits is still cancellable) and is linked to each suspended task so a
        /// cancellation request unblocks an in-flight await even when the awaited
        /// task does not honor the token itself. If cancellation fires during an
        /// <c>await</c>, the resulting <see cref="OperationCanceledException"/> is
        /// routed through <paramref name="exceptionSlot"/> exactly like any other
        /// awaited fault, so a body-level <c>try/except</c> around the await can
        /// observe it; otherwise it bubbles out and the returned Task transitions
        /// to <see cref="TaskStatus.Canceled"/> because the OCE's token matches.</para>
        /// </remarks>
        public static async Task<object?> Drive(
                IEnumerator<object> states,
                StrongBox<object?> resultSlot,
                StrongBox<object?> returnSlot,
                StrongBox<Exception?> exceptionSlot,
                CancellationToken cancellationToken = default) {
            while (states.MoveNext()) {
                // Sample between iterations: catches cancellation requested during a stretch of synchronously-resolved yields, 
                // and is the (uncatchable) interruption point Python try/except cannot guard.
                cancellationToken.ThrowIfCancellationRequested();

                object yielded = states.Current;
                if (yielded is Task task) {
                    try {
                        // Task.WaitAsync is BCL on net6+, polyfilled by Meziantou.Polyfill on net4x/netstandard2.0.
                        // No ConfigureAwait(false): honor caller's SyncContext / TaskScheduler.
                        await task.WaitAsync(cancellationToken);
                        resultSlot.Value = ExtractTaskResult(task);
                        exceptionSlot.Value = null;
                    } catch (Exception ex) {
                        // The body's rewriter inserts a check after each yield that rethrows this on the resumed thread,
                        // so a try/except inside `await` can catch it (including the cancellation OCE from WaitAsync).
                        resultSlot.Value = null;
                        exceptionSlot.Value = ex;
                    }
                } else {
                    // Synchronously-produced value forwards straight through.
                    resultSlot.Value = yielded;
                    exceptionSlot.Value = null;
                }
            }
            return returnSlot.Value;
        }

        private static object? ExtractTaskResult(Task task) {
            // The runtime type may be a Task<TResult> subclass (e.g. AsyncStateMachineBox<TStateMachine,TResult>, or RuntimeAsyncTask<T>);
            // so find Task<TResult>.Result through hierarchy
            var prop = task.GetType().GetProperty("Result");  // this may be incorrect in the unlikely (and bad) case if the subclass shadows Result (.e.g new T2 Result {...}, not in BCL/CLR)
            if (prop is null) return null;  // non-generic Task
            if (!prop.PropertyType.IsVisible) return null;  // Roslyn-emitted or CLR async Task uses an internal VoidTaskResult type argument; surface that as null.
            return prop.GetValue(task);  // Task<TResult>.Result, may be null (and is null if task has not completed yet)
        }
    }
}
