// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Represents the type of a null value.
    /// </summary>
    public sealed class DynamicNull {
        /// <summary>
        /// Private constructor is never called since 'null' is the only valid instance.
        /// </summary>
        private DynamicNull() { }
    }
}
