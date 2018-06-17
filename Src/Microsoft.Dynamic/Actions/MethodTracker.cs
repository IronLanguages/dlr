// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions {

    public class MethodTracker : MemberTracker {
        private readonly MethodInfo _method;

        internal MethodTracker(MethodInfo method) {
            ContractUtils.RequiresNotNull(method, nameof(method));
            _method = method;
            IsStatic = method.IsStatic;
        }

        internal MethodTracker(MethodInfo method, bool isStatic) {
            ContractUtils.RequiresNotNull(method, nameof(method));
            _method = method;
            IsStatic = isStatic;
        }

        public override Type DeclaringType => _method.DeclaringType;

        public override TrackerTypes MemberType => TrackerTypes.Method;

        public override string Name => _method.Name;

        public MethodInfo Method => _method;

        public bool IsPublic => _method.IsPublic;

        public bool IsStatic { get; }

        public override string ToString() {
            return _method.ToString();
        }

        public override MemberTracker BindToInstance(DynamicMetaObject instance) {
            if (IsStatic) {
                return this;
            }

            return new BoundMemberTracker(this, instance);
        }

        protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) {
            return binder.ReturnMemberTracker(type, BindToInstance(instance));
        }

        internal override DynamicMetaObject Call(OverloadResolverFactory resolverFactory, ActionBinder binder, params DynamicMetaObject[] arguments) {
            if (Method.IsPublic && Method.DeclaringType.IsVisible()) {
                return binder.MakeCallExpression(resolverFactory, Method, arguments);
            }

            //methodInfo.Invoke(obj, object[] params)
            if (Method.IsStatic) {
                return new DynamicMetaObject(
                        Expression.Convert(
                            Expression.Call(
                                AstUtils.Constant(Method),
                                typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }),
                                AstUtils.Constant(null),
                                AstUtils.NewArrayHelper(typeof(object), ArrayUtils.ConvertAll(arguments, x => x.Expression))
                            ),
                            Method.ReturnType
                        ),
                        BindingRestrictions.Empty
                    )
                ;
            }

            if (arguments.Length == 0) throw Error.NoInstanceForCall();

            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Call(
                        AstUtils.Constant(Method),
                        typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }),
                        arguments[0].Expression,
                        AstUtils.NewArrayHelper(typeof(object), ArrayUtils.ConvertAll(ArrayUtils.RemoveFirst(arguments), x => x.Expression))
                    ),
                    Method.ReturnType
                ),
                BindingRestrictions.Empty
            );
        }
    }
}
