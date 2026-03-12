// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// IDebugRuntimeVariables is used to wrap IRuntimeVariables and add properties for retrieving
    /// FunctionInfo and DebugMarker from debuggable labmdas.
    /// </summary>
    internal interface IDebugRuntimeVariables : IRuntimeVariables {
        FunctionInfo FunctionInfo { get; }
        int DebugMarker { get; }
    }
}
