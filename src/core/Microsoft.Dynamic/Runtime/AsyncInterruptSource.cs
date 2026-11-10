// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    ///   Raises exceptions inside a running async body, that of an <see cref="Ast.AsyncExpression"/> or an
    ///   <see cref="Ast.AsyncEnumerableExpression" />, at its awaits.
    /// </summary>
    /// <remarks>
    ///   <para>A source serves one body: the expression that supplies it must create a new one each time the node is evaluated.</para>
    ///
    ///   <para>An exception passed to <see cref="Interrupt"/> is raised inside the body at the await the body is suspended at,
    ///   or at its next await if the body is running. The body may catch it and continue, and be interrupted again later.
    ///   Unlike a cancelled <see cref="CancellationToken"/>, the source doesn't stay permanently interrupted.</para>
    ///
    ///   <para>An accepted interrupt is always observed: if it is pending before the body starts, the body doesn't run and its task
    ///   fails with the exception; if the body returns before reaching an await, its task fails with the exception instead of returning
    ///   the result. If the body raises an exception of its own while an interrupt is pending, the body's exception wins.</para>
    /// </remarks>
    public sealed class AsyncInterruptSource {
        private static readonly CancellationToken s_cancelled = new CancellationToken(true);

        private readonly Lock _lock = new();
        private Exception? _pending;
        private CancellationTokenSource? _cts;              // wakes the driver at an await; never reused once an interrupt is taken
        private CancellationToken _cancellationToken;       // the body's own token
        private CancellationTokenRegistration _registration;
        private bool _started;
        private bool _closed;

        /// <summary>
        ///   Requests that <paramref name="exception"/> be raised inside the body.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the request is accepted. If an interrupt is already pending, the request is merged into it:
        ///   the pending exception is kept and the new one is discarded.
        ///   <see langword="false"/> if the interrupt can't be delivered at this moment, for example because the body has finished;
        ///   the exception won't be raised, and the caller decides what to do instead.
        /// </returns>
        public bool Interrupt(Exception exception) {
            ContractUtils.RequiresNotNull(exception, nameof(exception));

            CancellationTokenSource? cts;
            lock (_lock) {
                if (_closed) return false;
                if (_pending is not null) return true;
                _pending = exception;
                cts = _cts;
            }
            // Outside the lock: Cancel may run the driver's continuation inline, on this thread.
            cts?.Cancel();
            return true;
        }

        /// <summary>
        ///   Called by the driver before the body runs. Returns the interrupt already pending, if any; the source is then closed, and the
        ///   body must not run.
        /// </summary>
        internal Exception? Start(CancellationToken cancellationToken) {
            lock (_lock) {
                if (_started) throw new InvalidOperationException("An AsyncInterruptSource can serve only one async body.");
                _started = true;
                if (_pending is not null) {
                    _closed = true;
                    return TakePendingLocked();
                }
                _cancellationToken = cancellationToken;
            }
            if (cancellationToken.CanBeCanceled) {
                _registration = cancellationToken.Register(static source => ((AsyncInterruptSource)source!).OnCancelled(), this);
            }
            return null;
        }

        private void OnCancelled() {
            CancellationTokenSource? cts;
            lock (_lock) {
                cts = _cts;
            }
            cts?.Cancel();
        }

        /// <summary>
        ///   Returns the token for the driver to wait with at an await of a task that hasn't completed. It is cancelled by an interrupt
        ///   or by the body's own token, and is already cancelled if either has happened.
        /// </summary>
        internal CancellationToken GetToken() {
            lock (_lock) {
                if (_pending is not null || _cancellationToken.IsCancellationRequested) return s_cancelled;
                return (_cts ??= new CancellationTokenSource()).Token;
            }
        }

        /// <summary>
        ///   Takes the pending interrupt, if any, for the driver to raise in the body.
        /// </summary>
        internal Exception? TakePending() {
            if (Volatile.Read(ref _pending) is null) return null;
            lock (_lock) {
                return TakePendingLocked();
            }
        }

        private Exception? TakePendingLocked() {
            Exception? pending = _pending;
            if (pending is not null) {
                _pending = null;
                // Interrupt may still be about to cancel the current token source, outside its lock, so the driver never waits on it again.
                _cts = null;
            }
            return pending;
        }

        /// <summary>
        ///   Called by the driver when the body has finished, or has been abandoned. Returns the interrupt still pending, if any.
        ///   From then on, <see cref="Interrupt"/> returns <see langword="false"/>.
        /// </summary>
        internal Exception? Close() {
            Exception? pending;
            lock (_lock) {
                if (_closed) return null;
                _closed = true;
                pending = TakePendingLocked();
                _cts = null;
            }
            _registration.Dispose();
            return pending;
        }
    }
}
