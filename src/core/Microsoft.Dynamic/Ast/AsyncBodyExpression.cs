// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    /// Wraps an async function body (possibly containing <see cref="AwaitExpression"/>
    /// nodes) into an expression that evaluates to <see cref="Task{TResult}"/> of
    /// <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="LambdaExpression"/>, this is a
    /// sub-expression: it does not introduce a new lambda scope, so the body has
    /// direct access to parameters and locals of the enclosing scope. Callers
    /// typically wrap the resulting Task in their own coroutine façade type.
    ///
    /// The body is expected to evaluate to an <see cref="object"/> (can be null):
    /// that value becomes the Task's result.
    /// <br/>
    /// State-machine splitting at await sites is delegated to <see cref="GeneratorExpression"/>;
    /// the runtime-async <c>await</c> opcodes come from
    /// <see cref="AsyncRunner.Drive"/>, which Roslyn compiles into a runtime-async
    /// method when the project sets <c>&lt;Features&gt;runtime-async=on&lt;/Features&gt;</c>
    /// on .NET 11+.
    /// </remarks>
    public sealed class AsyncBodyExpression : Expression {
        private Expression? _reduced;

        internal AsyncBodyExpression(string? name, Expression body, Expression cancellationToken) {
            Name = name;
            Body = body;
            CancellationToken = cancellationToken;
        }

        /// <summary>Optional diagnostic name (forwarded to the inner generator).</summary>
        public string? Name { get; }

        /// <summary>The function body. May contain <see cref="AwaitExpression"/> nodes.</summary>
        public Expression Body { get; }

        /// <summary>
        /// Expression evaluating to a <see cref="System.Threading.CancellationToken"/>
        /// that <see cref="AsyncRunner.Drive"/> samples between iterations and links
        /// to each suspended task. Defaults to <c>default(CancellationToken)</c>.
        /// </summary>
        public Expression CancellationToken { get; }

        public override bool CanReduce => true;

        public override Type Type => typeof(Task<object?>);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Expression Reduce() {
            // Cache the reduction so that LabelTarget identity is preserved
            // across the multiple Reduce() invocations the compiler may make
            // (closure analysis, IL emission, etc.). Without this the
            // inner GeneratorRewriter sees yields whose Target was minted on
            // a different Reduce() call than the surrounding generator.
            return _reduced ??= BuildReduction();
        }

        private Expression BuildReduction() {
            // resultSlot:    where the runner writes each awaited result before resuming.
            // exceptionSlot: where the runner stashes a faulted-await exception so the
            //                state machine can rethrow at the resumed position (lets
            //                a Python try/except around `await` observe e.g.
            //                StopAsyncIteration).
            // returnSlot:    where the body's final value lands before the runner returns.
            ParameterExpression resultSlot = Expression.Variable(typeof(StrongBox<object?>), "$awaitResult");
            ParameterExpression exceptionSlot = Expression.Variable(typeof(StrongBox<Exception?>), "$awaitException");
            ParameterExpression returnSlot = Expression.Variable(typeof(StrongBox<object?>), "$asyncReturn");
            LabelTarget yieldLabel = Expression.Label(typeof(object), "$asyncYield");

            // Rewrite AwaitExpression(e) -> { yield e; rethrow-if-pending; resultSlot.Value }
            var rewriter = new AwaitToYieldRewriter(yieldLabel, resultSlot, exceptionSlot);
            Expression rewrittenBody = rewriter.Visit(Body);

            // Capture whatever the body evaluates to into returnSlot.
            Expression captureReturn;
            if (Body.Type == typeof(void)) {
                captureReturn = rewrittenBody;
            } else {
                Expression asObject = rewrittenBody.Type == typeof(object)
                    ? rewrittenBody
                    : Expression.Convert(rewrittenBody, typeof(object));
                captureReturn = Expression.Assign(
                    Expression.Field(returnSlot, nameof(StrongBox<object?>.Value)),
                    asObject);
            }
            Expression generatorBody = Expression.Block(typeof(void), captureReturn);

            Expression generator = Utils.Generator(
                Name ?? "$async",
                yieldLabel,
                generatorBody,
                typeof(IEnumerator<object>),
                rewriteAssignments: false);

            Expression drive = Expression.Call(
                typeof(AsyncRunner).GetMethod(nameof(AsyncRunner.Drive))!,
                generator,
                resultSlot,
                returnSlot,
                exceptionSlot,
                CancellationToken);

            return Expression.Block(
                typeof(Task<object?>),
                new[] { resultSlot, exceptionSlot, returnSlot },
                Expression.Assign(resultSlot, Expression.New(typeof(StrongBox<object?>))),
                Expression.Assign(exceptionSlot, Expression.New(typeof(StrongBox<Exception?>))),
                Expression.Assign(returnSlot, Expression.New(typeof(StrongBox<object?>))),
                drive);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression b = visitor.Visit(Body);
            Expression ct = visitor.Visit(CancellationToken);
            if (b == Body && ct == CancellationToken) return this;
            return new AsyncBodyExpression(Name, b, ct);
        }

        private sealed class AwaitToYieldRewriter : ExpressionVisitor {
            private static readonly MethodInfo s_captureMethod
                = typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Capture))!;
            private static readonly MethodInfo s_throwMethod
                = typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Throw), Type.EmptyTypes)!;

            private readonly LabelTarget _yieldLabel;
            private readonly ParameterExpression _resultSlot;
            private readonly ParameterExpression _exceptionSlot;

            public AwaitToYieldRewriter(LabelTarget yieldLabel, ParameterExpression resultSlot, ParameterExpression exceptionSlot) {
                _yieldLabel = yieldLabel;
                _resultSlot = resultSlot;
                _exceptionSlot = exceptionSlot;
            }

            protected override Expression VisitExtension(Expression node) {
                if (node is AwaitExpression aw) {
                    Expression operand = Visit(aw.Operand);
                    Expression boxed = operand.Type == typeof(object) ? operand : Expression.Convert(operand, typeof(object));
                    Expression readException = Expression.Field(_exceptionSlot, nameof(StrongBox<Exception?>.Value));
                    Expression readSlot = Expression.Field(_resultSlot, nameof(StrongBox<object?>.Value));

                    // After the yield, if the runner stored an exception, rethrow it
                    // preserving the original stack trace (so the body's try/except
                    // observes the right exception object). Otherwise return the
                    // awaited result.
                    Expression rethrow = Expression.IfThen(
                        Expression.ReferenceNotEqual(readException, Expression.Constant(null, typeof(Exception))),
                        Expression.Call(
                            Expression.Call(s_captureMethod, readException),
                            s_throwMethod));

                    return Expression.Block(
                        typeof(object),
                        Utils.YieldReturn(_yieldLabel, boxed),
                        rethrow,
                        readSlot);
                }
                return base.VisitExtension(node);
            }
        }
    }

    public partial class Utils {
        /// <summary>
        /// Wraps an async-function body in an <see cref="AsyncBodyExpression"/>.
        /// The body may contain <see cref="AwaitExpression"/> suspension points
        /// and should evaluate to <see cref="object"/>; the resulting expression
        /// evaluates to <c>Task&lt;object&gt;</c>. Cancellation defaults to
        /// <c>default(CancellationToken)</c>; use the
        /// <see cref="AsyncBody(string, Expression, Expression)"/> overload to
        /// supply one.
        /// </summary>
        public static AsyncBodyExpression AsyncBody(string? name, Expression body) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            return new AsyncBodyExpression(name, body, Expression.Default(typeof(CancellationToken)));
        }

        /// <summary>
        /// Wraps an async-function body in an <see cref="AsyncBodyExpression"/>
        /// with a caller-provided <see cref="System.Threading.CancellationToken"/>.
        /// The token expression is evaluated once when the body starts and is
        /// then sampled by <see cref="AsyncRunner.Drive"/> between iterations
        /// and at each suspended await.
        /// </summary>
        public static AsyncBodyExpression AsyncBody(string? name, Expression body, Expression cancellationToken) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            ContractUtils.RequiresNotNull(cancellationToken, nameof(cancellationToken));
            if (cancellationToken.Type != typeof(CancellationToken)) {
                throw new ArgumentException(
                    $"Expression must evaluate to {nameof(CancellationToken)}, got {cancellationToken.Type}.",
                    nameof(cancellationToken));
            }
            return new AsyncBodyExpression(name, body, cancellationToken);
        }
    }
}
