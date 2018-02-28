// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// TraceSession.
    /// 
    /// Basically holds a list of last encountered DebugFrame instances 
    /// (one per running thread).
    /// </summary>
    public sealed class TracePipeline : ITracePipeline, IDebugCallback {
        private readonly DebugContext _debugContext;
        private readonly ThreadLocal<DebugFrame> _traceFrame = new ThreadLocal<DebugFrame>();
        private ITraceCallback _traceCallback;
        private bool _closed;

        private TracePipeline(DebugContext debugContext) {
            _debugContext = debugContext;
            debugContext.DebugCallback = this;
            debugContext.DebugMode = DebugMode.FullyEnabled;
        }

        public static TracePipeline CreateInstance(DebugContext debugContext) {
            ContractUtils.RequiresNotNull(debugContext, nameof(debugContext));

            if (debugContext.DebugCallback != null)
                throw new InvalidOperationException(ErrorStrings.DebugContextAlreadyConnectedToTracePipeline);

            return new TracePipeline(debugContext);
        }

        #region ITraceDebugPipeline

        public void Close() {
            VerifyNotClosed();
            _debugContext.DebugCallback = null;
            _debugContext.DebugMode = DebugMode.Disabled;
            _closed = true;
        }

        public bool TrySetNextStatement(string sourceFile, SourceSpan sourceSpan) {
            VerifyNotClosed();
            ContractUtils.RequiresNotNull(sourceFile, nameof(sourceFile));
            ContractUtils.Requires(sourceSpan != SourceSpan.Invalid && sourceSpan != SourceSpan.None, ErrorStrings.InvalidSourceSpan);

            // Find the thread object.  We also check if the current thread is in FrameExit traceback.
            DebugFrame traceFrame = _traceFrame.Value;
            if (traceFrame == null) {
                return false;
            }

            int sequencePointIndex = GetSequencePointIndexForSourceSpan(sourceFile, sourceSpan, traceFrame);
            if (sequencePointIndex == Int32.MaxValue)
                return false;

            traceFrame.CurrentSequencePointIndex = sequencePointIndex;
            return true;
        }

        public ITraceCallback TraceCallback {
            get {
                VerifyNotClosed();
                return _traceCallback;
            }
            set {
                VerifyNotClosed();
                _traceCallback = value;
            }
        }

        #endregion

        #region IDebugCallback

        void IDebugCallback.OnDebugEvent(TraceEventKind kind, DebugThread thread, FunctionInfo functionInfo, int sequencePointIndex, int stackDepth, object payload) {
            ITraceCallback traceCallback = _traceCallback;

            if (traceCallback != null) {
                // $TODO: what if the callback throws an exception? should we swallow it?
                var curThread = _traceFrame.Value;
                try {
                    if (kind == TraceEventKind.FrameExit || kind == TraceEventKind.ThreadExit) {
                        traceCallback.OnTraceEvent(
                            kind,
                            kind == TraceEventKind.FrameExit ? functionInfo.Name : null,
                            null,
                            SourceSpan.None,
                            null,
                            payload,
                            functionInfo?.CustomPayload
                        );
                    } else {
                        DebugFrame leafFrame = thread.GetLeafFrame();
                        _traceFrame.Value = leafFrame;
                        Debug.Assert(sequencePointIndex >= 0 && sequencePointIndex < functionInfo.SequencePoints.Length);
                        DebugSourceSpan sourceSpan = functionInfo.SequencePoints[sequencePointIndex];
                        traceCallback.OnTraceEvent(
                            kind,
                            functionInfo.Name,
                            sourceSpan.SourceFile.Name,
                            sourceSpan.ToDlrSpan(),
                            () => { return leafFrame.GetLocalsScope(); },
                            payload,
                            functionInfo.CustomPayload
                        );
                    }
                } finally {
                    _traceFrame.Value = curThread;
                }
            }
        }

        #endregion

        private int GetSequencePointIndexForSourceSpan(string sourceFile, SourceSpan sourceSpan, DebugFrame frame) {
            DebugSourceFile debugSourceFile = _debugContext.Lookup(sourceFile);
            if (debugSourceFile == null) {
                return Int32.MaxValue;
            }

            DebugSourceSpan debugSourceSpan = new DebugSourceSpan(debugSourceFile, sourceSpan);
            FunctionInfo leafFrameFuncInfo = frame.FunctionInfo;
            FunctionInfo funcInfo = debugSourceFile.LookupFunctionInfo(debugSourceSpan);

            // Verify that funcInfo matches the current frame
            if (funcInfo != leafFrameFuncInfo) {
                return Int32.MaxValue;
            }

            // Get the target sequence point
            return debugSourceSpan.GetSequencePointIndex(funcInfo);
        }

        private void VerifyNotClosed() {
            if (_closed) {
                throw new InvalidOperationException(ErrorStrings.ITracePipelineClosed);
            }
        }
    }
}
