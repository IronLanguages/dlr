// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {

    // TODO: this class should be abstract
    public class ScopeExtension {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public static readonly ScopeExtension[] EmptyArray = new ScopeExtension[0];

        public Scope Scope { get; }

        public ScopeExtension(Scope scope) {
            ContractUtils.RequiresNotNull(scope, nameof(scope));
            Scope = scope;
        }
    }
}
