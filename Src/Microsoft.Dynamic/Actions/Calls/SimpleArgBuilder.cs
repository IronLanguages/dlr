/* ****************************************************************************
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using Microsoft.Scripting.Generation;
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

        public override int ConsumedArgumentCount {
            get { return 1; }
        }

        public override int Priority {
            get { return 0; }
        }

        public bool IsParamsArray {
            get { return _isParams; }
        }

        public bool IsParamsDict {
            get { return _isParamsDict; }
        }

        internal protected override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            Debug.Assert(hasBeenUsed.Length == args.Length);
            Debug.Assert(_index < args.Length);
            Debug.Assert(!hasBeenUsed[Index]);
            
            hasBeenUsed[_index] = true;
            return resolver.Convert(args.GetObject(_index), args.GetType(_index), ParameterInfo, _parameterType);
        }

        public int Index {
            get {
                return _index;
            }
        }

        public override Type Type {
            get {
                return _parameterType;
            }
        }

        public override ArgBuilder Clone(ParameterInfo newType) {
            return new SimpleArgBuilder(newType, newType.ParameterType, _index, _isParams, _isParamsDict);
        }
    }
}
