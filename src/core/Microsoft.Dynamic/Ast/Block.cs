// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    public static partial class Utils {

        // Helper to add a variable to a block
        internal static Expression AddScopedVariable(Expression body, ParameterExpression variable, Expression variableInit) {
            List<ParameterExpression> vars = new List<ParameterExpression>();
            List<Expression> newBody = new List<Expression>();

            var exprs = new ReadOnlyCollection<Expression>(new [] { body });
            var parent = body;
            //Merge blocks if the current block has only one child that is another block, 
            //the blocks to merge must have the same type.
            while (exprs.Count == 1 && exprs[0].NodeType == ExpressionType.Block && parent.Type == exprs[0].Type) {
                BlockExpression scope = (BlockExpression)(exprs[0]);
                vars.AddRange(scope.Variables);
                parent = scope;
                exprs = scope.Expressions;
            }

            newBody.Add(Expression.Assign(variable, variableInit));
            newBody.AddRange(exprs);
            vars.Add(variable);
            return Expression.Block(
                vars,
                newBody.ToArray()
            );
        }

        internal static BlockExpression BlockVoid(Expression[] expressions) {
            if (expressions.Length == 0 || expressions[expressions.Length - 1].Type != typeof(void)) {
                expressions = expressions.AddLast(Utils.Empty());
            }
            return Expression.Block(expressions);
        }

        internal static BlockExpression Block(Expression[] expressions) {
            if (expressions.Length == 0) {
                expressions = expressions.AddLast(Utils.Empty());
            }
            return Expression.Block(expressions);
        }
    }
}
