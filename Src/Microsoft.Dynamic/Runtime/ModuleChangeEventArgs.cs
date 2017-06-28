/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

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
