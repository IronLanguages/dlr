// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    /// <summary>
    /// A simple forth-style stack machine for executing Expression trees
    /// without the need to compile to IL and then invoke the JIT.  This trades
    /// off much faster compilation time for a slower execution performance.
    /// For code that is only run a small number of times this can be a 
    /// sweet spot.
    /// 
    /// The core loop in the interpreter is the RunInstructions method.
    /// </summary>
    internal sealed class Interpreter {
        internal static readonly object NoValue = new();
        internal const int RethrowOnReturn = Int32.MaxValue;

        // zero: sync compilation
        // negative: default
        internal readonly int _compilationThreshold;

        internal readonly object[] _objects;
        internal readonly RuntimeLabel[] _labels;

        internal readonly string _name;
        private readonly ExceptionHandler[] _handlers;
        internal readonly DebugInfo[] _debugInfos;

        internal Interpreter(string name, LocalVariables locals, HybridReferenceDictionary<LabelTarget, BranchLabel> labelMapping,
            InstructionArray instructions, ExceptionHandler[] handlers, DebugInfo[] debugInfos, int compilationThreshold) {

            _name = name;
            LocalCount = locals.LocalCount;
            ClosureVariables = locals.ClosureVariables;

            Instructions = instructions;
            _objects = instructions.Objects;
            _labels = instructions.Labels;
            LabelMapping = labelMapping;

            _handlers = handlers;
            _debugInfos = debugInfos;
            _compilationThreshold = compilationThreshold;
        }

        internal int ClosureSize {
            get {
                if (ClosureVariables is null) {
                    return 0;
                }
                return ClosureVariables.Count;
            }
        }

        internal int LocalCount { get; }

        internal bool CompileSynchronously => _compilationThreshold <= 1;

        internal InstructionArray Instructions { get; }

        internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; }

        internal HybridReferenceDictionary<LabelTarget, BranchLabel> LabelMapping { get; }

        /// <summary>
        /// Runs instructions within the given frame.
        /// </summary>
        /// <remarks>
        /// Interpreted stack frames are linked via Parent reference so that each CLR frame of this method corresponds 
        /// to an interpreted stack frame in the chain. It is therefore possible to combine CLR stack traces with 
        /// interpreted stack traces by aligning interpreted frames to the frames of this method.
        /// Each group of subsequent frames of Run method corresponds to a single interpreted frame.
        /// </remarks>
        [SpecialName, MethodImpl(MethodImplOptions.NoInlining)]
        public void Run(InterpretedFrame frame) {
            while (true) {
                try {
                    var instructions = Instructions.Instructions;
                    int index = frame.InstructionIndex;
                    while (index < instructions.Length) {
                        index += instructions[index].Run(frame);
                        frame.InstructionIndex = index;
                    }
                    break;
                } catch (Exception exception) {
                    switch (HandleException(frame, exception)) {
                        case ExceptionHandlingResult.Rethrow: throw;
                        case ExceptionHandlingResult.Continue: continue;
                        case ExceptionHandlingResult.Return: return;
                    }
                }
            }

            // An exception that has no handler in this frame is rethrown once the finally blocks on its way have run outside
            // of the CLR catch block. It is thrown outside of the try block so that the catch block above doesn't handle it again.
            if (frame.InstructionIndex == RethrowOnReturn && frame.PendingRethrow is { } pending) {
                frame.PendingRethrow = null;
                ExceptionDispatchInfo.Capture(pending).Throw();
            }
        }

        private ExceptionHandlingResult HandleException(InterpretedFrame frame, Exception exception, bool runHandlerInCatch = false) {
            frame.SaveTraceToException(exception);
            frame.InstructionIndex += GotoHandler(frame, exception, out ExceptionHandler handler);

#if FEATURE_THREAD
            // stay in the current catch so that ThreadAbortException is not rethrown by CLR:
            if (exception is ThreadAbortException abort) {
                if (handler is not null && !handler.IsFault) {
                    _anyAbortException = abort;
                    frame.CurrentAbortHandler = handler;
                }
                runHandlerInCatch = true;
            }
#endif
            if (runHandlerInCatch) {
                if (handler is null || handler.IsFault) {
                    // the exception is rethrown by the CLR catch block once the finally or fault blocks have run
                    frame.PendingRethrow = null;
                    return RunInCatch(frame);
                }
                return RunCatchHandlerInCatch(frame);
            }

            // Continue in Run, outside of the CLR catch block, with the handler or with the finally blocks to run before Run
            // rethrows the exception. Code running inside the catch block would leave it unfinished if the method went on
            // after a jump (e.g. break or continue) out of a handler or a finally block, or after a handler in this frame caught
            // an exception thrown by a finally block. The catch blocks of further exceptions would then nest on the stack.
            // Rethrowing from outside of the catch block is also not slower than rethrowing from within it.
            return ExceptionHandlingResult.Continue;
        }

        /// <summary>
        /// Runs finally or fault blocks within the current CLR catch block.
        /// </summary>
        private ExceptionHandlingResult RunInCatch(InterpretedFrame frame) {
            Run(frame);

            // a finally block can throw an exception caught by a handler, which cancels the previous exception:
            if (frame.InstructionIndex == RethrowOnReturn) {
                return ExceptionHandlingResult.Rethrow;
            }
            return ExceptionHandlingResult.Return;
        }

        /// <summary>
        /// Runs a catch handler within the current CLR catch block until the handler completes.
        /// </summary>
        /// <remarks>
        /// Handlers of exceptions thrown meanwhile also run within their CLR catch blocks, so that
        /// <see cref="LeaveExceptionHandlerInstruction"/> executed here always completes this handler.
        /// </remarks>
        private ExceptionHandlingResult RunCatchHandlerInCatch(InterpretedFrame frame) {
            while (true) {
                try {
                    var instructions = Instructions.Instructions;
                    int index = frame.InstructionIndex;

                    while (index < instructions.Length) {
                        var curInstr = instructions[index];

                        index += curInstr.Run(frame);
                        frame.InstructionIndex = index;
                        
                        if (curInstr is LeaveExceptionHandlerInstruction) {
                            // we've completed handling of this exception
                            return ExceptionHandlingResult.Continue;
                        }
                    }

                    if (frame.InstructionIndex == RethrowOnReturn) {
                        // rethrown by the CLR catch block rather than as a pending rethrow
                        frame.PendingRethrow = null;
                        return ExceptionHandlingResult.Rethrow;
                    }

                    return ExceptionHandlingResult.Return;
                } catch (Exception nestedException) {
                    switch (HandleException(frame, nestedException, runHandlerInCatch: true)) {
                        case ExceptionHandlingResult.Rethrow: throw;
                        case ExceptionHandlingResult.Continue: continue;
                        case ExceptionHandlingResult.Return: return ExceptionHandlingResult.Return;
                        default: throw Assert.Unreachable;
                    }
                }
            }
        }

        private enum ExceptionHandlingResult {
            Rethrow,
            Continue,
            Return
        }

#if FEATURE_THREAD
        // To get to the current AbortReason object on Thread.CurrentThread 
        // we need to use ExceptionState property of any ThreadAbortException instance.
        [ThreadStatic]
        private static ThreadAbortException _anyAbortException;

        internal static void AbortThreadIfRequested(InterpretedFrame frame, int targetLabelIndex) {
            var abortHandler = frame.CurrentAbortHandler;
            if (abortHandler is not null && !abortHandler.IsInside(frame.Interpreter._labels[targetLabelIndex].Index)) {
                frame.CurrentAbortHandler = null;

                var currentThread = Thread.CurrentThread;
                if ((currentThread.ThreadState & System.Threading.ThreadState.AbortRequested) != 0) {
                    Debug.Assert(_anyAbortException is not null);

#pragma warning disable SYSLIB0006 // Type or member is obsolete
#if FEATURE_EXCEPTION_STATE
                    // The current abort reason needs to be preserved.
                    currentThread.Abort(_anyAbortException.ExceptionState);
#else
                    currentThread.Abort();
#endif
#pragma warning restore SYSLIB0006 // Type or member is obsolete
                }
            }
        }
#else
        internal static void AbortThreadIfRequested(InterpretedFrame frame, int targetLabelIndex) {
            // nop
        }
#endif

        internal ExceptionHandler GetBestHandler(int instructionIndex, Type exceptionType) {
            ExceptionHandler best = null;
            foreach (var handler in _handlers) {
                if (handler.Matches(exceptionType, instructionIndex)) {
                    if (handler.IsBetterThan(best)) {
                        best = handler;
                    }
                }
            }
            return best;
        }

        internal int ReturnAndRethrowLabelIndex {
            get {
                // the last label is "return and rethrow" label:
                Debug.Assert(_labels[_labels.Length - 1].Index == RethrowOnReturn);
                return _labels.Length - 1;
            }
        }

        internal int GotoHandler(InterpretedFrame frame, Exception exception, out ExceptionHandler handler) {
            handler = GetBestHandler(frame.InstructionIndex, exception.GetType());
            if (handler is null) {
                // Run throws the exception when the frame reaches the label (after running finally blocks on the way):
                frame.PendingRethrow = exception;
                return frame.Goto(ReturnAndRethrowLabelIndex, NoValue);
            }

            return frame.Goto(handler.LabelIndex, exception);
        }
    }
}
