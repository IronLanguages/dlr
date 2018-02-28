// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Debugging.CompilerServices;

namespace Microsoft.Scripting.Debugging {
    public static class RuntimeOps {
        [Obsolete("do not call this method", true)]
        public static DebugFrame CreateFrameForGenerator(DebugContext debugContext, FunctionInfo func) {
            return debugContext.CreateFrameForGenerator(func);
        }

        [Obsolete("do not call this method", true)]
        public static bool PopFrame(DebugThread thread) {
            return thread.PopFrame();
        }

        [Obsolete("do not call this method", true)]
        public static void OnTraceEvent(DebugThread thread, int debugMarker, Exception exception) {
            thread.DebugContext.DispatchDebugEvent(thread, debugMarker, exception != null ? TraceEventKind.Exception : TraceEventKind.TracePoint, exception);
        }

        [Obsolete("do not call this method", true)]
        public static void OnTraceEventUnwind(DebugThread thread, int debugMarker, Exception exception) {
            thread.DebugContext.DispatchDebugEvent(thread, debugMarker, TraceEventKind.ExceptionUnwind, exception);
        }

        [Obsolete("do not call this method", true)]
        public static void OnFrameEnterTraceEvent(DebugThread thread) {
            thread.DebugContext.DispatchDebugEvent(thread, 0, TraceEventKind.FrameEnter, null);
        }

        [Obsolete("do not call this method", true)]
        public static void OnFrameExitTraceEvent(DebugThread thread, int debugMarker, object retVal) {
            thread.DebugContext.DispatchDebugEvent(thread, debugMarker, TraceEventKind.FrameExit, retVal);
        }

        [Obsolete("do not call this method", true)]
        public static void OnThreadExitEvent(DebugThread thread) {
            thread.DebugContext.DispatchDebugEvent(thread, Int32.MaxValue, TraceEventKind.ThreadExit, null);
        }

        [Obsolete("do not call this method", true)]
        public static void ReplaceLiftedLocals(DebugFrame frame, IRuntimeVariables liftedLocals) {
            frame.ReplaceLiftedLocals(liftedLocals);
        }

        [Obsolete("do not call this method", true)]
        public static object GeneratorLoopProc(DebugThread thread) {
            return thread.DebugContext.GeneratorLoopProc(thread.GetLeafFrame(), out bool _);
        }

        [Obsolete("do not call this method", true)]
        public static IEnumerator<T> CreateDebugGenerator<T>(DebugFrame frame) {
            return new DebugGenerator<T>(frame);
        }

        [Obsolete("do not call this method", true)]
        public static int GetCurrentSequencePointForGeneratorFrame(DebugFrame frame) {
            Debug.Assert(frame != null);
            Debug.Assert(frame.Generator != null);

            return frame.CurrentLocationCookie;
        }

        [Obsolete("do not call this method", true)]
        public static int GetCurrentSequencePointForLeafGeneratorFrame(DebugThread thread) {
            DebugFrame frame = thread.GetLeafFrame();
            Debug.Assert(frame.Generator != null);

            return frame.CurrentLocationCookie;
        }

        [Obsolete("do not call this method", true)]
        public static bool IsCurrentLeafFrameRemappingToGenerator(DebugThread thread) {
            DebugFrame frame = null;
            if (thread.TryGetLeafFrame(ref frame)) {
                return frame.ForceSwitchToGeneratorLoop;
            }

            return false;
        }

        [Obsolete("do not call this method", true)]
        public static FunctionInfo CreateFunctionInfo(
            Delegate generatorFactory,
            string name,
            object locationSpanMap,
            object scopedVariables,
            object variables,
            object customPayload) {
            return DebugContext.CreateFunctionInfo(generatorFactory, name, (DebugSourceSpan[])locationSpanMap, (IList<VariableInfo>[])scopedVariables, (IList<VariableInfo>)variables, customPayload);
        }

        [Obsolete("do not call this method", true)]
        public static DebugThread GetCurrentThread(DebugContext debugContext) {
            return debugContext.GetCurrentThread();
        }

        [Obsolete("do not call this method", true)]
        public static DebugThread GetThread(DebugFrame frame) {
            return frame.Thread;
        }

        [Obsolete("do not call this method", true)]
        public static bool[] GetTraceLocations(FunctionInfo functionInfo) {
            return functionInfo.GetTraceLocations();
        }

        [Obsolete("do not call this method", true)]
        public static void LiftVariables(DebugThread thread, IRuntimeVariables runtimeVariables) {
            ((DefaultDebugThread)thread).LiftVariables(runtimeVariables);
        }
    }
}
