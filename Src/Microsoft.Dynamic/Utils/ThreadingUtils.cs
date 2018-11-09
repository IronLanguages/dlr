// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.Scripting.Utils {
    public static class ThreadingUtils {
        private static int id;
        private static System.Threading.ThreadLocal<int> threadIds = new System.Threading.ThreadLocal<int>(() => Interlocked.Increment(ref id));
        
        public static int GetCurrentThreadId() {
            return threadIds.Value;
        }
    }
}
