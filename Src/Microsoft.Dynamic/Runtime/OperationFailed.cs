// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Singleton instance returned from an operator method when the operator method cannot provide a value.
    /// </summary>
    public sealed class OperationFailed {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly OperationFailed Value = new OperationFailed();

        private OperationFailed() {
        }
    }
}
