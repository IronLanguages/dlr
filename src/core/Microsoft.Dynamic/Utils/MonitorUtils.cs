// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.Scripting.Utils {
    public static class MonitorUtils {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static void Enter(object obj, ref bool lockTaken) {
            Monitor.Enter(obj, ref lockTaken);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static bool TryEnter(object obj, ref bool lockTaken) {
            Monitor.TryEnter(obj, ref lockTaken);
            return lockTaken;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static void Exit(object obj, ref bool lockTaken) {
            try {
            } finally {
                // finally prevents thread abort to leak the lock:
                lockTaken = false;
                Monitor.Exit(obj);
            }
        }
    }
}
