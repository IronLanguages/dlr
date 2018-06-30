// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter {
    using InterpretedFrameThreadLocal = Microsoft.Scripting.Utils.ThreadLocal<InterpretedFrame>;

    public sealed class InterpretedFrame {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly InterpretedFrameThreadLocal CurrentFrame = new InterpretedFrameThreadLocal();

        internal readonly Interpreter Interpreter;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        private int[] _continuations;
        private int _continuationIndex;
        private int _pendingContinuation;
        private object _pendingValue;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public readonly object[] Data;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public readonly StrongBox<object>[] Closure;

        public int StackIndex;
        public int InstructionIndex;

        // When a ThreadAbortException is raised from interpreted code this is the first frame that caught it.
        // No handlers within this handler re-abort the current thread when left.
        public ExceptionHandler CurrentAbortHandler;

        internal InterpretedFrame(Interpreter interpreter, StrongBox<object>[] closure) {
            Interpreter = interpreter;
            StackIndex = interpreter.LocalCount;
            Data = new object[StackIndex + interpreter.Instructions.MaxStackDepth];

            int c = interpreter.Instructions.MaxContinuationDepth;
            if (c > 0) {
                _continuations = new int[c];
            }

            Closure = closure;
        }

        public DebugInfo GetDebugInfo(int instructionIndex) {
            return DebugInfo.GetMatchingDebugInfo(Interpreter._debugInfos, instructionIndex);
        }

        public string Name => Interpreter._name;

        #region Data Stack Operations

        public void Push(object value) {
            Data[StackIndex++] = value;
        }

        public void Push(bool value) {
            Data[StackIndex++] = value ? ScriptingRuntimeHelpers.True : ScriptingRuntimeHelpers.False;
        }

        public void Push(int value) {
            Data[StackIndex++] = ScriptingRuntimeHelpers.Int32ToObject(value);
        }

        public object Pop() {
            var val = Data[--StackIndex];
            Data[StackIndex] = null;
            return val;
        }

        public object Pop(int n) {
            StackIndex -= n;
            var val = Data[StackIndex];
            Array.Clear(Data, StackIndex, n);
            return val;
        }

        internal void SetStackDepth(int depth) {
            StackIndex = Interpreter.LocalCount + depth;
        }

        public object Peek() {
            return Data[StackIndex - 1];
        }

        public void Dup() {
            int i = StackIndex;
            Data[i] = Data[i - 1];
            StackIndex = i + 1;
        }

        #endregion

        #region Stack Trace

        public InterpretedFrame Parent { get; private set; }

        public static bool IsInterpretedFrame(MethodBase method) {
            ContractUtils.RequiresNotNull(method, nameof(method));
            return method.DeclaringType == typeof(Interpreter) && method.Name == "Run";
        }

#if FEATURE_STACK_TRACE
        /// <summary>
        /// A single interpreted frame might be represented by multiple subsequent Interpreter.Run CLR frames.
        /// This method filters out the duplicate CLR frames.
        /// </summary>
        public static IEnumerable<StackFrame> GroupStackFrames(IEnumerable<StackFrame> stackTrace) {
            bool inInterpretedFrame = false;
            foreach (StackFrame frame in stackTrace) {
                MethodBase method = frame.GetMethod();

                // mono sets the method to null for dynamic methods
                // IsInterpretedFrame doesn't allow null methods
                if(method == null) {
                    continue;
                }

                if (InterpretedFrame.IsInterpretedFrame(method)) {
                    if (inInterpretedFrame) {
                        continue;
                    }
                    inInterpretedFrame = true;
                } else {
                    inInterpretedFrame = false;
                }
                yield return frame;
            }
        }
#endif

        public IEnumerable<InterpretedFrameInfo> GetStackTraceDebugInfo() {
            var frame = this;
            do {
                yield return new InterpretedFrameInfo(frame.Name, frame.GetDebugInfo(frame.InstructionIndex));
                frame = frame.Parent;
            } while (frame != null);
        }

        internal void SaveTraceToException(Exception exception) {
            if (exception.GetData(typeof(InterpretedFrameInfo)) == null) {
                exception.SetData(typeof(InterpretedFrameInfo), new List<InterpretedFrameInfo>(GetStackTraceDebugInfo()).ToArray());
            }
        }

        public static InterpretedFrameInfo[] GetExceptionStackTrace(Exception exception) {
            return exception.GetData(typeof(InterpretedFrameInfo)) as InterpretedFrameInfo[];
        }

#if DEBUG
        internal string[] Trace {
            get {
                var trace = new List<string>();
                var frame = this;
                do {
                    trace.Add(frame.Name);
                    frame = frame.Parent;
                } while (frame != null);
                return trace.ToArray();
            }
        }
#endif

        internal InterpretedFrameThreadLocal.StorageInfo Enter() {
            var currentFrame = InterpretedFrame.CurrentFrame.GetStorageInfo();
            Parent = currentFrame.Value;
            currentFrame.Value = this;
            return currentFrame;
        }

        internal void Leave(InterpretedFrameThreadLocal.StorageInfo currentFrame) {
            currentFrame.Value = Parent;
        }

        #endregion

        #region Continuations

        public void RemoveContinuation() {
            _continuationIndex--;
        }

        public void PushContinuation(int continuation) {
            _continuations[_continuationIndex++] = continuation;
        }

        public int YieldToCurrentContinuation() {
            var target = Interpreter._labels[_continuations[_continuationIndex - 1]];
            SetStackDepth(target.StackDepth);
            return target.Index - InstructionIndex;
        }

        public int YieldToPendingContinuation() {
            Debug.Assert(_pendingContinuation >= 0);

            RuntimeLabel pendingTarget = Interpreter._labels[_pendingContinuation];

            // the current continuation might have higher priority (continuationIndex is the depth of the current continuation):
            if (pendingTarget.ContinuationStackDepth < _continuationIndex) {
                RuntimeLabel currentTarget = Interpreter._labels[_continuations[_continuationIndex - 1]];
                SetStackDepth(currentTarget.StackDepth);
                return currentTarget.Index - InstructionIndex;
            }

            SetStackDepth(pendingTarget.StackDepth);
            if (_pendingValue != Interpreter.NoValue) {
                Data[StackIndex - 1] = _pendingValue;
            }
            return pendingTarget.Index - InstructionIndex;
        }

        internal void PushPendingContinuation() {
            Push(_pendingContinuation);
            Push(_pendingValue);
#if DEBUG
            _pendingContinuation = -1;
#endif
        }

        internal void PopPendingContinuation() {
            _pendingValue = Pop();
            _pendingContinuation = (int)Pop();
        }

        private static MethodInfo _Goto;
        private static MethodInfo _VoidGoto;

        internal static MethodInfo GotoMethod => _Goto ?? (_Goto = typeof(InterpretedFrame).GetMethod("Goto"));

        internal static MethodInfo VoidGotoMethod => _VoidGoto ?? (_VoidGoto = typeof(InterpretedFrame).GetMethod("VoidGoto"));

        public int VoidGoto(int labelIndex) {
            return Goto(labelIndex, Interpreter.NoValue);
        }

        public int Goto(int labelIndex, object value) {
            // TODO: we know this at compile time (except for compiled loop):
            RuntimeLabel target = Interpreter._labels[labelIndex];
            if (_continuationIndex == target.ContinuationStackDepth) {
                SetStackDepth(target.StackDepth);
                if (value != Interpreter.NoValue) {
                    Data[StackIndex - 1] = value;
                }
                return target.Index - InstructionIndex;
            }

            // if we are in the middle of executing jump we forget the previous target and replace it by a new one:
            _pendingContinuation = labelIndex;
            _pendingValue = value;
            return YieldToCurrentContinuation();
        }

        #endregion
    }
}
