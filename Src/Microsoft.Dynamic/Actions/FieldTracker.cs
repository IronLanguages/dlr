// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions {
    public class FieldTracker : MemberTracker {
        private readonly FieldInfo _field;

        public FieldTracker(FieldInfo field) {
            ContractUtils.RequiresNotNull(field, nameof(field));
            _field = field;
        }

        public override Type DeclaringType => _field.DeclaringType;

        public override TrackerTypes MemberType => TrackerTypes.Field;

        public override string Name => _field.Name;

        public bool IsPublic => _field.IsPublic;

        public bool IsInitOnly => _field.IsInitOnly;

        public bool IsLiteral => _field.IsLiteral;

        public Type FieldType => _field.FieldType;

        public bool IsStatic => _field.IsStatic;

        public FieldInfo Field => _field;

        public override string ToString() {
            return _field.ToString();
        }

        #region Public expression builders

        public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type) {
            if (Field.IsLiteral) {
                return new DynamicMetaObject(
                    AstUtils.Constant(Field.GetValue(null), typeof(object)),
                    BindingRestrictions.Empty
                );
            }

            if (!IsStatic) {
                // return the field tracker...
                return binder.ReturnMemberTracker(type, this);
            }

            if (Field.DeclaringType.ContainsGenericParameters()) {
                return null;
            }

            if (IsPublic && DeclaringType.IsPublic()) {
                return new DynamicMetaObject(
                    Expression.Convert(Expression.Field(null, Field), typeof(object)),
                    BindingRestrictions.Empty
                );
            }

            return new DynamicMetaObject(
                Expression.Call(
                    AstUtils.Convert(AstUtils.Constant(Field), typeof(FieldInfo)),
                    typeof(FieldInfo).GetMethod("GetValue"),
                    AstUtils.Constant(null)
                ),
                BindingRestrictions.Empty
            );
        }

        public override ErrorInfo GetError(ActionBinder binder, Type instanceType) {
            // FieldTracker only has one error - accessing a static field from 
            // a generic type.
            Debug.Assert(Field.DeclaringType.ContainsGenericParameters());

            return binder.MakeContainsGenericParametersError(this);
        }

        #endregion

        #region Internal expression builders

        protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) {
            if (IsPublic && DeclaringType.IsVisible()) {
                return new DynamicMetaObject(
                    AstUtils.Convert(
                        Expression.Field(
                            AstUtils.Convert(instance.Expression, Field.DeclaringType),
                            Field
                        ),
                        typeof(object)                    
                    ),
                    BindingRestrictions.Empty
                );
            }

            return DefaultBinder.MakeError(((DefaultBinder)binder).MakeNonPublicMemberGetError(resolverFactory, this, type, instance), BindingRestrictions.Empty, typeof(object));
        }

        public override MemberTracker BindToInstance(DynamicMetaObject instance) {
            if (IsStatic) return this;

            return new BoundMemberTracker(this, instance);
        }

        #endregion
    }
}
