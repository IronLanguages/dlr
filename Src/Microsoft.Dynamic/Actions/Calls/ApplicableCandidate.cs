// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    public sealed class ApplicableCandidate {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public readonly MethodCandidate Method;
        public readonly ArgumentBinding ArgumentBinding;

        internal ApplicableCandidate(MethodCandidate method, ArgumentBinding argBinding) {
            Assert.NotNull(method, argBinding);
            Method = method;
            ArgumentBinding = argBinding;
        }

        public ParameterWrapper GetParameter(int argumentIndex) {
            return Method.GetParameter(argumentIndex, ArgumentBinding);
        }

        public override string ToString() {
            return Method.ToString();
        }
    }
}
