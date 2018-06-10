﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Linq.Expressions;

using System;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    /// Represents either a YieldBreak or YieldReturn in a GeneratorExpression
    /// If Value is non-null, it's a YieldReturn; otherwise it's a YieldBreak
    /// and executing it will stop enumeration of the generator, causing
    /// MoveNext to return false.
    /// </summary>
    public sealed class YieldExpression : Expression {
        private readonly Expression _value;

        internal YieldExpression(LabelTarget target, Expression value, int yieldMarker) {
            Target = target;
            _value = value;
            YieldMarker = yieldMarker;
        }

        public override bool CanReduce {
            get { return false; }
        }

        public sealed override Type Type {
            get { return typeof(void); }
        }

        public sealed override ExpressionType NodeType {
            get { return ExpressionType.Extension; }
        }

        /// <summary>
        /// The value yieled from this expression, if it is a yield return
        /// </summary>
        public Expression Value {
            get { return _value; }
        }

        /// <summary>
        /// Gets the label used to yield from this generator
        /// </summary>
        public LabelTarget Target { get; }

        public int YieldMarker { get; }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression v = visitor.Visit(_value);
            if (v == _value) {
                return this;
            }
            return Utils.MakeYield(Target, v, YieldMarker);
        }
    }

    public partial class Utils {
        public static YieldExpression YieldBreak(LabelTarget target) {
            return MakeYield(target, null, -1);
        }
        public static YieldExpression YieldReturn(LabelTarget target, Expression value) {
            return MakeYield(target, value, -1);
        }
        public static YieldExpression YieldReturn(LabelTarget target, Expression value, int yieldMarker) {
            ContractUtils.RequiresNotNull(value, nameof(value));
            return MakeYield(target, value, yieldMarker);
        }
        public static YieldExpression MakeYield(LabelTarget target, Expression value, int yieldMarker) {
            ContractUtils.RequiresNotNull(target, nameof(target));
            ContractUtils.Requires(target.Type != typeof(void), "target", "generator label must have a non-void type");
            if (value != null) {
                if (!TypeUtils.AreReferenceAssignable(target.Type, value.Type)) {
                    // C# autoquotes generator return values
                    if (target.Type.IsSubclassOf(typeof(Expression)) &&
                        TypeUtils.AreAssignable(target.Type, value.GetType())) {
                        value = Expression.Quote(value);
                    }
                    throw new ArgumentException(string.Format("Expression of type '{0}' cannot be yielded to a generator label of type '{1}'", value.Type, target.Type));
                }
            }

            return new YieldExpression(target, value, yieldMarker);
        }
    }
}
