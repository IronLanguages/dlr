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
        public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters) {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNullItems(parameters, nameof(parameters));

            Name = name;
            Parameters = parameters;
            Documentation = documentation;
        }

        public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters, ParameterDoc returnParameter) {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNullItems(parameters, nameof(parameters));

            Name = name;
            Parameters = parameters;
            Documentation = documentation;
            ReturnParameter = returnParameter;
        }

        /// <summary>
        /// The name of the invokable object.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The documentation for the overload or null if no documentation is available.
        /// </summary>
        public string Documentation { get; }

        /// <summary>
        /// The parameters for the invokable object.
        /// </summary>
        public ICollection<ParameterDoc> Parameters { get; }

        /// <summary>
        /// Information about the return value.
        /// </summary>
        public ParameterDoc ReturnParameter { get; }
    }

}
