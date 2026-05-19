// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq.Expressions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    ///   A suspension point inside an <see cref="AsyncLambdaExpression"/>. The
    ///   <see cref="Operand"/> is expected to evaluate to a <see cref="System.Threading.Tasks.Task"/>
    ///   or <see cref="System.Threading.Tasks.Task{TResult}"/>.
    /// </summary>
    /// <remarks>
    ///   The <see cref="AsyncRunner"/> drives
    ///   the lambda by calling its <c>GeneratorNext</c> form; each await yields the
    ///   task to the runner, which performs a real runtime-async <c>await</c> and
    ///   resumes the body with the boxed result.
    ///   <br/>
    ///   Standalone reduction is not supported - <see cref="AsyncLambdaExpression"/>
    ///   rewrites these nodes into yield+resume pairs before the body is lowered.
    /// </remarks>
    public sealed class AwaitExpression : Expression {
        internal AwaitExpression(Expression operand) {
            Operand = operand;
        }

        /// <summary>The awaitable being awaited. Must produce a Task or Task&lt;T&gt;.</summary>
        public Expression Operand { get; }

        public override bool CanReduce => false;

        public override Type Type => typeof(object);

        public override ExpressionType NodeType => ExpressionType.Extension;

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression o = visitor.Visit(Operand);
            if (o == Operand) return this;
            return new AwaitExpression(o);
        }
    }

    public partial class Utils {
        /// <summary>Wraps <paramref name="awaitable"/> in an <see cref="AwaitExpression"/>.</summary>
        public static AwaitExpression Await(Expression awaitable) {
            ContractUtils.RequiresNotNull(awaitable, nameof(awaitable));
            return new AwaitExpression(awaitable);
        }
    }
}
