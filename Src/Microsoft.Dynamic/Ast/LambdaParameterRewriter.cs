// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System.Collections.Generic;

namespace Microsoft.Scripting.Ast {
    internal sealed class LambdaParameterRewriter : ExpressionVisitor {
        private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

        internal LambdaParameterRewriter(Dictionary<ParameterExpression, ParameterExpression> map) {
            _map = map;
        }

        // We don't need to worry about parameter shadowing, because we're
        // replacing the instances consistently everywhere
        protected override Expression VisitParameter(ParameterExpression node) {
            if (_map.TryGetValue(node, out ParameterExpression result)) {
                return result;
            }
            return node;
        }
    }
}
