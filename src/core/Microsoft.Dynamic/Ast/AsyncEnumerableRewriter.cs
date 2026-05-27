// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

#if NET

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    ///   Reduces an <see cref="AsyncEnumerableExpression"/> to an <c>IAsyncEnumerable&lt;object&gt;</c>-valued
    ///   expression tree that yields each <see cref="AwaitExpression"/>'s operand (wrapped in an
    ///   <see cref="Runtime.AwaitPoint"/>) alongside language-level yields, and hands
    ///   the resulting state machine to <see cref="Runtime.AsyncHelpers.DriveAsyncEnumerable"/>.
    /// </summary>
    internal sealed class AsyncEnumerableRewriter {
        private static readonly MethodInfo s_driveMethod
            = typeof(Runtime.AsyncHelpers).GetMethod(nameof(Runtime.AsyncHelpers.DriveAsyncEnumerable))!;
        private static readonly ConstructorInfo s_awaitPointCtor
            = typeof(Runtime.AwaitPoint).GetConstructor([typeof(Task)])!;
        private static readonly FieldInfo s_valueSlotField
            = typeof(StrongBox<object?>).GetField(nameof(StrongBox<object?>.Value))!;
        private static readonly FieldInfo s_exceptionSlotField
            = typeof(StrongBox<Exception?>).GetField(nameof(StrongBox<Exception?>.Value))!;
        private static readonly ConstructorInfo s_valueSlotCtor
            = typeof(StrongBox<object?>).GetConstructor(Type.EmptyTypes)!;
        private static readonly ConstructorInfo s_exceptionSlotCtor
            = typeof(StrongBox<Exception?>).GetConstructor(Type.EmptyTypes)!;

        private readonly AsyncEnumerableExpression _node;

        public AsyncEnumerableRewriter(AsyncEnumerableExpression node) {
            _node = node;
        }

        public Expression Reduce() {
            // valueSlot / exceptionSlot carry the per-await result / fault back into the body at each
            // await's resume point (same role as in AsyncRewriter). The generator's final value is
            // irrelevant — generators don't return a value — so there is no capture step here.
            ParameterExpression valueSlot = Expression.Variable(typeof(StrongBox<object?>), "$asyncValue");
            ParameterExpression exceptionSlot = Expression.Variable(typeof(StrongBox<Exception?>), "$awaitException");

            var rewriter = new AwaitToAwaitPointRewriter(_node.YieldLabel, valueSlot, exceptionSlot);
            Expression rewrittenBody = rewriter.Visit(_node.Body);

            // Coerce to void for Utils.Generator (the generator body's value is discarded).
            Expression generatorBody = rewrittenBody.Type == typeof(void)
                ? rewrittenBody
                : Expression.Block(typeof(void), rewrittenBody);

            Expression generator = Utils.Generator(
                _node.Name ?? "$asyncgen",
                _node.YieldLabel,
                generatorBody,
                typeof(IEnumerator<object>),
                rewriteAssignments: false);

            // Argument order matches DriveAsyncEnumerable: ..., cancellationToken, cancellationException
            // (same as DriveAsync — cancellationToken is the last required parameter).
            Expression drive = Expression.Call(
                s_driveMethod,
                generator,
                valueSlot,
                exceptionSlot,
                _node.CancellationToken,
                _node.CancellationException);

            return Expression.Block(
                typeof(IAsyncEnumerable<object?>),
                [valueSlot, exceptionSlot],
                Expression.Assign(valueSlot, Expression.New(s_valueSlotCtor)),
                Expression.Assign(exceptionSlot, Expression.New(s_exceptionSlotCtor)),
                drive);
        }

        /// <summary>
        ///   Rewrites <c>AwaitExpression(task)</c> → <c>{ yield AwaitPoint(task); rethrow-if-pending; valueSlot.Value }</c>,
        ///   targeting the shared yield label. Mirrors <c>AsyncRewriter.AwaitToYieldRewriter</c> but wraps the awaited
        ///   Task in an <see cref="Microsoft.Scripting.Runtime.AwaitPoint"/> so the driver distinguishes it from a
        ///   value yielded by a language-level <c>yield</c>.
        /// </summary>
        private sealed class AwaitToAwaitPointRewriter : ExpressionVisitor {
            private static readonly MethodInfo s_captureMethod
                = typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Capture))!;
            private static readonly MethodInfo s_throwMethod
                = typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Throw), Type.EmptyTypes)!;

            private readonly LabelTarget _yieldLabel;
            private readonly ParameterExpression _valueSlot;
            private readonly ParameterExpression _exceptionSlot;

            public AwaitToAwaitPointRewriter(LabelTarget yieldLabel, ParameterExpression valueSlot, ParameterExpression exceptionSlot) {
                _yieldLabel = yieldLabel;
                _valueSlot = valueSlot;
                _exceptionSlot = exceptionSlot;
            }

            protected override Expression VisitExtension(Expression node) {
                if (node is AwaitExpression aw) {
                    Expression operand = Visit(aw.Operand);
                    // Wrap the awaited Task in an AwaitPoint marker, then box to object for the yield.
                    Expression awaitPoint = Expression.New(s_awaitPointCtor, Expression.Convert(operand, typeof(Task)));
                    Expression yielded = Expression.Convert(awaitPoint, typeof(object));

                    Expression readException = Expression.Field(_exceptionSlot, s_exceptionSlotField);
                    Expression readSlot = Expression.Field(_valueSlot, s_valueSlotField);

                    Expression rethrow = Expression.IfThen(
                        Expression.ReferenceNotEqual(readException, Expression.Constant(null, typeof(Exception))),
                        Expression.Call(
                            Expression.Call(s_captureMethod, readException),
                            s_throwMethod));

                    return Expression.Block(
                        typeof(object),
                        Utils.YieldReturn(_yieldLabel, yielded),
                        rethrow,
                        readSlot);
                }
                return base.VisitExtension(node);
            }
        }
    }
}

#endif
