// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// <param name="interrupts">
        ///   Optional source of exceptions to raise inside the body at its awaits, any number of times; see remarks and <see
        ///   cref="AsyncInterruptSource"/>.
        /// </param>
        /// <param name="observer">
        ///   Optional observer, notified as the body starts, suspends, resumes and returns, and of its outcome; see <see
        ///   cref="AsyncBodyEvent"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   Caller's cancellation token. Sampled at each loop iteration and linked to each
        ///   awaited task; see remarks for the cancellation model.
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
        ///   <para>An exception from <paramref name="interrupts"/> is routed the same way, at the await the body is suspended at, or at
        ///   its next await if it is running. Unlike cancellation, it is delivered once, so the body can handle it and continue. If it is
        ///   pending before the body starts, the body doesn't run and the returned Task fails with it; if the body returns while it is
        ///   pending, the Task fails with it instead of returning the result. If it wins over an exception the awaited task raised, that
        ///   exception is passed to <paramref name="observer"/> with <see cref="AsyncBodyEventKind.Resumed"/>.</para>
        ///
        ///   <para>This method is not obsolete but is not part of the public API surface; do not call it directly from source-level code.
        ///   It is made public only for internal use by the DLR.</para>
        /// </remarks>
        [Obsolete("do not call this method directly from source-level code", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<object?> DriveAsync(IEnumerator<object?> states,
                                                     StrongBox<object?> valueSlot,
                                                     StrongBox<Exception?> exceptionSlot,
                                                     AsyncInterruptSource? interrupts,
                                                     IObserver<AsyncBodyEvent>? observer,
                                                     CancellationToken cancellationToken) {

            if (interrupts?.Start(cancellationToken) is Exception early) {
                observer?.OnError(early);
                ExceptionDispatchInfo.Capture(early).Throw();
            }

            try {
                observer?.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Started, null));
                while (true) {
                    try {
                        if (!states.MoveNext()) break;
                    } catch (Exception ex) when (observer is not null) {
                        // The body has raised: the task fails with the exception.
                        interrupts?.Close();
                        observer.OnError(ex);
                        throw;
                    }
                    object? item = states.Current;

                    // Surface an interrupt or cancellation at the just-yielded suspension point so the body's try/except around
                    // `await` can observe it.
                    // Checked here as well as in the WaitAsync branch so that a stretch of synchronously-resolved yields
                    // can still be interrupted. An interrupt is delivered once; cancellation stays.
                    if (interrupts?.TakePending() is Exception interrupt) {
                        valueSlot.Value = null;
                        exceptionSlot.Value = interrupt;
                        continue;
                    }
                    if (cancellationToken.IsCancellationRequested) {
                        valueSlot.Value = null;
                        exceptionSlot.Value = new OperationCanceledException(cancellationToken);
                        continue;
                    }

                    if (item is Task task) {
                        // The interrupt source's token is cancelled by an interrupt as well as by cancellationToken.
                        bool interruptible = interrupts is not null && !task.IsCompleted;
                        CancellationToken token = interruptible ? interrupts!.GetToken() : cancellationToken;
                        // Task.WaitAsync is BCL on net6+, polyfilled by Meziantou.Polyfill on net4x/netstandard2.0.
                        Task waiting = task.WaitAsync(token);

                        // Only an await of a task that hasn't completed gives up the thread. If the task completes before the await,
                        // the await continues on this thread, so the observer still gets a matching pair.
                        bool suspends = observer is not null && !waiting.IsCompleted;
                        if (suspends) observer!.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Awaiting, task));
                        try {
                            // No ConfigureAwait(false): honour caller's SyncContext / TaskScheduler.
                            await waiting;
                            valueSlot.Value = ExtractTaskResult(task);
                            exceptionSlot.Value = null;
                        } catch (Exception ex) {
                            // The body's rewriter inserts a check after each yield that rethrows this on the resumed thread,
                            // so a try/except inside `await` can catch it (including the cancellation OCE from WaitAsync).
                            valueSlot.Value = null;
                            exceptionSlot.Value = ex;
                        }
                        Exception? superseded = interruptible
                            ? ResolveInterruptibleAwait(valueSlot, exceptionSlot, interrupts!, token, cancellationToken)
                            : null;
                        if (suspends) observer!.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Resumed, superseded));
                    } else {
                        // Synchronously-produced value forwards straight through.
                        valueSlot.Value = item;
                        exceptionSlot.Value = null;
                    }
                }

                // Body has completed: its final assignment has just written into valueSlot
                // (see AsyncExpression.BuildReduction), so the per-await role of the slot
                // is over and the same slot now carries the function's return value.
                observer?.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Returning, valueSlot.Value));

                // An interrupt the body hasn't observed fails the task instead.
                if (interrupts?.Close() is Exception late) {
                    observer?.OnError(late);
                    ExceptionDispatchInfo.Capture(late).Throw();
                }
                observer?.OnCompleted();
            } finally {
                interrupts?.Close();
            }

            return valueSlot.Value;
        }


        /// <summary>
        ///   Settles what the body observes after an await that waited on the token of <paramref name="interrupts"/>.
        ///   Returns the exception the awaited task raised if an interrupt wins over it, otherwise <see langword="null"/>.
        /// </summary>
        private static Exception? ResolveInterruptibleAwait(StrongBox<object?> valueSlot, StrongBox<Exception?> exceptionSlot,
                                                            AsyncInterruptSource interrupts, CancellationToken token, CancellationToken cancellationToken) {
            // Whether the token, and not the awaited task, ended the wait.
            // (The WaitAsync polyfill on .NET Framework doesn't put the token it was given into the exception, so it can't be compared.)
            bool waitCancelled = exceptionSlot.Value is OperationCanceledException && token.IsCancellationRequested;

            if (interrupts.TakePending() is Exception interrupt) {
                // A pending interrupt wins over the awaited result or exception.
                Exception? superseded = waitCancelled ? null : exceptionSlot.Value;
                valueSlot.Value = null;
                exceptionSlot.Value = interrupt;
                return superseded;
            }
            if (waitCancelled) {
                // With no interrupt pending, only cancellationToken cancels the interrupt source's token: report the caller's token.
                exceptionSlot.Value = new OperationCanceledException(cancellationToken);
            }
            return null;
        }


#if NET
        /// <summary>
        ///   Drives a language async-generator body, producing an <see cref="IAsyncEnumerable{T}"/> of object.
        ///   The async-enumerable version of <see cref="DriveAsync"/>.
        /// </summary>
        /// <remarks>
        ///   The body's state machine (an <see cref="IEnumerator{T}"/> from <c>GeneratorRewriter</c>) yields two
        ///   kinds of items, discriminated by type:
        ///   <list type="bullet">
        ///     <item><see cref="AwaitPoint"/> — an internal suspension point. The driver awaits its Task and feeds
        ///     the result back through <paramref name="valueSlot"/> (faults/cancellation through
        ///     <paramref name="exceptionSlot"/>), exactly like <see cref="DriveAsync"/>. Not emitted to the consumer.</item>
        ///
        ///     <item>anything else — a value produced by a language-level <c>yield</c>; emitted to the consumer via
        ///     <c>yield return</c>. A yielded Task is NOT an <see cref="AwaitPoint"/>, so it is surfaced as a value,
        ///     never awaited — this is the disambiguation that lets <c>await</c> and <c>yield</c> coexist.</item>
        ///   </list>
        ///
        ///   <para>Interrupts from <paramref name="interrupts"/> and cancellation are raised at awaits only, as in <see cref="DriveAsync"/>:
        ///   the body doesn't observe the exception slot after a language-level <c>yield</c>. An interrupt that arrives while the consumer
        ///   holds the body at a <c>yield</c> stays pending until the body's next await, or fails the enumeration if the body ends first.
        ///   The body starts at the first <c>MoveNextAsync</c>.</para>
        ///
        ///   <para><paramref name="observer"/> is notified as in <see cref="DriveAsync"/>, and also around each value handed to the
        ///   consumer, since the consumer decides when, and on which thread, the body continues. If the consumer abandons the body at a
        ///   <c>yield</c>, by disposing the enumerator, the observer's last call is <see cref="IObserver{T}.OnCompleted"/>.</para>
        ///
        ///   <para>This method is not obsolete but is not part of the public API surface; do not call it directly from source-level code.
        ///   It is made public only for internal use by the DLR.</para>
        /// </remarks>
        [Obsolete("do not call this method directly from source-level code", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async IAsyncEnumerable<object?> DriveAsyncEnumerable(IEnumerator<object?> states,
                                                                           StrongBox<object?> valueSlot,
                                                                           StrongBox<Exception?> exceptionSlot,
                                                                           AsyncInterruptSource? interrupts,
                                                                           IObserver<AsyncBodyEvent>? observer,
                                                                           [EnumeratorCancellation] CancellationToken cancellationToken) {

            if (interrupts?.Start(cancellationToken) is Exception early) {
                observer?.OnError(early);
                ExceptionDispatchInfo.Capture(early).Throw();
            }

            // Whether the consumer holds the body at a `yield`, where it can abandon it by disposing the enumerator.
            bool yielded = false;
            try {
                observer?.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Started, null));
                while (true) {
                    try {
                        if (!states.MoveNext()) break;
                    } catch (Exception ex) when (observer is not null) {
                        // The body has raised: the enumeration throws the exception.
                        interrupts?.Close();
                        observer.OnError(ex);
                        throw;
                    }
                    object? item = states.Current;

                    if (item is AwaitPoint awaitPoint) {
                        if (interrupts?.TakePending() is Exception interrupt) {
                            valueSlot.Value = null;
                            exceptionSlot.Value = interrupt;
                            continue;
                        }
                        if (cancellationToken.IsCancellationRequested) {
                            valueSlot.Value = null;
                            exceptionSlot.Value = new OperationCanceledException(cancellationToken);
                            continue;
                        }

                        // Suspension point — await internally, do not surface to the consumer.
                        Task task = awaitPoint.Task;
                        bool interruptible = interrupts is not null && !task.IsCompleted;
                        CancellationToken token = interruptible ? interrupts!.GetToken() : cancellationToken;
                        Task waiting = task.WaitAsync(token);
                        bool suspends = observer is not null && !waiting.IsCompleted;
                        if (suspends) observer!.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Awaiting, task));
                        try {
                            await waiting;
                            valueSlot.Value = ExtractTaskResult(task);
                            exceptionSlot.Value = null;
                        } catch (Exception ex) {
                            valueSlot.Value = null;
                            exceptionSlot.Value = ex;
                        }
                        Exception? superseded = interruptible
                            ? ResolveInterruptibleAwait(valueSlot, exceptionSlot, interrupts!, token, cancellationToken)
                            : null;
                        if (suspends) observer!.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Resumed, superseded));
                    } else {
                        // Value from a language-level `yield` — emit it.
                        observer?.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Yielding, item));
                        yielded = true;
                        yield return item;
                        yielded = false;
                        observer?.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Resumed, null));

                        // No value flows back in for a plain `async for`;
                        // Clear the slots so the body's resume of the yield expression observes null.
                        valueSlot.Value = null;
                        exceptionSlot.Value = null;
                    }
                }

                // The body has ended. An interrupt it hasn't observed fails the enumeration.
                observer?.OnNext(new AsyncBodyEvent(AsyncBodyEventKind.Returning, null));
                if (interrupts?.Close() is Exception late) {
                    observer?.OnError(late);
                    ExceptionDispatchInfo.Capture(late).Throw();
                }
                observer?.OnCompleted();
            } finally {
                // Also reached when the consumer disposes the enumerator early.
                interrupts?.Close();
                if (yielded) observer?.OnCompleted();
            }
        }
#endif


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


    /// <summary>
    ///   Marker wrapping a <see cref="Task"/> to flag an await-suspension point inside an async-generator body,
    ///   distinguishing it from a value produced by a language-level <c>yield</c>. Emitted by
    ///   <see cref="Microsoft.Scripting.Ast.AsyncEnumerableExpression"/>'s await rewrite and consumed by
    ///   <see cref="AsyncHelpers.DriveAsyncEnumerable"/>.
    /// </summary>
    internal sealed class AwaitPoint(Task task) {
        public Task Task { get; } = task;
    }
}
