// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Debugging {
    public enum TraceEventKind {
        // Fired when the execution enters a new frame
        //
        // Payload:
        //   none
        FrameEnter,

        // Fired when the execution leaves a frame
        //
        // Payload:
        //   return value from the function
        FrameExit,

        // Fired when the execution leaves a frame
        //
        // Payload:
        //   none
        ThreadExit,

        // Fired when the execution encounters a trace point
        //
        // Payload:
        //   none
        TracePoint,

        // Fired when an exception is thrown during the execution
        // 
        // Payload:
        //   the exception object that was thrown
        Exception,

        // Fired when an exception is thrown and is not handled by 
        // the current method.
        //
        // Payload:
        //   the exception object that was thrown
        ExceptionUnwind,
    }
}
