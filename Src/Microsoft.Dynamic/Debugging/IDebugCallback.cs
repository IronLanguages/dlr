// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Debugging {
    internal interface IDebugCallback {
        /// <summary>
        /// Callback that is fired by the traceback engine
        /// </summary>
        void OnDebugEvent(
            TraceEventKind kind, 
            DebugThread thread, 
            FunctionInfo functionInfo, 
            int sequencePointIndex,
            int stackDepth, 
            object payload);
    }
}
