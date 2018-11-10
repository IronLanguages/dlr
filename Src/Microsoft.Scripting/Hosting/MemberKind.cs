// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Specifies the type of member.
    /// </summary>
    public enum MemberKind {
        None,
        Class,
        Delegate,
        Enum,
        Event,
        Field,
        Function,
        Module,
        Property,
        Constant,
        EnumMember,
        Instance,
        Method,
        Namespace
    }
}
