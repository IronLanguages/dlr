// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Event args for when a ScriptScope has had its contents changed.  
    /// </summary>
    public class ModuleChangeEventArgs : EventArgs {
        /// <summary>
        /// Creates a new ModuleChangeEventArgs object with the specified name and type.
        /// </summary>
        public ModuleChangeEventArgs(string name, ModuleChangeType changeType) {
            Name = name;
            ChangeType = changeType;
        }

        /// <summary>
        /// Creates a nwe ModuleChangeEventArgs with the specified name, type, and changed value.
        /// </summary>
        public ModuleChangeEventArgs(string name, ModuleChangeType changeType, object value) {
            Name = name;
            ChangeType = changeType;
            Value = value;
        }

        /// <summary>
        /// Gets the name of the symbol that has changed.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the way in which the symbol has changed: Set or Delete.
        /// </summary>
        public ModuleChangeType ChangeType { get; }

        /// <summary>
        /// Gets the symbol has been set provides the new value.
        /// </summary>
        public object Value { get; }
    }
}
