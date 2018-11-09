// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Provides documentation about a member in a live object.
    /// </summary>
    [Serializable]
    public class MemberDoc {
        public MemberDoc(string name, MemberKind kind) {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.Requires(kind >= MemberKind.None && kind <= MemberKind.Namespace, nameof(kind));

            Name = name;
            Kind = kind;
        }

        /// <summary>
        /// The name of the member
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The kind of the member if it's known.
        /// </summary>
        public MemberKind Kind { get; }
    }
}
