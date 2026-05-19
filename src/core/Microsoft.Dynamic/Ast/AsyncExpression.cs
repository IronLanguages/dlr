// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq.Expressions;
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
    ///   The body is expected to evaluate to an <see cref="object"/> (can be null):
    ///   that value becomes the Task's result.
    ///   <br/>
    ///   State-machine splitting at await sites is delegated to <see cref="GeneratorExpression"/>
    ///   via <see cref="AsyncRewriter"/>; the <c>await</c> handling comes from
    ///   <see cref="AsyncHelpers.DriveAsync"/>.
    /// </remarks>
    public sealed class AsyncExpression : Expression {
        private Expression? _reduced;

        internal AsyncExpression(string? name, Expression body, Expression cancellationToken) {
            Name = name;
            Body = body;
            CancellationToken = cancellationToken;
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

        public override bool CanReduce => true;

        public override Type Type => typeof(Task<object?>);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Expression Reduce() {
            return _reduced ??= new AsyncRewriter(this).Reduce();
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression b = visitor.Visit(Body);
            Expression ct = visitor.Visit(CancellationToken);
            if (b == Body && ct == CancellationToken) return this;
            return new AsyncExpression(Name, b, ct);
        }
    }

    public partial class Utils {
        /// <summary>
        ///   Wraps an async-function body in an <see cref="AsyncExpression"/>.
        /// </summary>
        /// <remarks>
        ///   The body may contain <see cref="AwaitExpression"/> suspension points and should evaluate to <see cref="object"/>; the
        ///   resulting expression evaluates to <c>Task&lt;object&gt;</c>. Cancellation defaults to <c>default(CancellationToken)</c>; use
        ///   the <see cref="Async(string, Expression, Expression)"/> overload to supply one.
        /// </remarks>
        public static AsyncExpression Async(string? name, Expression body) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            return new AsyncExpression(name, body, Expression.Default(typeof(CancellationToken)));
        }

        /// <summary>
        ///   Wraps an async-function body in an <see cref="AsyncExpression"/> with a caller-provided <see
        ///   cref="System.Threading.CancellationToken"/>.
        /// </summary>
        /// <remarks>
        ///   The token expression is evaluated once when the body starts and is then sampled by <see cref="AsyncHelpers.DriveAsync"/>
        ///   between iterations and at each suspended await.
        /// </remarks>
        public static AsyncExpression Async(string? name, Expression body, Expression cancellationToken) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            ContractUtils.RequiresNotNull(cancellationToken, nameof(cancellationToken));
            if (cancellationToken.Type != typeof(CancellationToken)) {
                throw new ArgumentException(
                    $"Expression must evaluate to {nameof(CancellationToken)}, got {cancellationToken.Type}.",
                    nameof(cancellationToken));
            }
            return new AsyncExpression(name, body, cancellationToken);
        }
    }
}
