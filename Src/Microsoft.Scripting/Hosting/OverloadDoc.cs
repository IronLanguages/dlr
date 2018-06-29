// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Provides documentation for a single overload of an invokable object.
    /// </summary>
    [Serializable]
    public class OverloadDoc {
        private readonly string _name, _doc;
        private readonly ICollection<ParameterDoc> _params;
        private readonly ParameterDoc _returnParam;        

        public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters) {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNullItems(parameters, nameof(parameters));

            _name = name;
            _params = parameters;
            _doc = documentation;   
        }

        public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters, ParameterDoc returnParameter) {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNullItems(parameters, nameof(parameters));

            _name = name;
            _params = parameters;
            _doc = documentation;
            _returnParam = returnParameter;
        }

        /// <summary>
        /// The name of the invokable object.
        /// </summary>
        public string Name {
            get {
                return _name;
            }
        }

        /// <summary>
        /// The documentation for the overload or null if no documentation is available.
        /// </summary>
        public string Documentation {
            get {
                return _doc;
            }
        }

        /// <summary>
        /// The parameters for the invokable object.
        /// </summary>
        public ICollection<ParameterDoc> Parameters {
            get {
                return _params;
            }
        }

        /// <summary>
        /// Information about the return value.
        /// </summary>
        public ParameterDoc ReturnParameter {
            get {
                return _returnParam;
            }
        }
    }

}
