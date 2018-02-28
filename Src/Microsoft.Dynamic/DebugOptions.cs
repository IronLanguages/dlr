// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security;
using System;

namespace Microsoft.Scripting {

    /// <summary>
    /// This class holds onto internal debugging options used in this assembly. 
    /// These options can be set via environment variables DLR_{option-name}.
    /// Boolean options map "true" to true and other values to false.
    /// 
    /// These options are for internal debugging only, and should not be
    /// exposed through any public APIs.
    /// </summary>
    internal static class DebugOptions {

        private static bool ReadOption(string name) {
            string envVar = ReadString(name);
            return envVar != null && envVar.ToLowerInvariant() == "true";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name")]
        private static bool ReadDebugOption(string name) {
#if DEBUG
            return ReadOption(name);
#else
            return false;
#endif
        }

        private static string ReadString(string name) {
#if FEATURE_PROCESS
            try {
                return Environment.GetEnvironmentVariable("DLR_" + name);
            } catch (SecurityException) {
                return null;
            }
#else
            return null;
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name")]
        private static string ReadDebugString(string name) {
#if DEBUG
            return ReadString(name);
#else
            return null;
#endif
        }

        private static readonly bool _trackPerformance = ReadDebugOption("TrackPerformance");

        internal static bool TrackPerformance {
            get { return _trackPerformance; }
        }
    }
}