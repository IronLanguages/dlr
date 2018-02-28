// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting {

    /// <summary>
    /// See also <c>Microsoft.VisualStudio.Package.TokenTriggers</c>.
    /// </summary>
    [Flags]
    public enum TokenTriggers {
        None = 0,
        MemberSelect = 1,
        MatchBraces = 2,
        ParameterStart = 16,
        ParameterNext = 32,
        ParameterEnd = 64,
        Parameter = 128,
        MethodTip = 240,
    }
}
