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
    public sealed class BlockBuilder : ExpressionCollectionBuilder<Expression> {
        public BlockBuilder() {
        }

        /// <summary>
        /// Returns <c>null</c> if no expression was added into the builder.
        /// If only a single expression was added returns it.
        /// Otherwise returns a <see cref="BlockExpression"/> containing the expressions added to the builder.
        /// </summary>
        public Expression ToExpression() {
            switch (Count) {
                case 0: return null;
                case 1: return Expression0;
                case 2: return Expression.Block(Expression0, Expression1);
                case 3: return Expression.Block(Expression0, Expression1, Expression2);
                case 4: return Expression.Block(Expression0, Expression1, Expression2, Expression3);
                default: return Expression.Block(Expressions);
            }
        }

        public static implicit operator Expression(BlockBuilder/*!*/ block) {
            return block.ToExpression();
        }
    }
}
