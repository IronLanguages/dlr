// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

#if NET

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    ///   Wraps an async-generator body (one that contains both <see cref="AwaitExpression"/> nodes and
    ///   <see cref="YieldExpression"/> nodes targeting <see cref="YieldLabel"/>) into an expression that
    ///   evaluates to <see cref="IAsyncEnumerable{T}"/> of <see cref="object"/>.
    /// </summary>
    /// <remarks>
    ///   Await points are rewritten to <c>yield AwaitPoint(task)</c> against the <em>same</em> label as the
    ///   language-level <c>yield</c>s, so a single <c>GeneratorRewriter</c>-produced
    ///   <see cref="IEnumerator{T}"/> carries both kinds of items.
    ///   <see cref="Microsoft.Scripting.Runtime.AsyncHelpers.DriveAsyncEnumerable"/> then awaits
    ///   <see cref="Microsoft.Scripting.Runtime.AwaitPoint"/> items internally and emits the rest to the
    ///   consumer. This marker is what lets <c>await</c> and <c>yield</c> coexist: a yielded Task is not an
    ///   AwaitPoint, so it is surfaced as a value rather than awaited.
    /// </remarks>
    public sealed class AsyncEnumerableExpression : Expression {
        private Expression? _reduced;

        internal AsyncEnumerableExpression(string? name, Expression body, LabelTarget yieldLabel,
                                           Expression? cancellationToken = null,
                                           Expression? cancellationException = null) {
            Name = name;
            Body = body;
            YieldLabel = yieldLabel;
            CancellationToken = cancellationToken ?? Expression.Default(typeof(CancellationToken));
            CancellationException = cancellationException ?? Expression.Constant(null, typeof(StrongBox<Exception?>));
        }

        /// <summary>Optional diagnostic name (forwarded to the inner generator).</summary>
        public string? Name { get; }

        /// <summary>The generator body. May contain <see cref="AwaitExpression"/> and <see cref="YieldExpression"/> nodes.</summary>
        public Expression Body { get; }

        /// <summary>
        ///   The label both the language-level <c>yield</c>s and the rewritten <c>await</c>s target, so they
        ///   land in one generator. Supplied by the host (e.g. IronPython's shared generator label).
        /// </summary>
        public LabelTarget YieldLabel { get; }

        /// <summary>Expression evaluating to the cancellation token (see <see cref="AsyncExpression"/>). Default <c>default(CancellationToken)</c>.</summary>
        public Expression CancellationToken { get; }

        /// <summary>Expression evaluating to a <c>StrongBox&lt;Exception?&gt;</c> exception override (or null). Default null.</summary>
        public Expression CancellationException { get; }

        public override bool CanReduce => true;

        public override Type Type => typeof(IAsyncEnumerable<object?>);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Expression Reduce() {
            return _reduced ??= new AsyncEnumerableRewriter(this).Reduce();
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression b = visitor.Visit(Body);
            Expression ct = visitor.Visit(CancellationToken);
            Expression ce = visitor.Visit(CancellationException);
            if (b == Body && ct == CancellationToken && ce == CancellationException) return this;
            return new AsyncEnumerableExpression(Name, b, YieldLabel, ct, ce);
        }
    }

    public partial class Utils {
        /// <summary>
        ///   Wraps an async-generator body in an <see cref="AsyncEnumerableExpression"/> producing <c>IAsyncEnumerable&lt;object&gt;</c>.
        /// </summary>
        /// <param name="yieldLabel">
        ///   It must be the same label the body's language-level <c>yield</c>s target.
        /// </param>
        public static AsyncEnumerableExpression AsyncEnumerable(string? name, Expression body, LabelTarget yieldLabel) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            ContractUtils.RequiresNotNull(yieldLabel, nameof(yieldLabel));
            return new AsyncEnumerableExpression(name, body, yieldLabel);
        }

        /// <summary>
        ///   Wraps an async-generator body in an <see cref="AsyncEnumerableExpression"/> producing <c>IAsyncEnumerable&lt;object&gt;</c>,
        ///   with a caller-provided <see cref="System.Threading.CancellationToken"/> and, optionally, an exception-override box.
        /// </summary>
        /// <remarks>
        ///   When cancellation fires and the box's <c>Value</c> is non-null, that exception is delivered to
        ///   the body instead of a fresh <see cref="System.OperationCanceledException"/>. This lets a host inject
        ///   an arbitrary exception (e.g. Python's <c>coro.throw(exc)</c>) by populating the box and then
        ///   cancelling the token. <paramref name="cancellationException"/> defaults to <c>null</c>
        ///   — the plain OCE-on-cancellation behavior.
        /// </remarks>
        /// <param name="yieldLabel">
        ///   It must be the same label the body's language-level <c>yield</c>s target.
        /// </param>
        public static AsyncEnumerableExpression AsyncEnumerable(string? name, Expression body, LabelTarget yieldLabel,
                                                                Expression cancellationToken,
                                                                Expression? cancellationException = null) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            ContractUtils.RequiresNotNull(yieldLabel, nameof(yieldLabel));
            RequireType(cancellationToken, typeof(CancellationToken), nameof(cancellationToken));
            if (cancellationException is not null) {
                RequireType(cancellationException, typeof(StrongBox<Exception?>), nameof(cancellationException));
            }
            return new AsyncEnumerableExpression(name, body, yieldLabel, cancellationToken, cancellationException);
        }
    }
}

#endif
