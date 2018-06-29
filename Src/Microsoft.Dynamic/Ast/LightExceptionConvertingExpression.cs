// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Linq.Expressions;

using System;
using System.Reflection;
using Microsoft.Scripting.Utils;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Ast {
    internal class LightExceptionConvertingExpression : Expression, ILightExceptionAwareExpression {
        private readonly Expression _expr;
        private readonly bool _supportsLightEx;

        internal LightExceptionConvertingExpression(Expression expr, bool supportsLightEx) {
            _expr = expr;
            _supportsLightEx = supportsLightEx;
        }

        public override bool CanReduce {
            get { return true; }
        }

        public override ExpressionType NodeType {
            get { return ExpressionType.Extension; }
        }

        public override Type Type {
            get { return _expr.Type; }
        }

        public override Expression Reduce() {
            return new LightExceptionRewriter().Rewrite(_expr);
        }        

        #region ILightExceptionAwareExpression Members

        Expression ILightExceptionAwareExpression.ReduceForLightExceptions() {
            if (_supportsLightEx) {
                return _expr;
            }
            return Reduce();
        }

        #endregion

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            var expr = visitor.Visit(_expr);
            if (expr != _expr) {
                return new LightExceptionConvertingExpression(expr, _supportsLightEx);
            }
            return this;
        }
    }
}
