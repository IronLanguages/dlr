// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

#if NET

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Runtime;
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
                                           Expression? interrupts = null,
                                           Expression? observer = null,
                                           Expression? cancellationToken = null,
                                           DebugContext? debugContext = null,
                                           DebugLambdaInfo? debugLambdaInfo = null) {
            Name = name;
            Body = body;
            YieldLabel = yieldLabel;
            Interrupts = interrupts ?? Utils.DefaultInterrupts;
            Observer = observer ?? Utils.DefaultObserver;
            CancellationToken = cancellationToken ?? Utils.DefaultCancellationToken;
            DebugContext = debugContext;
            DebugLambdaInfo = debugLambdaInfo;
        }

        /// <summary>
        ///   Optional tracing hook; see <see cref="AsyncExpression.DebugContext"/>. When both are non-null,
        ///   <see cref="Reduce"/> instruments the body with debug points so it becomes traceable.
        /// </summary>
        internal DebugContext? DebugContext { get; }

        internal DebugLambdaInfo? DebugLambdaInfo { get; }

        /// <summary>
        ///   Returns a copy of this node that instruments its body for tracing under the given debug context.
        /// </summary>
        public AsyncEnumerableExpression WithDebugInfo(DebugContext debugContext, DebugLambdaInfo debugLambdaInfo) {
            ContractUtils.RequiresNotNull(debugContext, nameof(debugContext));
            ContractUtils.RequiresNotNull(debugLambdaInfo, nameof(debugLambdaInfo));
            return new AsyncEnumerableExpression(Name, Body, YieldLabel, Interrupts, Observer, CancellationToken, debugContext, debugLambdaInfo);
        }

        /// <summary>The observer expression set by <see cref="WithObserver"/>; defaults to a null constant.</summary>
        internal Expression Observer { get; }

        /// <summary>
        ///   Returns a copy of this node that notifies an observer as the body runs (see <see cref="AsyncExpression.WithObserver"/>).
        /// </summary>
        /// <param name="observer">
        ///   Expression evaluating to an <see cref="IObserver{T}"/> of <see cref="AsyncBodyEvent"/> (null allowed), evaluated each time
        ///   the node is evaluated.
        /// </param>
        /// <exception cref="InvalidOperationException">The node already has an observer.</exception>
        public AsyncEnumerableExpression WithObserver(Expression observer) {
            ContractUtils.RequiresNotNull(observer, nameof(observer));
            ContractUtils.RequiresReferenceAssignable(observer, typeof(IObserver<AsyncBodyEvent>), nameof(observer));
            if (Utils.HasObserver(Observer)) throw new InvalidOperationException("The async enumerable expression already has an observer.");
            return new AsyncEnumerableExpression(Name, Body, YieldLabel, Interrupts, observer, CancellationToken, DebugContext, DebugLambdaInfo);
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

        /// <summary>
        ///   Expression evaluating to an <see cref="AsyncInterruptSource"/> (or null), a new one each time the node is evaluated
        ///   (see <see cref="AsyncExpression.Interrupts"/>). Default null.
        /// </summary>
        public Expression Interrupts { get; }

        /// <summary>Expression evaluating to the cancellation token (see <see cref="AsyncExpression"/>). Default <c>default(CancellationToken)</c>.</summary>
        public Expression CancellationToken { get; }

        public override bool CanReduce => true;

        public override Type Type => typeof(IAsyncEnumerable<object?>);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Expression Reduce() {
            return _reduced ??= new AsyncEnumerableRewriter(this).Reduce();
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression b = visitor.Visit(Body);
            Expression i = visitor.Visit(Interrupts);
            Expression o = visitor.Visit(Observer);
            Expression ct = visitor.Visit(CancellationToken);
            if (b == Body && i == Interrupts && o == Observer && ct == CancellationToken) return this;
            return new AsyncEnumerableExpression(Name, b, YieldLabel, i, o, ct, DebugContext, DebugLambdaInfo);
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
        ///   with an interrupt source and, optionally, a caller-provided <see cref="System.Threading.CancellationToken"/>.
        /// </summary>
        /// <remarks>
        ///   <paramref name="interrupts"/> evaluates to an <see cref="AsyncInterruptSource"/>, a new one each time the node is evaluated,
        ///   or to null. It lets a host raise arbitrary exceptions inside the body at its awaits, any number of times.
        ///   <paramref name="cancellationToken"/> defaults to <c>default(CancellationToken)</c>.
        /// </remarks>
        /// <param name="yieldLabel">
        ///   It must be the same label the body's language-level <c>yield</c>s target.
        /// </param>
        public static AsyncEnumerableExpression AsyncEnumerable(string? name, Expression body, LabelTarget yieldLabel,
                                                                Expression? interrupts,
                                                                Expression? cancellationToken = null) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            ContractUtils.RequiresNotNull(yieldLabel, nameof(yieldLabel));
            if (interrupts is not null) {
                ContractUtils.RequiresType(interrupts, typeof(AsyncInterruptSource), nameof(interrupts));
            }
            if (cancellationToken is not null) {
                ContractUtils.RequiresType(cancellationToken, typeof(CancellationToken), nameof(cancellationToken));
            }
            return new AsyncEnumerableExpression(name, body, yieldLabel, interrupts, cancellationToken: cancellationToken);
        }
    }
}

#endif
