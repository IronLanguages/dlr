// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    ///   Wraps an async function body (possibly containing <see cref="AwaitExpression"/>
    ///   nodes) into an expression that evaluates to <see cref="Task{TResult}"/> of
    ///   <see cref="object"/>.
    /// </summary>
    /// <remarks>
    ///   Unlike <see cref="LambdaExpression"/>, this is a
    ///   sub-expression: it does not introduce a new lambda scope, so the body has
    ///   direct access to parameters and locals of the enclosing scope. Callers
    ///   typically wrap the resulting Task in their own coroutine façade type.
    ///   <br/>
    ///   The body may evaluate to any type, including <see langword="void"/>. A
    ///   <see langword="void"/> body produces a <see langword="null"/> Task result;
    ///   any other type is converted to <see cref="object"/> (value types are boxed)
    ///   and that value becomes the Task's result.
    ///   <br/>
    ///   State-machine splitting at await sites is delegated to <see cref="GeneratorExpression"/>
    ///   via <see cref="AsyncRewriter"/>; the <c>await</c> handling comes from
    ///   <see cref="AsyncHelpers.DriveAsync"/>.
    /// </remarks>
    public sealed class AsyncExpression : Expression {
        private Expression? _reduced;

        internal AsyncExpression(string? name, Expression body,
                                  Expression? cancellationToken = null,
                                  Expression? cancellationException = null) {
            Name = name;
            Body = body;
            CancellationToken = cancellationToken ?? DefaultCancellationToken;
            CancellationException = cancellationException ?? DefaultCancellationException;
        }

        /// <summary>
        ///   Optional diagnostic name (forwarded to the inner generator).
        /// </summary>
        public string? Name { get; }

        /// <summary>
        ///   The function body. May contain <see cref="AwaitExpression"/> nodes.
        /// </summary>
        public Expression Body { get; }

        /// <summary>
        ///   Expression evaluating to a <see cref="System.Threading.CancellationToken"/> that <see cref="AsyncHelpers.DriveAsync"/> samples
        ///   between iterations and links to each suspended task. Defaults to <c>default(CancellationToken)</c>.
        /// </summary>
        public Expression CancellationToken { get; }

        /// <summary>
        ///   Expression evaluating to a <c>StrongBox&lt;Exception?&gt;</c> (null allowed) — see
        ///   <see cref="AsyncHelpers.DriveAsync"/>'s <c>cancellationException</c> parameter. When the box is
        ///   non-null and its <c>Value</c> is non-null at cancellation time, that exception is surfaced to
        ///   the body instead of <see cref="System.OperationCanceledException"/>. Defaults to a null
        ///   constant (the plain-cancellation behavior).
        /// </summary>
        public Expression CancellationException { get; }

        public override bool CanReduce => true;

        public override Type Type => typeof(Task<object?>);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Expression Reduce() {
            return _reduced ??= new AsyncRewriter(this).Reduce();
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression b = visitor.Visit(Body);
            Expression ct = visitor.Visit(CancellationToken);
            Expression ce = visitor.Visit(CancellationException);
            if (b == Body && ct == CancellationToken && ce == CancellationException) return this;
            return new AsyncExpression(Name, b, ct, ce);
        }

        private static Expression DefaultCancellationException
            => Expression.Constant(null, typeof(StrongBox<Exception?>));

        private static Expression DefaultCancellationToken
            => Expression.Default(typeof(CancellationToken));
    }

    public partial class Utils {
        /// <summary>
        ///   Wraps an async-function body in an <see cref="AsyncExpression"/>.
        /// </summary>
        /// <remarks>
        ///   The body may contain <see cref="AwaitExpression"/> suspension points and may evaluate to any
        ///   type (including <see langword="void"/>): non-void values are converted to <see cref="object"/>
        ///   — value types are boxed — and become the result of the resulting <c>Task&lt;object&gt;</c>; a
        ///   void body produces a <see langword="null"/> result. Cancellation defaults to
        ///   <c>default(CancellationToken)</c>.
        /// </remarks>
        public static AsyncExpression Async(string? name, Expression body) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            return new AsyncExpression(name, body);
        }

        /// <summary>
        ///   Wraps an async-function body in an <see cref="AsyncExpression"/> with a caller-provided
        ///   <see cref="System.Threading.CancellationToken"/> and, optionally, an exception-override box.
        /// </summary>
        /// <remarks>
        ///   When cancellation fires and the box's <c>Value</c> is non-null, that exception is delivered to
        ///   the body instead of a fresh <see cref="System.OperationCanceledException"/>. This lets a host inject
        ///   an arbitrary exception (e.g. Python's <c>coro.throw(exc)</c>) by populating the box and then
        ///   cancelling the token. <paramref name="cancellationException"/> defaults to <c>null</c>
        ///   — the plain OCE-on-cancellation behavior.
        /// </remarks>
        public static AsyncExpression Async(string? name, Expression body,
                                            Expression cancellationToken,
                                            Expression? cancellationException = null) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            ContractUtils.RequiresNotNull(cancellationToken, nameof(cancellationToken));
            RequireType(cancellationToken, typeof(CancellationToken), nameof(cancellationToken));
            if (cancellationException is not null) {
                RequireType(cancellationException, typeof(StrongBox<Exception?>), nameof(cancellationException));
            }
            return new AsyncExpression(name, body, cancellationToken, cancellationException);
        }

        private static void RequireType(Expression expr, Type expected, string paramName) {
            if (expr.Type != expected) {
                throw new ArgumentException(
                    $"Expression must evaluate to {expected.Name}, got {expr.Type}.",
                    paramName);
            }
        }
    }
}
