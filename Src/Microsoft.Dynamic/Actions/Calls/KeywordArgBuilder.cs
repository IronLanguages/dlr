// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// ArgBuilder which provides a value for a keyword argument.  
    /// 
    /// The KeywordArgBuilder calculates its position at emit time using it's initial 
    /// offset within the keyword arguments, the number of keyword arguments, and the 
    /// total number of arguments provided by the user.  It then delegates to an 
    /// underlying ArgBuilder which only receives the single correct argument.
    /// 
    /// Delaying the calculation of the position to emit time allows the method binding to be 
    /// done without knowing the exact the number of arguments provided by the user. Hence,
    /// the method binder can be dependent only on the set of method overloads and keyword names,
    /// but not the user arguments. While the number of user arguments could be determined
    /// upfront, the current MethodBinder does not have this design.
    /// </summary>
    internal sealed class KeywordArgBuilder : ArgBuilder {
        private readonly int _kwArgCount, _kwArgIndex;
        private readonly ArgBuilder _builder;

        public KeywordArgBuilder(ArgBuilder builder, int kwArgCount, int kwArgIndex) 
            : base(builder.ParameterInfo) {

            Debug.Assert(BuilderExpectsSingleParameter(builder));
            Debug.Assert(builder.ConsumedArgumentCount == 1);
            _builder = builder;

            Debug.Assert(kwArgIndex < kwArgCount);
            _kwArgCount = kwArgCount;
            _kwArgIndex = kwArgIndex;
        }

        public override int Priority => _builder.Priority;

        public override int ConsumedArgumentCount => 1;

        /// <summary>
        /// The underlying builder should expect a single parameter as KeywordArgBuilder is responsible
        /// for calculating the correct parameter to use
        /// </summary>
        /// <param name="builder"></param>
        internal static bool BuilderExpectsSingleParameter(ArgBuilder builder) {
            return ((SimpleArgBuilder)builder).Index == 0;
        }

        protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            Debug.Assert(BuilderExpectsSingleParameter(_builder));

            int index = GetKeywordIndex(args.Length);
            Debug.Assert(!hasBeenUsed[index]);
            hasBeenUsed[index] = true;
            return _builder.ToExpression(resolver, MakeRestrictedArg(args, index), new bool[1]);
        }

        public override Type Type => _builder.Type;

        internal override Expression ToReturnExpression(OverloadResolver resolver) {
            return _builder.ToReturnExpression(resolver);
        }

        internal override Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args) {
            int index = GetKeywordIndex(args.Length);
            return _builder.UpdateFromReturn(resolver, MakeRestrictedArg(args, index));
        }

        private static RestrictedArguments MakeRestrictedArg(RestrictedArguments args, int index) {
            return new RestrictedArguments(new[] { args.GetObject(index) }, new[] { args.GetType(index) }, false);
        }

        private int GetKeywordIndex(int paramCount) {
            return paramCount - _kwArgCount + _kwArgIndex;
        }

        internal override Expression ByRefArgument => _builder.ByRefArgument;

        public override ArgBuilder Clone(ParameterInfo newType) {
            return new KeywordArgBuilder(_builder.Clone(newType), _kwArgCount, _kwArgIndex);
        }
    }
}
