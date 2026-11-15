// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Scripting.Debugging.CompilerServices;
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
                                  Expression? interrupts = null,
                                  Expression? observer = null,
                                  Expression? cancellationToken = null,
                                  DebugContext? debugContext = null,
                                  DebugLambdaInfo? debugLambdaInfo = null) {
            Name = name;
            Body = body;
            Interrupts = interrupts ?? Utils.DefaultInterrupts;
            Observer = observer ?? Utils.DefaultObserver;
            CancellationToken = cancellationToken ?? Utils.DefaultCancellationToken;
            DebugContext = debugContext;
            DebugLambdaInfo = debugLambdaInfo;
        }

        /// <summary>
        ///   Optional tracing hook. When both are non-null, <see cref="Reduce"/> instruments the body with debug
        ///   points so it becomes traceable (see <c>AsyncDebugSupport</c>); when null, reduction is unchanged.
        ///   Set by the host (e.g. IronPython's <c>FunctionCode</c>) only when building the tracing variant.
        /// </summary>
        internal DebugContext? DebugContext { get; }

        internal DebugLambdaInfo? DebugLambdaInfo { get; }

        /// <summary>
        ///   Returns a copy of this node that instruments its body for tracing under the given debug context.
        /// </summary>
        public AsyncExpression WithDebugInfo(DebugContext debugContext, DebugLambdaInfo debugLambdaInfo) {
            ContractUtils.RequiresNotNull(debugContext, nameof(debugContext));
            ContractUtils.RequiresNotNull(debugLambdaInfo, nameof(debugLambdaInfo));
            return new AsyncExpression(Name, Body, Interrupts, Observer, CancellationToken, debugContext, debugLambdaInfo);
        }

        /// <summary>
        ///   The observer expression set by <see cref="WithObserver"/>; defaults to a null constant.
        /// </summary>
        internal Expression Observer { get; }

        /// <summary>
        ///   Returns a copy of this node that notifies an observer as the body runs: when it starts, suspends, resumes and returns,
        ///   and of its outcome. The calls for each body follow the contract described on <see cref="AsyncBodyEvent"/>.
        /// </summary>
        /// <param name="observer">
        ///   Expression evaluating to an <see cref="IObserver{T}"/> of <see cref="AsyncBodyEvent"/> (null allowed). It is evaluated each
        ///   time the node is evaluated, so it typically produces a new observer each time.
        /// </param>
        /// <exception cref="InvalidOperationException">The node already has an observer.</exception>
        public AsyncExpression WithObserver(Expression observer) {
            ContractUtils.RequiresNotNull(observer, nameof(observer));
            ContractUtils.RequiresReferenceAssignable(observer, typeof(IObserver<AsyncBodyEvent>), nameof(observer));
            if (Utils.HasObserver(Observer)) throw new InvalidOperationException("The async expression already has an observer.");
            return new AsyncExpression(Name, Body, Interrupts, observer, CancellationToken, DebugContext, DebugLambdaInfo);
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
        ///   Expression evaluating to an <see cref="AsyncInterruptSource"/> (null allowed) that raises exceptions inside the body at its
        ///   awaits, any number of times. It must produce a new source each time the node is evaluated. Defaults to a null constant.
        /// </summary>
        public Expression Interrupts { get; }

        /// <summary>
        ///   Expression evaluating to a <see cref="System.Threading.CancellationToken"/> that <see cref="AsyncHelpers.DriveAsync"/> samples
        ///   between iterations and links to each suspended task. Defaults to <c>default(CancellationToken)</c>.
        /// </summary>
        public Expression CancellationToken { get; }

        public override bool CanReduce => true;

        public override Type Type => typeof(Task<object?>);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Expression Reduce() {
            return _reduced ??= new AsyncRewriter(this).Reduce();
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression b = visitor.Visit(Body);
            Expression i = visitor.Visit(Interrupts);
            Expression o = visitor.Visit(Observer);
            Expression ct = visitor.Visit(CancellationToken);
            if (b == Body && i == Interrupts && o == Observer && ct == CancellationToken) return this;
            return new AsyncExpression(Name, b, i, o, ct, DebugContext, DebugLambdaInfo);
        }
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
        ///   Wraps an async-function body in an <see cref="AsyncExpression"/> with an interrupt source and, optionally, a
        ///   caller-provided <see cref="System.Threading.CancellationToken"/>.
        /// </summary>
        /// <remarks>
        ///   <paramref name="interrupts"/> evaluates to an <see cref="AsyncInterruptSource"/>, a new one each time the node is evaluated,
        ///   or to null. It lets a host raise arbitrary exceptions inside the body, any number of times (e.g. for Python's
        ///   <c>coro.throw(exc)</c>). <paramref name="cancellationToken"/> defaults to <c>default(CancellationToken)</c>.
        /// </remarks>
        public static AsyncExpression Async(string? name, Expression body,
                                            Expression? interrupts,
                                            Expression? cancellationToken = null) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            if (interrupts is not null) {
                ContractUtils.RequiresType(interrupts, typeof(AsyncInterruptSource), nameof(interrupts));
            }
            if (cancellationToken is not null) {
                ContractUtils.RequiresType(cancellationToken, typeof(CancellationToken), nameof(cancellationToken));
            }
            return new AsyncExpression(name, body, interrupts, cancellationToken: cancellationToken);
        }
    }
}
