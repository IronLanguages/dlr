// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System.Collections.Generic;

namespace Microsoft.Scripting.Debugging.CompilerServices {
    /// <summary>
    /// Used by compilers to provide additional debug information about LambdaExpression to DebugContext
    /// </summary>
    public sealed class DebugLambdaInfo {
        public DebugLambdaInfo(
            IDebugCompilerSupport compilerSupport,
            string lambdaAlias,
            bool optimizeForLeafFrames,
            IList<MSAst.ParameterExpression> hiddenVariables,
            IDictionary<MSAst.ParameterExpression, string> variableAliases,
            object customPayload) {
            CompilerSupport = compilerSupport;
            LambdaAlias = lambdaAlias;
            HiddenVariables = hiddenVariables;
            VariableAliases = variableAliases;
            CustomPayload = customPayload;
            OptimizeForLeafFrames = optimizeForLeafFrames;
        }

        public IDebugCompilerSupport CompilerSupport { get; }

        public string LambdaAlias { get; }

        public IList<MSAst.ParameterExpression> HiddenVariables { get; }

        public IDictionary<MSAst.ParameterExpression, string> VariableAliases { get; }

        public object CustomPayload { get; }

        public bool OptimizeForLeafFrames { get; }
    }
}
