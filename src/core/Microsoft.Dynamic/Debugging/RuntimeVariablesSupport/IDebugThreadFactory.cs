// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System.Collections.Generic;
using Microsoft.Scripting.Debugging.CompilerServices;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// IDebugThreadFactory is used to abstract how frames and local variables are maintained at run/debug time.
    /// </summary>
    internal interface IDebugThreadFactory {
        DebugThread CreateDebugThread(DebugContext debugContext);

        MSAst.Expression CreatePushFrameExpression(
            MSAst.ParameterExpression functionInfo,
            MSAst.ParameterExpression debugMarker,
            IList<MSAst.ParameterExpression> locals,
            IList<VariableInfo> varInfos,
            MSAst.Expression runtimeThread);
    }
}
