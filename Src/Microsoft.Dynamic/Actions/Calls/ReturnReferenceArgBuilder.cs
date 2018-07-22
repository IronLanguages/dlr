// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls {

    /// <summary>
    /// Builds a parameter for a reference argument when a StrongBox has not been provided.  The
    /// updated return value is returned as one of the resulting return values.
    /// </summary>
    internal sealed class ReturnReferenceArgBuilder : SimpleArgBuilder {
        private ParameterExpression _tmp;

        public ReturnReferenceArgBuilder(ParameterInfo info, int index)
            : base(info, info.ParameterType.GetElementType(), index, false, false) {
        }

        protected override SimpleArgBuilder Copy(int newIndex) {
            return new ReturnReferenceArgBuilder(ParameterInfo, newIndex);
        }

        public override ArgBuilder Clone(ParameterInfo newType) {
            return new ReturnReferenceArgBuilder(newType, Index);
        }

        protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            if (_tmp == null) {
                _tmp = resolver.GetTemporary(Type, "outParam");
            }

            return Expression.Block(Expression.Assign(_tmp, base.ToExpression(resolver, args, hasBeenUsed)), _tmp);
        }

        internal override Expression ToReturnExpression(OverloadResolver resolver) {
            return _tmp;
        }

        internal override Expression ByRefArgument => _tmp;

        public override int Priority => 5;
    }
}
