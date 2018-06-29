// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Linq.Expressions;

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Interpreter;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    /// Expression which produces a light exception value.  This should be constructed
    /// with the expression which creates the exception and this method will then call
    /// a helper method which wraps the exception in our internal light exception class.
    /// </summary>
    class LightThrowExpression : Expression {
        private readonly Expression _exception;
        private static MethodInfo _throw = new Func<Exception, object>(LightExceptions.Throw).GetMethodInfo();

        public LightThrowExpression(Expression exception) {
            _exception = exception;
        }

        public override Expression Reduce() {
            return Call(_throw, _exception);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            var exception = visitor.Visit(_exception);
            if (exception != _exception) {
                return new LightThrowExpression(exception);
            }

            return this;
        }

        public override bool CanReduce {
            get {
                return true;
            }
        }

        public override ExpressionType NodeType {
            get {
                return ExpressionType.Extension;
            }
        }

        public override Type Type {
            get { return typeof(object); }
        }
    }
}
