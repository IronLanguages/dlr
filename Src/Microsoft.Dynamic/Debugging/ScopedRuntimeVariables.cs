﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// Implements IRuntimeVariables in a way that preserves scoping within the lambda.
    /// </summary>
    internal class ScopedRuntimeVariables : IRuntimeVariables {
        private readonly IList<VariableInfo> _variableInfos;
        private readonly IRuntimeVariables _variables;

        internal ScopedRuntimeVariables(IList<VariableInfo> variableInfos, IRuntimeVariables variables) {
            _variableInfos = variableInfos;
            _variables = variables;
        }

        #region IRuntimeVariables

        public int Count {
            get { return _variableInfos.Count; }
        }

        public object this[int index] {
            get {
                Debug.Assert(index < _variableInfos.Count);
                return _variables[_variableInfos[index].GlobalIndex];
            }
            set {
                Debug.Assert(index < _variableInfos.Count);
                _variables[_variableInfos[index].GlobalIndex] = value;
            }
        }

        #endregion
    }
}
