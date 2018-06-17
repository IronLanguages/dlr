// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Interceptor prototype. The interceptor is a call site binder that wraps
    /// a real call site binder and can perform arbitrary operations on the expression
    /// trees that the wrapped binder produces:
    ///   * Dumping the trees
    ///   * Additional rewriting
    ///   * Static compilation
    ///   * ...
    /// </summary>
    public static class Interceptor {
        public static Expression Intercept(Expression expression) {
            InterceptorWalker iw = new InterceptorWalker();
            return iw.Visit(expression);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static LambdaExpression Intercept(LambdaExpression lambda) {
            InterceptorWalker iw = new InterceptorWalker();
            return iw.Visit(lambda) as LambdaExpression;
        }

        internal class InterceptorSiteBinder : CallSiteBinder {
            private readonly CallSiteBinder _binder;

            internal InterceptorSiteBinder(CallSiteBinder binder) {
                _binder = binder;
            }

            public override int GetHashCode() {
                return _binder.GetHashCode();
            }

            public override bool Equals(object obj) =>
                obj != null && obj.Equals(_binder);

            public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel) {
                Expression binding = _binder.Bind(args, parameters, returnLabel);

                //
                // TODO: Implement interceptor action here
                //

                //
                // Call interceptor recursively to continue intercepting on rules
                //
                return Intercept(binding);
            }
        }

        internal class InterceptorWalker : DynamicExpressionVisitor {
            protected override Expression VisitDynamic(DynamicExpression node) {
                CallSiteBinder binder = node.Binder;
                if (!(binder is InterceptorSiteBinder)) {
                    binder = new InterceptorSiteBinder(binder);
                    return DynamicExpression.MakeDynamic(node.DelegateType, binder, node.Arguments);
                }

                return node;
            }
        }
    }
}
