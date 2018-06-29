// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Dynamic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    /// Factory methods.
    /// </summary>
    public static partial class Utils {
        public static LoopExpression While(Expression test, Expression body, Expression @else) {
            return Loop(test, null, body, @else, null, null);
        }

        public static LoopExpression While(Expression test, Expression body, Expression @else, LabelTarget @break, LabelTarget @continue) {
            return Loop(test, null, body, @else, @break, @continue);
        }

        public static LoopExpression Infinite(Expression body) {
            return Expression.Loop(body, null, null);
        }

        public static LoopExpression Infinite(Expression body, LabelTarget @break, LabelTarget @continue) {
            return Expression.Loop(body, @break, @continue);
        }

        public static LoopExpression Loop(Expression test, Expression increment, Expression body, Expression @else) {
            return Loop(test, increment, body, @else, null, null);
        }

        public static LoopExpression Loop(Expression test, Expression increment, Expression body, Expression @else, LabelTarget @break, LabelTarget @continue) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            if (test != null) {
                ContractUtils.Requires(test.Type == typeof(bool), nameof(test), "Test must be boolean");
                if (@break == null) {
                    @break = Expression.Label();
                }
            }

            // for (;;) {
            //     if (test) {
            //     } else {
            //        else;
            //        break;
            //     }
            //     Body
            // continue:
            //     Increment;
            // }

            // If there is no test, 'else' will never execute and gets simply thrown away.
            return Expression.Loop(
                Expression.Block(
                    test != null
                        ? (Expression)Expression.Condition(
                            test,
                            Utils.Empty(),
                            Expression.Block(
                                @else ?? Empty(),
                                Expression.Break(@break)
                            )
                        )
                        : Empty(),
                    body,
                    @continue != null ? (Expression)Expression.Label(@continue) : Empty(),
                    increment ?? Empty()
                ),
                @break,
                null
            );
        }
    }
}
