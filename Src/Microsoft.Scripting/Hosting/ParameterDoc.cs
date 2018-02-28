// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Provides documentation for a single parameter.
    /// </summary>
    [Serializable]
    public class ParameterDoc {
        private readonly string _name, _typeName, _doc;
        private readonly ParameterFlags _flags;

        public ParameterDoc(string name)
            : this(name, null, null, ParameterFlags.None) {
        }

        public ParameterDoc(string name, ParameterFlags paramFlags)
            : this(name, null, null, paramFlags) {
        }

        public ParameterDoc(string name, string typeName)
            : this(name, typeName, null, ParameterFlags.None) {
        }

        public ParameterDoc(string name, string typeName, string documentation)
            : this(name, typeName, documentation, ParameterFlags.None) {
        }

        public ParameterDoc(string name, string typeName, string documentation, ParameterFlags paramFlags) {
            ContractUtils.RequiresNotNull(name, nameof(name));

            _name = name;
            _flags = paramFlags;
            _typeName = typeName;
            _doc = documentation;
        }

        /// <summary>
        /// The name of the parameter
        /// </summary>
        public string Name {
            get {
                return _name;
            }
        }

        /// <summary>
        /// The type name of the parameter or null if no type information is available.
        /// </summary>
        public string TypeName {
            get {
                return _typeName;
            }
        }

        /// <summary>
        /// Provides addition information about the parameter such as if it's a parameter array.
        /// </summary>
        public ParameterFlags Flags {
            get {
                return _flags;
            }
        }

        /// <summary>
        /// Gets the documentation string for this parameter or null if no documentation is available.
        /// </summary>
        public string Documentation {
            get {
                return _doc;
            }
        }
    }

}
