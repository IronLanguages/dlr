// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    public static partial class Utils {

        /// <summary>
        /// Null coalescing expression
        /// {result} ::= ((tmp = {_left}) == null) ? {right} : tmp
        /// '??' operator in C#.
        /// </summary>
        public static Expression Coalesce(Expression left, Expression right, out ParameterExpression temp) {
            return CoalesceInternal(left, right, null, false, out temp);
        }

        /// <summary>
        /// True coalescing expression.
        /// {result} ::= IsTrue(tmp = {left}) ? {right} : tmp
        /// Generalized AND semantics.
        /// </summary>
        public static Expression CoalesceTrue(Expression left, Expression right, MethodInfo isTrue, out ParameterExpression temp) {
            ContractUtils.RequiresNotNull(isTrue, nameof(isTrue));
            return CoalesceInternal(left, right, isTrue, false, out temp);
        }

        /// <summary>
        /// False coalescing expression.
        /// {result} ::= IsTrue(tmp = {left}) ? tmp : {right}
        /// Generalized OR semantics.
        /// </summary>
        public static Expression CoalesceFalse(Expression left, Expression right, MethodInfo isTrue, out ParameterExpression temp) {
            ContractUtils.RequiresNotNull(isTrue, nameof(isTrue));
            return CoalesceInternal(left, right, isTrue, true, out temp);
        }

        private static Expression CoalesceInternal(Expression left, Expression right, MethodInfo isTrue, bool isReverse, out ParameterExpression temp) {
            ContractUtils.RequiresNotNull(left, nameof(left));
            ContractUtils.RequiresNotNull(right, nameof(right));

            // A bit too strict, but on a safe side.
            ContractUtils.Requires(left.Type == right.Type, "Expression types must match");

            temp = Expression.Variable(left.Type, "tmp_left");

            Expression condition;
            if (isTrue != null) {
                ContractUtils.Requires(isTrue.ReturnType == typeof(bool), nameof(isTrue), "Predicate must return bool.");
                ParameterInfo[] parameters = isTrue.GetParameters();
                ContractUtils.Requires(parameters.Length == 1, nameof(isTrue), "Predicate must take one parameter.");
                ContractUtils.Requires(isTrue.IsStatic && isTrue.IsPublic, nameof(isTrue), "Predicate must be public and static.");

                Type pt = parameters[0].ParameterType;
                ContractUtils.Requires(TypeUtils.CanAssign(pt, left.Type), nameof(left), "Incorrect left expression type");
                condition = Expression.Call(isTrue, Expression.Assign(temp, left));
            } else {
                ContractUtils.Requires(TypeUtils.CanCompareToNull(left.Type), nameof(left), "Incorrect left expression type");
                condition = Expression.Equal(Expression.Assign(temp, left), Constant(null, left.Type));
            }

            Expression t, f;
            if (isReverse) {
                t = temp;
                f = right;
            } else {
                t = right;
                f = temp;
            }

            return Expression.Condition(condition, t, f);
        }

        public static Expression Coalesce(LambdaBuilder builder, Expression left, Expression right) {
            Expression result = Coalesce(left, right, out ParameterExpression temp);
            builder.AddHiddenVariable(temp);
            return result;
        }

        /// <summary>
        /// True coalescing expression.
        /// {result} ::= IsTrue(tmp = {left}) ? {right} : tmp
        /// Generalized AND semantics.
        /// </summary>
        public static Expression CoalesceTrue(LambdaBuilder builder, Expression left, Expression right, MethodInfo isTrue) {
            ContractUtils.RequiresNotNull(isTrue, nameof(isTrue));
            Expression result = CoalesceTrue(left, right, isTrue, out ParameterExpression temp);
            builder.AddHiddenVariable(temp);
            return result;
        }

        /// <summary>
        /// False coalescing expression.
        /// {result} ::= IsTrue(tmp = {left}) ? tmp : {right}
        /// Generalized OR semantics.
        /// </summary>
        public static Expression CoalesceFalse(LambdaBuilder builder, Expression left, Expression right, MethodInfo isTrue) {
            ContractUtils.RequiresNotNull(isTrue, nameof(isTrue));
            Expression result = CoalesceFalse(left, right, isTrue, out ParameterExpression temp);
            builder.AddHiddenVariable(temp);
            return result;
        }

        public static BinaryExpression Update(this BinaryExpression expression, Expression left, Expression right) {
            return expression.Update(left, expression.Conversion, right);
        }
    }
}
