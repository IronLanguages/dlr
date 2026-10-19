// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter {
    /// <summary>
    /// Helpers referenced by the instructions copied from .NET (see DotNet/README.md).
    /// </summary>
    internal static class Utils {
        public static readonly object BoxedFalse = ScriptingRuntimeHelpers.False;
        public static readonly object BoxedTrue = ScriptingRuntimeHelpers.True;
    }
}
