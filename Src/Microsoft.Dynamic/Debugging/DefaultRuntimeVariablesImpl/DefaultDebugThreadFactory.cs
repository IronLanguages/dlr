// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System.Collections.Generic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Debugging {
    using Ast = MSAst.Expression;

    /// <summary>
    /// Default implementation of IDebugThreadFactory, which uses DLR's RuntimeVariablesExpression for lifting locals.
    /// </summary>
    internal sealed class DefaultDebugThreadFactory : IDebugThreadFactory {
        public DebugThread CreateDebugThread(Microsoft.Scripting.Debugging.CompilerServices.DebugContext debugContext) {
            return new DefaultDebugThread(debugContext);
        }

        public MSAst.Expression CreatePushFrameExpression(MSAst.ParameterExpression functionInfo, MSAst.ParameterExpression debugMarker, IList<MSAst.ParameterExpression> locals, IList<VariableInfo> varInfos, MSAst.Expression runtimeThread) {
            MSAst.ParameterExpression[] args = new MSAst.ParameterExpression[2 + locals.Count];
            args[0] = functionInfo;
            args[1] = debugMarker;
            for (int i = 0; i < locals.Count; i++) {
                args[i + 2] = locals[i];
            }

            return Ast.Call(
                typeof(RuntimeOps).GetMethod("LiftVariables"),
                runtimeThread,
                Ast.RuntimeVariables(args)
            );
        }
    }
}
