// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Runtime {
    public static class ExceptionHelpers {
#if FEATURE_STACK_TRACE
        private const string prevStackTraces = "PreviousStackTraces";

        /// <summary>
        /// Updates an exception before it's getting re-thrown so
        /// we can present a reasonable stack trace to the user.
        /// </summary>
        public static Exception UpdateForRethrow(Exception rethrow) {

            // we don't have any dynamic stack trace data, capture the data we can
            // from the raw exception object.
            StackTrace st = new StackTrace(rethrow, true);

            if (!TryGetAssociatedStackTraces(rethrow, out List<StackTrace> prev)) {
                prev = new List<StackTrace>();
                AssociateStackTraces(rethrow, prev);
            }

            prev.Add(st);

            return rethrow;
        }

        /// <summary>
        /// Returns all the stack traces associates with an exception
        /// </summary>
        public static IList<StackTrace> GetExceptionStackTraces(Exception rethrow) {
            return TryGetAssociatedStackTraces(rethrow, out List<StackTrace> result) ? result : null;
        }

        private static void AssociateStackTraces(Exception e, List<StackTrace> traces) {
            e.Data[prevStackTraces] = traces;
        }

        private static bool TryGetAssociatedStackTraces(Exception e, out List<StackTrace> traces) {
            traces = e.Data[prevStackTraces] as List<StackTrace>;
            return traces != null;
        }        
#else
        public static Exception UpdateForRethrow(Exception rethrow) {
            return rethrow;
        }
#endif
    }
}
