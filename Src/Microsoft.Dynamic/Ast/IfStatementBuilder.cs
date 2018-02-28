// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System.Collections.Generic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    public sealed class IfStatementBuilder {
        private readonly List<IfStatementTest> _clauses = new List<IfStatementTest>();

        internal IfStatementBuilder() {
        }

        public IfStatementBuilder ElseIf(Expression test, params Expression[] body) {
            ContractUtils.RequiresNotNullItems(body, nameof(body));
            return ElseIf(test, Utils.BlockVoid(body));
        }

        public IfStatementBuilder ElseIf(Expression test, Expression body) {
            ContractUtils.RequiresNotNull(test, nameof(test));
            ContractUtils.Requires(test.Type == typeof(bool), nameof(test));
            ContractUtils.RequiresNotNull(body, nameof(body));
            _clauses.Add(Utils.IfCondition(test, body));
            return this;
        }

        public Expression Else(params Expression[] body) {
            ContractUtils.RequiresNotNullItems(body, nameof(body));
            return Else(Utils.BlockVoid(body));
        }

        public Expression Else(Expression body) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            return BuildConditions(_clauses, body);
        }

        internal static Expression BuildConditions(IList<IfStatementTest> clauses, Expression @else) {
            Expression result = @else ?? Utils.Empty();

            // This should probably be using SwitchExpression to avoid stack
            // overflow if we have lots of "else" clauses.
            int index = clauses.Count;
            while (index-- > 0) {
                IfStatementTest ist = clauses[index];

                result = Expression.IfThenElse(ist.Test, ist.Body, result);
            }

            return result;
        }

        public Expression ToStatement() {
            return BuildConditions(_clauses, null);
        }

        public static implicit operator Expression(IfStatementBuilder builder) {
            ContractUtils.RequiresNotNull(builder, nameof(builder));
            return builder.ToStatement();
        }
    }

    public partial class Utils {
        public static IfStatementBuilder If() {
            return new IfStatementBuilder();
        }

        public static IfStatementBuilder If(Expression test, params Expression[] body) {
            return If().ElseIf(test, body);
        }

        public static IfStatementBuilder If(Expression test, Expression body) {
            return If().ElseIf(test, body);
        }

        public static Expression If(IfStatementTest[] tests, Expression @else) {
            ContractUtils.RequiresNotNullItems(tests, nameof(tests));
            return IfStatementBuilder.BuildConditions(tests, @else);
        }

        public static Expression IfThen(Expression test, Expression body) {
            return IfThenElse(test, body, null);
        }

        public static Expression IfThen(Expression test, params Expression[] body) {
            return IfThenElse(test, Utils.BlockVoid(body), null);
        }

        public static Expression IfThenElse(Expression test, Expression body, Expression @else) {
            return If(
                new IfStatementTest[] {
                    Utils.IfCondition(test, body)
                },
                @else
            );
        }

        public static Expression Unless(Expression test, Expression body) {
            return IfThenElse(test, Utils.Empty(), body);
        }
    }
}
