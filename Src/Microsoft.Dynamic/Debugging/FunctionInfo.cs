// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Debugging {
    [DebuggerDisplay("{Name}")]
    public sealed class FunctionInfo {
        private readonly IList<VariableInfo>[] _variableScopeMap;
        private readonly bool[] _traceLocations;

        internal FunctionInfo(
            Delegate generatorFactory,
            string name,
            DebugSourceSpan[] sequencePoints,
            IList<VariableInfo>[] scopedVariables,
            IList<VariableInfo> variables,
            object customPayload) {

            GeneratorFactory = generatorFactory;
            Name = name;
            SequencePoints = sequencePoints;
            _variableScopeMap = scopedVariables;
            Variables = variables;
            CustomPayload = customPayload;
            _traceLocations = new bool[sequencePoints.Length];
        }

        internal Delegate GeneratorFactory { get; }

        internal IList<VariableInfo> Variables { get; }

        internal IList<VariableInfo>[] VariableScopeMap {
            get { return _variableScopeMap; }
        }

        internal FunctionInfo PreviousVersion { get; set; }

        internal FunctionInfo NextVersion { get; set; }

        internal int Version { get; set; }

        /// <summary>
        /// SequencePoints
        /// </summary>
        internal DebugSourceSpan[] SequencePoints { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the custom payload.
        /// </summary>
        internal object CustomPayload { get; }

        /// <summary>
        /// GetTraceLocations
        /// </summary>
        /// <returns></returns>
        internal bool[] GetTraceLocations() {
            return _traceLocations;
        }
    }
}
