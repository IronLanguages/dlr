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
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Debugging {
    [DebuggerDisplay("{Name}")]
    public sealed class FunctionInfo {
        private readonly DebugSourceSpan[] _sequencePoints;
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
            _sequencePoints = sequencePoints;
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
        internal DebugSourceSpan[] SequencePoints {
            get { return _sequencePoints; }
        }

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
