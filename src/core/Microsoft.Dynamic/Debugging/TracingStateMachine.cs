// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    ///   Wraps the state machine of an async body compiled for tracing, and dispatches the body's trace events.
    /// </summary>
    /// <remarks>
    ///   It sits between the body's state machine and its driver (<see cref="AsyncHelpers.DriveAsync"/> or
    ///   <c>AsyncHelpers.DriveAsyncEnumerable</c>), and is inserted only when the body is compiled for tracing
    ///   (see <see cref="Microsoft.Scripting.Ast.AsyncDebugSupport"/>), so the drivers know nothing about tracing. It consumes the
    ///   <see cref="DebugPoint"/>s that the instrumented body yields at its sequence points and passes everything else through.
    ///
    ///   <para>The body's frame lives exactly as long as the body runs, as for a generator in CPython: one <see cref="MoveNext"/> is one
    ///   resumption, so the frame is pushed and <c>call</c> fired when the body resumes, and <c>return</c> fired and the frame popped
    ///   when the body suspends, completes, or faults. A suspended body is therefore never on a thread's frame stack, whatever runs in
    ///   the meantime and whichever thread the body resumes on. The one exception is a suspension that the driver resumes synchronously
    ///   (an await of an already completed task): then the frame stays open, since no other code runs in between and CPython does not
    ///   suspend there either.</para>
    ///
    ///   <para>An exception raised by the trace callback when the body is about to resume is not thrown out of the driver. It is handed
    ///   to the body through the exception slot, the same way the driver delivers a faulted await, so it surfaces inside the body, where
    ///   the body's own <c>try</c> statements see it.</para>
    ///
    ///   <para>The frame reports the body's location and its variables, which can be changed through it, 
    ///   but its location cannot be changed.</para>
    /// </remarks>
    internal sealed class TracingStateMachine : IEnumerator<object?> {
        // The entry sequence point is the location of the 'call' event, so it fires no 'line' event of its own (as in DebuggableLambdaBuilder).
        private const int EntryCookie = 0;

        private readonly IEnumerator<object?> _states;
        private readonly DebugContext _debugContext;
        private readonly FrameRuntimeVariables _variables;
        private readonly StrongBox<object?>? _resultSlot;
        private readonly StrongBox<Exception?> _exceptionSlot;
        private readonly bool _isAsyncEnumerable;

        // The thread the frame is open on; null while the body is suspended.
        private DefaultDebugThread? _thread;

        /// <param name="states">The state machine of the instrumented body.</param>
        /// <param name="debugContext">The debug context the trace events are dispatched through.</param>
        /// <param name="functionInfo">
        ///   The body's sequence points, indexed by the cookies of its <see cref="DebugPoint"/>s, and its variables, indexed like
        ///   <paramref name="variables"/>.
        /// </param>
        /// <param name="resultSlot">Where the body leaves its return value on completion, or null if it has none (an async generator).</param>
        /// <param name="exceptionSlot">The slot the body reads at each resume point, and rethrows from if it is set.</param>
        /// <param name="isAsyncEnumerable">
        ///   Whether the driver is <c>AsyncHelpers.DriveAsyncEnumerable</c>, which hands every item but an <see cref="AwaitPoint"/> to the
        ///   consumer, rather than <see cref="AsyncHelpers.DriveAsync"/>, which awaits every item.
        /// </param>
        /// <param name="variables">The storage of the body's variables, which the frame exposes.</param>
        internal TracingStateMachine(IEnumerator<object?> states, DebugContext debugContext, FunctionInfo functionInfo,
                                     StrongBox<object?>? resultSlot, StrongBox<Exception?> exceptionSlot, bool isAsyncEnumerable,
                                     IRuntimeVariables variables) {
            _states = states;
            _debugContext = debugContext;
            _variables = new FrameRuntimeVariables(functionInfo, variables);
            _resultSlot = resultSlot;
            _exceptionSlot = exceptionSlot;
            _isAsyncEnumerable = isAsyncEnumerable;
        }


        public object? Current => _states.Current;

        object? IEnumerator.Current => Current;


        public bool MoveNext() {
            if (_thread is null) {
                Open();
            }

            while (true) {
                bool moved;
                try {
                    moved = _states.MoveNext();
                } catch (Exception ex) {
                    // The 'exception' event proper has already been fired by the body's handlers (see OnExceptionCaught);
                    // like a synchronous debuggable body, the frame only reports here that it is being unwound.
                    try {
                        Dispatch(TraceEventKind.ExceptionUnwind, ex);
                    } finally {
                        Close(returnValue: null);
                    }
                    throw;
                }

                if (!moved) {
                    Close(_resultSlot?.Value);
                    return false;
                }

                object? item = _states.Current;
                if (item is not DebugPoint debugPoint) {
                    if (!ResumesSynchronously(item)) {
                        Close(item is AwaitPoint awaitPoint ? awaitPoint.Task : item);
                    }
                    return true;
                }

                _variables.Marker = debugPoint.Cookie;
                if (debugPoint.Cookie != EntryCookie) {
                    // This resumes the body past a debug point, so, like the driver resuming it past an await, it always writes the slot.
                    _exceptionSlot.Value = TryDispatch(TraceEventKind.TracePoint);
                }
            }
        }


        public void Reset() {
            throw new NotSupportedException();
        }


        public void Dispose() {
            _states.Dispose();
        }


        /// <summary>
        ///   Fires the <c>exception</c> event for an exception the body is about to handle.
        /// </summary>
        /// <remarks>
        ///   Called by the instrumented body on entering each exception handler, the way a synchronous debuggable body reports
        ///   first-chance exceptions. An IronPython function body handles every exception at least once (to update its traceback), so this
        ///   also reports an exception that eventually propagates out of the body.
        /// </remarks>
        internal void OnExceptionCaught(Exception exception) {
            Debug.Assert(_thread is not null, "the body only runs while its frame is open");
            Dispatch(TraceEventKind.Exception, exception);
        }


        private void Open() {
            _thread = (DefaultDebugThread)_debugContext.GetCurrentThread();
            _thread.LiftVariables(_variables);

            // At the very first resumption the marker is the entry point, and the body yields it before reading the slot;
            // at later resumptions the body is resuming past a suspension and reads the slot immediately.
            if (TryDispatch(TraceEventKind.FrameEnter) is { } exception) {
                _exceptionSlot.Value = exception;
            }
        }


        private void Close(object? returnValue) {
            try {
                Dispatch(TraceEventKind.FrameExit, returnValue);
            } finally {
                _thread!.PopFrame();
                _thread = null;
            }
        }


        /// <summary>
        ///   Whether the driver resumes the body right away after <paramref name="item"/>, with no other code running in between.
        /// </summary>
        private bool ResumesSynchronously(object? item) {
            return _isAsyncEnumerable
                ? item is AwaitPoint { Task.IsCompleted: true }    // anything else is a value handed over to the consumer
                : item is not Task { IsCompleted: false };         // DriveAsync forwards a non-Task value straight back
        }


        private void Dispatch(TraceEventKind kind, object? payload = null) {
            _debugContext.DispatchDebugEvent(_thread!, _variables.Marker, kind, payload);
        }


        private Exception? TryDispatch(TraceEventKind kind) {
            try {
                Dispatch(kind);
                return null;
            } catch (Exception ex) {
                return ex;
            }
        }


        /// <summary>
        ///   The runtime variables of the body's frame: the body's <see cref="FunctionInfo"/>, its current sequence point, and its variables.
        /// </summary>
        /// <remarks>
        ///   Slot 0 is the <see cref="FunctionInfo"/> and slot 1 the marker, followed by the body's variables, which is the layout
        ///   <see cref="DebugRuntimeVariables"/> expects (the same one a synchronous debuggable body lifts its locals in).
        /// </remarks>
        private sealed class FrameRuntimeVariables : IRuntimeVariables {
            private readonly FunctionInfo _functionInfo;
            private readonly IRuntimeVariables _variables;

            internal FrameRuntimeVariables(FunctionInfo functionInfo, IRuntimeVariables variables) {
                _functionInfo = functionInfo;
                _variables = variables;
            }

            internal int Marker { get; set; }

            public int Count => 2 + _variables.Count;

            public object? this[int index] {
                get => index switch {
                    0 => _functionInfo,
                    1 => Marker,
                    _ => _variables[index - 2],
                };
                set {
                    if (index == 1) {
                        Marker = (int)value!;
                    } else if (index > 1) {
                        _variables[index - 2] = value;
                    }
                }
            }
        }
    }


    /// <summary>
    ///   Marker that an async body compiled for tracing yields at each of its sequence points.
    /// </summary>
    /// <remarks>
    ///   It is consumed by <see cref="TracingStateMachine"/>, so the driver never sees it. <see cref="Cookie"/> indexes the sequence points
    ///   of the body's <see cref="FunctionInfo"/>.
    /// </remarks>
    internal sealed class DebugPoint(int cookie) {
        public int Cookie { get; } = cookie;
    }
}
