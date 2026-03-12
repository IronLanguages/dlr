// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System.Collections.Generic;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// Used to extract locals information from expressions.
    /// </summary>
    internal sealed class LambdaWalker : MSAst.ExpressionVisitor {
        private readonly List<MSAst.ParameterExpression> _locals;
        private readonly Dictionary<MSAst.ParameterExpression, object> _strongBoxedLocals;

        internal LambdaWalker() {
            _locals = new List<MSAst.ParameterExpression>();
            _strongBoxedLocals = new Dictionary<MSAst.ParameterExpression, object>();
        }

        internal List<MSAst.ParameterExpression> Locals {
            get { return _locals; }
        }

        internal Dictionary<MSAst.ParameterExpression, object> StrongBoxedLocals {
            get { return _strongBoxedLocals; }
        }

        protected override MSAst.Expression VisitBlock(MSAst.BlockExpression node) {
            // Record all variables declared within the block
            foreach (MSAst.ParameterExpression local in node.Variables) {
                _locals.Add(local);
            }

            return base.VisitBlock(node);
        }

        protected override MSAst.Expression VisitRuntimeVariables(MSAst.RuntimeVariablesExpression node) {
            // Record all strongbox'ed variables
            foreach (MSAst.ParameterExpression local in node.Variables) {
                _strongBoxedLocals.Add(local, null);
            }

            return base.VisitRuntimeVariables(node);
        }

        protected override MSAst.Expression VisitLambda<T>(MSAst.Expression<T> node) {
            // Explicitely don't walk nested lambdas.  They should already have been transformed
            return node;
        }
    }
}
