// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;

using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    using Ast = Expression;

    internal sealed class ByRefReturnBuilder : ReturnBuilder {
        private readonly IList<int> _returnArgs;

        public ByRefReturnBuilder(IList<int> returnArgs)
            : base(typeof(object)) {
            _returnArgs = returnArgs;
        }

        internal override Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret) {
            if (_returnArgs.Count == 1) {
                if (_returnArgs[0] == -1) {
                    return ret;
                }
                return Ast.Block(ret, builders[_returnArgs[0]].ToReturnExpression(resolver));
            }

            Expression[] retValues = new Expression[_returnArgs.Count];
            int rIndex = 0;
            bool usesRet = false;
            foreach (int index in _returnArgs) {
                if (index == -1) {
                    usesRet = true;
                    retValues[rIndex++] = ret;
                } else {
                    retValues[rIndex++] = builders[index].ToReturnExpression(resolver);
                }
            }

            Expression retArray = AstUtils.NewArrayHelper(typeof(object), retValues);
            if (!usesRet) {
                retArray = Ast.Block(ret, retArray);
            }

            return resolver.GetByRefArrayExpression(retArray);
        }

        public override int CountOutParams => _returnArgs.Count;
    }
}
