// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Specifies the action for which the default binder is requesting a member.
    /// </summary>
    public enum MemberRequestKind {
        None,
        Get,
        Set,
        Delete,
        Invoke,
        InvokeMember,
        Convert,
        Operation
    }
}
