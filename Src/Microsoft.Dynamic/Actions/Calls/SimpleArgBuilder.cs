// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// SimpleArgBuilder produces the value produced by the user as the argument value.  It
    /// also tracks information about the original parameter and is used to create extended
    /// methods for params arrays and param dictionary functions.
    /// </summary>
    public class SimpleArgBuilder : ArgBuilder {
        // Index of actual argument expression.
        private int _index;

        private readonly Type _parameterType;
        private readonly bool _isParams, _isParamsDict;

        /// <summary>
        /// Parameter info is not available for this argument.
        /// </summary>
        public SimpleArgBuilder(Type parameterType, int index, bool isParams, bool isParamsDict)
            : this(null, parameterType, index, isParams, isParamsDict) {
        }

        /// <summary>
        /// Type and whether the parameter is a params-array or params-dictionary is derived from info.
        /// </summary>
        [Obsolete("Use other overload")]
        public SimpleArgBuilder(ParameterInfo info, int index)
            : this(info, info.ParameterType, index, info.IsParamArray(), info.IsParamDictionary()) {
        }
        
        public SimpleArgBuilder(ParameterInfo info, Type parameterType, int index, bool isParams, bool isParamsDict)
            : base(info) {
            ContractUtils.Requires(index >= 0);
            ContractUtils.RequiresNotNull(parameterType, nameof(parameterType));

            _index = index;
            _parameterType = parameterType;
            _isParams = isParams;
            _isParamsDict = isParamsDict;
        }

        internal SimpleArgBuilder MakeCopy(int newIndex) {
            var result = Copy(newIndex);
            // Copy() must be overriden in derived classes and return an instance of the derived class:
            Debug.Assert(result.GetType() == GetType());
            return result;
        }

        protected virtual SimpleArgBuilder Copy(int newIndex) {
            return new SimpleArgBuilder(ParameterInfo, _parameterType, newIndex, _isParams, _isParamsDict);
        }

        public override int ConsumedArgumentCount => 1;

        public override int Priority => 0;

        public bool IsParamsArray => _isParams;

        public bool IsParamsDict => _isParamsDict;

        protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            Debug.Assert(hasBeenUsed.Length == args.Length);
            Debug.Assert(_index < args.Length);
            Debug.Assert(!hasBeenUsed[Index]);
            
            hasBeenUsed[_index] = true;
            return resolver.Convert(args.GetObject(_index), args.GetType(_index), ParameterInfo, _parameterType);
        }

        public int Index => _index;

        public override Type Type => _parameterType;

        public override ArgBuilder Clone(ParameterInfo newType) {
            return new SimpleArgBuilder(newType, newType.ParameterType, _index, _isParams, _isParamsDict);
        }
    }
}
