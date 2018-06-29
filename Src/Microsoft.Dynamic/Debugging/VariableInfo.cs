// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// Used to provide information about locals/parameters at debug time.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    internal sealed class VariableInfo {
        
        /// <summary>
        /// Index within byref variables list or within strongbox variables list.
        /// </summary>
        private readonly int _localIndex;

        /// <summary>
        /// Index within the combined list.
        /// </summary>
        private readonly int _globalIndex;

        internal VariableInfo(string name, Type type, bool parameter, bool hidden, bool strongBoxed, int localIndex, int globalIndex) {
            Name = name;
            VariableType = type;
            IsParameter = parameter;
            Hidden = hidden;
            IsStrongBoxed = strongBoxed;
            _localIndex = localIndex;
            _globalIndex = globalIndex;
        }

        internal VariableInfo(string name, Type type, bool parameter, bool hidden, bool strongBoxed)
            : this(name, type, parameter, hidden, strongBoxed, Int32.MaxValue, Int32.MaxValue) {
            Name = name;
            VariableType = type;
            IsParameter = parameter;
            Hidden = hidden;
            IsStrongBoxed = strongBoxed;
        }

        internal bool Hidden { get; }

        internal bool IsStrongBoxed { get; }

        internal int LocalIndex {
            get { Debug.Assert(_localIndex != Int32.MaxValue); return _localIndex; }
        }

        internal int GlobalIndex {
            get { Debug.Assert(_globalIndex != Int32.MaxValue); return _globalIndex; }
        }

        /// <summary>
        /// Gets the variable type.
        /// </summary>
        internal Type VariableType { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets or sets a value indicating whether it is a parameter.
        /// </summary>
        internal bool IsParameter { get; }
    }
}
