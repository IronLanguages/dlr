// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// Implementation of IDebugRuntimeVariables, which wraps IRuntimeVariables + FunctionInfo/DebugMarker
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    internal class DebugRuntimeVariables : IDebugRuntimeVariables {
        private readonly IRuntimeVariables _runtimeVariables;

        internal DebugRuntimeVariables(IRuntimeVariables runtimeVariables) {
            _runtimeVariables = runtimeVariables;
        }

        #region IRuntimeVariables

        public int Count {
            get { return _runtimeVariables.Count - 2; }
        }

        public object this[int index] {
            get { return _runtimeVariables[2 + index]; }
            set { _runtimeVariables[2 + index] = value; }
        }

        #endregion

        #region IDebugRuntimeVariables

        public FunctionInfo FunctionInfo {
            get { return (FunctionInfo)_runtimeVariables[0]; }
        }

        public int DebugMarker {
            get { return (int)_runtimeVariables[1]; }
        }

        #endregion
    }
}
