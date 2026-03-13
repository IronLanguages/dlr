// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_NATIVE

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.Utils {
    internal static class NativeMethods {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetEnvironmentVariable(string name, string value);
    }
}

#endif
