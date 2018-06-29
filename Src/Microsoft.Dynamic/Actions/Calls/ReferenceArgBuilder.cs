// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using RuntimeHelpers = Microsoft.Scripting.Runtime.ScriptingRuntimeHelpers;

namespace Microsoft.Scripting.Actions.Calls {

    /// <summary>
    /// An argument that the user wants to explicitly pass by-reference (with copy-in copy-out semantics).
    /// The user passes a StrongBox[T] object whose value will get updated when the call returns.
    /// </summary>
    internal sealed class ReferenceArgBuilder : SimpleArgBuilder {
        private readonly Type _elementType;
        private ParameterExpression _tmp;

        public ReferenceArgBuilder(ParameterInfo info, Type elementType, Type strongBox, int index)
            : base(info, strongBox, index, false, false) {
            _elementType = elementType;
        }

        protected override SimpleArgBuilder Copy(int newIndex) {
            return new ReferenceArgBuilder(ParameterInfo, _elementType, Type, newIndex);
        }

        public override ArgBuilder Clone(ParameterInfo newType) {
            Type elementType = newType.ParameterType.GetElementType();
            return new ReferenceArgBuilder(newType, elementType, typeof(StrongBox<>).MakeGenericType(elementType), Index);
        }

        public override int Priority {
            get { return 5; }
        }

        internal protected override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            if (_tmp == null) {
                _tmp = resolver.GetTemporary(_elementType, "outParam");
            }

            Debug.Assert(!hasBeenUsed[Index]);
            hasBeenUsed[Index] = true;
            Expression arg = args.GetObject(Index).Expression;

            return Expression.Condition(
                Expression.TypeIs(arg, Type),
                Expression.Assign(
                    _tmp,
                    Expression.Field(
                        AstUtils.Convert(arg, Type),
                        Type.GetDeclaredField("Value")
                    )
                ),
                Expression.Throw(
                    Expression.Call(
                        new Func<Type, object, Exception>(RuntimeHelpers.MakeIncorrectBoxTypeError).GetMethodInfo(),
                        AstUtils.Constant(_elementType),
                        AstUtils.Convert(arg, typeof(object))
                    ),
                    _elementType
                )
            );
        }

        internal override Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args) {
            return Expression.Assign(
                Expression.Field(
                    Expression.Convert(args.GetObject(Index).Expression, Type),
                    Type.GetDeclaredField("Value")
                ),
                _tmp
            );
        }

        internal override Expression ByRefArgument {
            get { return _tmp; }
        }
    }
}
