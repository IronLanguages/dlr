// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Debugging.CompilerServices;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// Default implementation of BaseDebugThread, which uses DLR's RuntimeVariablesExpression for lifting locals.
    /// </summary>
    internal sealed class DefaultDebugThread : DebugThread {
        private readonly List<FrameRuntimeVariablesPair> _frames;

        internal DefaultDebugThread(DebugContext debugContext)
            : base(debugContext) {
            _frames = new List<FrameRuntimeVariablesPair>();
        }

        #region Internal Members

        internal void LiftVariables(IRuntimeVariables runtimeVariables) {
            // Don't create the frame object right away.  Create it when it's actually needed for debugging.
            _frames.Add(new FrameRuntimeVariablesPair(new DebugRuntimeVariables(runtimeVariables), null));
        }

        #endregion

        #region BaseDebugThread overrides

        internal override IEnumerable<DebugFrame> Frames {
            get {
                for (int i = _frames.Count - 1; i >= 0; i--) {
                    yield return GetFrame(i);
                }
            }
        }

        internal override DebugFrame GetLeafFrame() {
            return GetFrame(_frames.Count - 1);
        }

        internal override bool TryGetLeafFrame(ref DebugFrame frame) {
            if (_frames.Count > 0) {
                frame = _frames[_frames.Count - 1].Frame;
                return frame != null;
            }

            frame = null;
            return false;
        }

        internal override int FrameCount {
            get { return _frames.Count; }
        }

        internal override void PushExistingFrame(DebugFrame frame) {
            _frames.Add(new FrameRuntimeVariablesPair(null, frame));
        }

        internal override bool PopFrame() {
            Debug.Assert(_frames.Count > 0);
            _frames.RemoveAt(_frames.Count - 1);
            return _frames.Count == 0;
        }

        internal override FunctionInfo GetLeafFrameFunctionInfo(out int stackDepth) {
            int leafIndex = _frames.Count - 1;
            if (leafIndex >= 0) {
                stackDepth = leafIndex;
                DebugFrame leafFrame = _frames[leafIndex].Frame;
                if (leafFrame != null) {
                    Debug.Assert(leafIndex == leafFrame.StackDepth);
                    return leafFrame.FunctionInfo;
                }

                Debug.Assert(_frames[leafIndex].RuntimeVariables is IDebugRuntimeVariables);
                return ((IDebugRuntimeVariables)_frames[leafIndex].RuntimeVariables).FunctionInfo;
            }

            stackDepth = Int32.MaxValue;
            return null;
        }

        #endregion

        private DebugFrame GetFrame(int index) {
            DebugFrame frame = null;
            if (index >= 0) {
                frame = _frames[index].Frame;
                if (frame == null) {
                    IDebugRuntimeVariables runtimeVariables = _frames[index].RuntimeVariables as IDebugRuntimeVariables;
                    Debug.Assert(runtimeVariables != null);
                    frame = new DebugFrame(this, runtimeVariables.FunctionInfo, runtimeVariables, index);
                    _frames[index] = new FrameRuntimeVariablesPair(null, frame);
                }
            }

            if (index == _frames.Count - 1) {
                frame.IsInTraceback = IsInTraceback;
                frame.ThrownException = ThrownException;
            }

            return frame;
        }

        private struct FrameRuntimeVariablesPair {
            public IRuntimeVariables RuntimeVariables;
            public DebugFrame Frame;

            public FrameRuntimeVariablesPair(IRuntimeVariables runtimeVariables, DebugFrame frame) {
                RuntimeVariables = runtimeVariables;
                Frame = frame;
            }
        }
    }
}
