// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

namespace Microsoft.Scripting.Debugging.CompilerServices {
    /// <summary>
    /// Implemented by compilers to allow the traceback engine to get additional information.
    /// </summary>
    public interface IDebugCompilerSupport {
        bool DoesExpressionNeedReduction(MSAst.Expression expression);
        MSAst.Expression QueueExpressionForReduction(MSAst.Expression expression);
        bool IsCallToDebuggableLambda(MSAst.Expression expression);
    }
}
