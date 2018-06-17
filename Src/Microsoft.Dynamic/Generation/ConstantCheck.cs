// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System.Diagnostics;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation {

    public static class ConstantCheck {

        /// <summary>
        /// Tests to see if the expression is a constant with the given value.
        /// </summary>
        /// <param name="expression">The expression to examine</param>
        /// <param name="value">The constant value to check for.</param>
        /// <returns>true/false</returns>
        public static bool Check(Expression expression, object value) {
            ContractUtils.RequiresNotNull(expression, nameof(expression));
            return IsConstant(expression, value);
        }


        /// <summary>
        /// Tests to see if the expression is a constant with the given value.
        /// </summary>
        /// <param name="e">The expression to examine</param>
        /// <param name="value">The constant value to check for.</param>
        /// <returns>true/false</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        internal static bool IsConstant(Expression e, object value) {
            switch (e.NodeType) {
                case ExpressionType.AndAlso:
                    return CheckAndAlso((BinaryExpression)e, value);

                case ExpressionType.OrElse:
                    return CheckOrElse((BinaryExpression)e, value);

                case ExpressionType.Constant:
                    return CheckConstant((ConstantExpression)e, value);

                case ExpressionType.TypeIs:
                    return Check((TypeBinaryExpression)e, value);

                default:
                    return false;
            }
        }

        //CONFORMING
        internal static bool IsNull(Expression e) {
            return IsConstant(e, null);
        }


        private static bool CheckAndAlso(BinaryExpression node, object value) {
            Debug.Assert(node.NodeType == ExpressionType.AndAlso);

            if (node.Method != null) {
                return false;
            }
            //TODO: we can propagate through conversion, but it may not worth it.
            if (node.Conversion != null) {
                return false;
            }
    
            if (value is bool b) {
                if (b) {
                    return IsConstant(node.Left, true) && IsConstant(node.Right, true);
                }

                // if left isn't a constant it has to be evaluated
                return IsConstant(node.Left, false);
            }
            return false;
        }

        private static bool CheckOrElse(BinaryExpression node, object value) {
            Debug.Assert(node.NodeType == ExpressionType.OrElse);

            if (node.Method != null) {
                return false;
            }

            if (value is bool b) {
                if (b) {
                    return IsConstant(node.Left, true);
                }

                return IsConstant(node.Left, false) && IsConstant(node.Right, false);
            }

            return false;
        }

        private static bool CheckConstant(ConstantExpression node, object value) {
            if (value == null) {
                return node.Value == null;
            }

            return value.Equals(node.Value);
        }

        private static bool Check(TypeBinaryExpression node, object value) {
            // allow constant TypeIs expressions to be optimized away
            if (value is bool b && b) {
                return node.TypeOperand.IsAssignableFrom(node.Expression.Type);
            }
            return false;
        }
    }
}
