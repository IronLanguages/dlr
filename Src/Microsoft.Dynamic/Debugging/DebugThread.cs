// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Debugging {
    [DebuggerDisplay("ThreadId = {_threadId}")]
    public abstract class DebugThread {
        private readonly int _threadId;

        internal DebugThread(DebugContext debugContext) {
            DebugContext = debugContext;
            _threadId = ThreadingUtils.GetCurrentThreadId();
        }

        internal DebugContext DebugContext { get; }

        internal Exception ThrownException { get; set; }

        internal bool IsCurrentThread {
            get 
            {
                return _threadId == ThreadingUtils.GetCurrentThreadId(); 
            }
        }

        internal bool IsInTraceback { get; set; }

        #region Abstract Methods

        internal abstract IEnumerable<DebugFrame> Frames { get; }
        internal abstract DebugFrame GetLeafFrame();
        internal abstract bool TryGetLeafFrame(ref DebugFrame frame);
        internal abstract int FrameCount { get; }
        internal abstract void PushExistingFrame(DebugFrame frame);
        internal abstract bool PopFrame();
        internal abstract FunctionInfo GetLeafFrameFunctionInfo(out int stackDepth);

        #endregion
    }
}
