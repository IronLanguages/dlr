// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// The way in which a module has changed : Set or Delete
    /// </summary>
    public enum ModuleChangeType {
        /// <summary>
        /// A new value has been set in the module (or a previous value has changed).
        /// </summary>
        Set,
        /// <summary>
        /// A value has been removed from the module.
        /// </summary>
        Delete
    }
}
