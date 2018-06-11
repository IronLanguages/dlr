/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Linq.Expressions;

using System;
using System.Dynamic;
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    using AstUtils = Microsoft.Scripting.Ast.Utils;

    /// <summary>
    /// Wraps all arguments passed to a dynamic site with more arguments than can be accepted by a Func/Action delegate.
    /// The binder generating a rule for such a site should unwrap the arguments first and then perform a binding to them.
    /// </summary>
    public sealed class ArgumentArray {
        private readonly object[] _arguments;

        // the index of the first item _arguments that represents an argument:
        private readonly int _first;

        internal ArgumentArray(object[] arguments, int first, int count) {
            _arguments = arguments;
            _first = first;
            Count = count;
        }

        /// <summary>
        /// Gets the number of items in _arguments that represent the arguments.
        /// </summary>
        public int Count { get; }

        public object GetArgument(int index) {
            ContractUtils.RequiresArrayIndex(_arguments, index, nameof(index));
            return _arguments[_first + index];
        }

        public DynamicMetaObject GetMetaObject(Expression parameter, int index) {
            return DynamicMetaObject.Create(
                GetArgument(index),
                Expression.Call(
                    _GetArgMethod, 
                    AstUtils.Convert(parameter, typeof(ArgumentArray)),
                    AstUtils.Constant(index)
                )
            );
        }

        [CLSCompliant(false)]
        public static object GetArg(ArgumentArray array, int index) {
            return array._arguments[array._first + index];
        }

        private static readonly MethodInfo _GetArgMethod = new Func<ArgumentArray, int, object>(GetArg).GetMethodInfo();
    }
}
