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

#if FEATURE_CORE_DLR
using MSAst = System.Linq.Expressions;
#else
using MSAst = Microsoft.Scripting.Ast;
#endif

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
