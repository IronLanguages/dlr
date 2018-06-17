// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions {

    public partial class DefaultBinder : ActionBinder {
        /// <summary>
        /// Builds a MetaObject for performing a member get.  Supports all built-in .NET members, the OperatorMethod 
        /// GetBoundMember, and StrongBox instances.
        /// </summary>
        /// <param name="name">
        /// The name of the member to retrieve.  This name is not processed by the DefaultBinder and
        /// is instead handed off to the GetMember API which can do name mangling, case insensitive lookups, etc...
        /// </param>
        /// <param name="target">
        /// The MetaObject from which the member is retrieved.
        /// </param>
        /// <param name="value">
        /// The value being assigned to the target member.
        /// </param>
        public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value) {
            return SetMember(name, target, value, new DefaultOverloadResolverFactory(this));
        }
        
        /// <summary>
        /// Builds a MetaObject for performing a member get.  Supports all built-in .NET members, the OperatorMethod 
        /// GetBoundMember, and StrongBox instances.
        /// </summary>
        /// <param name="name">
        /// The name of the member to retrieve.  This name is not processed by the DefaultBinder and
        /// is instead handed off to the GetMember API which can do name mangling, case insensitive lookups, etc...
        /// </param>
        /// <param name="target">
        /// The MetaObject from which the member is retrieved.
        /// </param>
        /// <param name="value">
        /// The value being assigned to the target member.
        /// </param>
        /// <param name="resolverFactory">
        /// Provides overload resolution and method binding for any calls which need to be performed for the SetMember.
        /// </param>
        public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value, OverloadResolverFactory resolverFactory) {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNull(target, nameof(target));
            ContractUtils.RequiresNotNull(value, nameof(value));
            ContractUtils.RequiresNotNull(resolverFactory, nameof(resolverFactory));

            return MakeSetMemberTarget(
                new SetOrDeleteMemberInfo(name, resolverFactory),
                target,
                value,
                null
            );
        }

        /// <summary>
        /// Builds a MetaObject for performing a member get.  Supports all built-in .NET members, the OperatorMethod 
        /// GetBoundMember, and StrongBox instances.
        /// </summary>
        /// <param name="name">
        /// The name of the member to retrieve.  This name is not processed by the DefaultBinder and
        /// is instead handed off to the GetMember API which can do name mangling, case insensitive lookups, etc...
        /// </param>
        /// <param name="target">
        /// The MetaObject from which the member is retrieved.
        /// </param>
        /// <param name="value">
        /// The value being assigned to the target member.
        /// </param>
        /// <param name="errorSuggestion">
        /// Provides a DynamicMetaObject that is to be used as the result if the member cannot be set.  If null then then a language
        /// specific error code is provided by ActionBinder.MakeMissingMemberErrorForAssign which can be overridden by the language.
        /// </param>
        public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
            return SetMember(name, target, value, errorSuggestion, new DefaultOverloadResolverFactory(this));
        }
        
        /// <summary>
        /// Builds a MetaObject for performing a member get.  Supports all built-in .NET members, the OperatorMethod 
        /// GetBoundMember, and StrongBox instances.
        /// </summary>
        /// <param name="name">
        /// The name of the member to retrieve.  This name is not processed by the DefaultBinder and
        /// is instead handed off to the GetMember API which can do name mangling, case insensitive lookups, etc...
        /// </param>
        /// <param name="target">
        /// The MetaObject from which the member is retrieved.
        /// </param>
        /// <param name="value">
        /// The value being assigned to the target member.
        /// </param>
        /// <param name="resolverFactory">
        /// Provides overload resolution and method binding for any calls which need to be performed for the SetMember.
        /// </param>
        /// <param name="errorSuggestion">
        /// Provides a DynamicMetaObject that is to be used as the result if the member cannot be set.  If null then then a language
        /// specific error code is provided by ActionBinder.MakeMissingMemberErrorForAssign which can be overridden by the language.
        /// </param>
        public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion, OverloadResolverFactory resolverFactory) {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNull(target, nameof(target));
            ContractUtils.RequiresNotNull(value, nameof(value));
            ContractUtils.RequiresNotNull(resolverFactory, nameof(resolverFactory));

            return MakeSetMemberTarget(
                new SetOrDeleteMemberInfo(name, resolverFactory),
                target,
                value,
                errorSuggestion
            );
        }

        private DynamicMetaObject MakeSetMemberTarget(SetOrDeleteMemberInfo memInfo, DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
            Type type = target.GetLimitType();
            DynamicMetaObject self = target;
            
            target = target.Restrict(target.GetLimitType());

            memInfo.Body.Restrictions = target.Restrictions;

            if (typeof(TypeTracker).IsAssignableFrom(type)) {
                type = ((TypeTracker)target.Value).Type;
                self = null;

                memInfo.Body.Restrictions = memInfo.Body.Restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value)
                );
            }

            MakeSetMemberRule(memInfo, type, self, value, errorSuggestion);

            return memInfo.Body.GetMetaObject(target, value);
        }

        private void MakeSetMemberRule(SetOrDeleteMemberInfo memInfo, Type type, DynamicMetaObject self, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
            if (MakeOperatorSetMemberBody(memInfo, self, value, type, "SetMember")) {
                return;
            }


            MemberGroup members = GetMember(MemberRequestKind.Set, type, memInfo.Name);

            // if lookup failed try the strong-box type if available.
            if (self != null && members.Count == 0 && typeof(IStrongBox).IsAssignableFrom(type)) {
                self = new DynamicMetaObject(Expression.Field(AstUtils.Convert(self.Expression, type), type.GetInheritedFields("Value").First()), BindingRestrictions.Empty, ((IStrongBox)self.Value).Value);
                type = type.GetGenericArguments()[0];

                members = GetMember(MemberRequestKind.Set, type, memInfo.Name);
            }

            TrackerTypes memberTypes = GetMemberType(members, out Expression error);
            if (error == null) {
                switch (memberTypes) {
                    case TrackerTypes.Method:
                    case TrackerTypes.TypeGroup:
                    case TrackerTypes.Type:
                    case TrackerTypes.Constructor:
                        memInfo.Body.FinishError(
                            errorSuggestion ?? MakeError(MakeReadOnlyMemberError(type, memInfo.Name), BindingRestrictions.Empty, typeof(object))
                        );
                        break;
                    case TrackerTypes.Event:
                        memInfo.Body.FinishError(
                            errorSuggestion ?? MakeError(MakeEventValidation(members, self, value, memInfo.ResolutionFactory), BindingRestrictions.Empty, typeof(object))
                        );
                        break;
                    case TrackerTypes.Field:
                        MakeFieldRule(memInfo, self, value, type, members, errorSuggestion);
                        break;
                    case TrackerTypes.Property:
                        MakePropertyRule(memInfo, self, value, type, members, errorSuggestion);
                        break;
                    case TrackerTypes.Custom:
                        MakeGenericBody(memInfo, self, value, type, members[0], errorSuggestion);
                        break;
                    case TrackerTypes.All:
                        // no match
                        if (MakeOperatorSetMemberBody(memInfo, self, value, type, "SetMemberAfter")) {
                            return;
                        }

                        memInfo.Body.FinishError(
                            errorSuggestion ?? MakeError(MakeMissingMemberErrorForAssign(type, self, memInfo.Name), BindingRestrictions.Empty, typeof(object))
                        );
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            } else {
                memInfo.Body.FinishError(error);
            }
        }

        private void MakeGenericBody(SetOrDeleteMemberInfo memInfo, DynamicMetaObject instance, DynamicMetaObject target, Type instanceType, MemberTracker tracker, DynamicMetaObject errorSuggestion) {
            if (instance != null) {
                tracker = tracker.BindToInstance(instance);
            }

            DynamicMetaObject val = tracker.SetValue(memInfo.ResolutionFactory, this, instanceType, target, errorSuggestion);

            if (val != null) {
                memInfo.Body.FinishCondition(val);
            } else {
                memInfo.Body.FinishError(
                    MakeError(tracker.GetError(this, instanceType), typeof(object))
                );
            }
        }

        private void MakePropertyRule(SetOrDeleteMemberInfo memInfo, DynamicMetaObject instance, DynamicMetaObject target, Type targetType, MemberGroup properties, DynamicMetaObject errorSuggestion) {
            PropertyTracker info = (PropertyTracker)properties[0];

            MethodInfo setter = info.GetSetMethod(true);

            // Allow access to protected getters TODO: this should go, it supports IronPython semantics.
            if (setter != null && !setter.IsPublic && !setter.IsProtected()) {
                if (!PrivateBinding) {
                    setter = null;
                }
            }

            if (setter != null) {
                // TODO (tomat): this used to use setter.ReflectedType, is it still correct?
                setter = CompilerHelpers.TryGetCallableMethod(targetType, setter);

                if (!PrivateBinding && !CompilerHelpers.IsVisible(setter)) {
                    setter = null;
                }
            }

            if (setter != null) {
                if (info.IsStatic != (instance == null)) {
                    memInfo.Body.FinishError(
                        errorSuggestion ?? MakeError(
                            MakeStaticPropertyInstanceAccessError(
                                info,
                                true,
                                instance,
                                target
                            ), 
                            typeof(object)
                        )
                    );
                } else if (info.IsStatic && info.DeclaringType != targetType) {
                    memInfo.Body.FinishError(
                        errorSuggestion ?? MakeError(
                            MakeStaticAssignFromDerivedTypeError(targetType, instance, info, target, memInfo.ResolutionFactory), 
                            typeof(object)
                        )
                    );
                } else if (setter.ContainsGenericParameters) {
                    memInfo.Body.FinishCondition(
                        MakeGenericPropertyExpression(memInfo)
                    );
                } else if (setter.IsPublic && !setter.DeclaringType.IsValueType()) {
                    if (instance == null) {
                        memInfo.Body.FinishCondition(
                            Expression.Block(
                                AstUtils.SimpleCallHelper(
                                    setter,
                                    ConvertExpression(
                                        target.Expression,
                                        setter.GetParameters()[0].ParameterType,
                                        ConversionResultKind.ExplicitCast,
                                        memInfo.ResolutionFactory
                                    )
                                ),
                                Expression.Constant(null)
                            )
                        );
                    } else {
                        memInfo.Body.FinishCondition(
                            MakeReturnValue(
                                MakeCallExpression(memInfo.ResolutionFactory, setter, instance, target),
                                target
                            )
                        );
                    }
                } else {
                    // TODO: Should be able to do better w/ value types.
                    memInfo.Body.FinishCondition(
                        MakeReturnValue(
                            Expression.Call(
                                AstUtils.Constant(((ReflectedPropertyTracker)info).Property), // TODO: Private binding on extension properties
                                typeof(PropertyInfo).GetMethod("SetValue", new Type[] { typeof(object), typeof(object), typeof(object[]) }),
                                instance == null ? AstUtils.Constant(null) : AstUtils.Convert(instance.Expression, typeof(object)),
                                AstUtils.Convert(
                                    ConvertExpression(
                                        target.Expression,
                                        setter.GetParameters()[0].ParameterType,
                                        ConversionResultKind.ExplicitCast,
                                        memInfo.ResolutionFactory
                                    ),
                                    typeof(object)
                                ),
                                Expression.NewArrayInit(typeof(object))
                            ),
                            target
                        )
                    );
                }
            } else {
                memInfo.Body.FinishError(
                    errorSuggestion ?? MakeError(
                        MakeMissingMemberErrorForAssignReadOnlyProperty(targetType, instance, memInfo.Name), typeof(object)
                    )
                );
            }
        }

        private void MakeFieldRule(SetOrDeleteMemberInfo memInfo, DynamicMetaObject instance, DynamicMetaObject target, Type targetType, MemberGroup fields, DynamicMetaObject errorSuggestion) {
            FieldTracker field = (FieldTracker)fields[0];

            // TODO: Tmp variable for target
            if (instance != null && field.DeclaringType.IsGenericType() && field.DeclaringType.GetGenericTypeDefinition() == typeof(StrongBox<>)) {
                // work around a CLR bug where we can't access generic fields from dynamic methods.
                Type[] generic = field.DeclaringType.GetGenericArguments();
                memInfo.Body.FinishCondition(
                    MakeReturnValue(
                        Expression.Assign(
                            Expression.Field(
                                AstUtils.Convert(instance.Expression, field.DeclaringType),
                                field.DeclaringType.GetDeclaredField("Value")
                            ),
                            AstUtils.Convert(target.Expression, generic[0])
                        ),
                        target
                    )
                );
            } else if (field.IsInitOnly || field.IsLiteral) {
                memInfo.Body.FinishError(
                    errorSuggestion ?? MakeError(
                        MakeReadOnlyMemberError(targetType, memInfo.Name), 
                        typeof(object)
                    )
                );
            } else if (field.IsStatic && targetType != field.DeclaringType) {
                memInfo.Body.FinishError(
                    errorSuggestion ?? MakeError(
                        MakeStaticAssignFromDerivedTypeError(targetType, instance, field, target, memInfo.ResolutionFactory), 
                        typeof(object)
                    )
                );
            } else if (field.DeclaringType.IsValueType() && !field.IsStatic) {
                memInfo.Body.FinishError(
                    errorSuggestion ?? MakeError(
                        MakeSetValueTypeFieldError(field, instance, target),
                        typeof(object)
                    )
                );
            } else if (field.IsPublic && field.DeclaringType.IsVisible()) {
                if (!field.IsStatic && instance == null) {
                    memInfo.Body.FinishError(
                        Expression.Throw(
                            Expression.New(
                                typeof(ArgumentException).GetConstructor(new[] { typeof(string) }),
                                AstUtils.Constant("assignment to instance field w/o instance")
                            ),
                            typeof(object)
                        )
                    );

                } else {
                    memInfo.Body.FinishCondition(
                        MakeReturnValue(
                            Expression.Assign(
                                Expression.Field(
                                    field.IsStatic ?
                                        null :
                                        AstUtils.Convert(instance.Expression, field.DeclaringType),
                                    field.Field
                                ),
                                ConvertExpression(target.Expression, field.FieldType, ConversionResultKind.ExplicitCast, memInfo.ResolutionFactory)
                            ),
                            target
                        )
                    );
                }
            } else {
                Debug.Assert(field.IsStatic || instance != null);

                memInfo.Body.FinishCondition(
                    MakeReturnValue(
                        Expression.Call(
                            AstUtils.Convert(AstUtils.Constant(field.Field), typeof(FieldInfo)),
                            typeof(FieldInfo).GetMethod("SetValue", new Type[] { typeof(object), typeof(object) }),
                            field.IsStatic ?
                                AstUtils.Constant(null) :
                                (Expression)AstUtils.Convert(instance.Expression, typeof(object)),
                            AstUtils.Convert(target.Expression, typeof(object))
                        ),
                        target
                    )
                );
            }
        }

        private DynamicMetaObject MakeReturnValue(DynamicMetaObject expression, DynamicMetaObject target) {
            return new DynamicMetaObject(
                Expression.Block(
                    expression.Expression,
                    Expression.Convert(target.Expression, typeof(object))
                ),
                target.Restrictions.Merge(expression.Restrictions)
            );
        }

        private Expression MakeReturnValue(Expression expression, DynamicMetaObject target) {
            return Expression.Block(
                expression,
                Expression.Convert(target.Expression, typeof(object))
            );
        }

        /// <summary> if a member-injector is defined-on or registered-for this type call it </summary>
        private bool MakeOperatorSetMemberBody(SetOrDeleteMemberInfo memInfo, DynamicMetaObject self, DynamicMetaObject target, Type type, string name) {
            if (self != null) {
                MethodInfo setMem = GetMethod(type, name);
                if (setMem != null) {
                    ParameterExpression tmp = Expression.Variable(target.Expression.Type, "setValue");
                    memInfo.Body.AddVariable(tmp);

                    var callMo = MakeCallExpression(
                        memInfo.ResolutionFactory, 
                        setMem, 
                        self.Clone(AstUtils.Convert(self.Expression, type)),
                        new DynamicMetaObject(AstUtils.Constant(memInfo.Name), BindingRestrictions.Empty, memInfo.Name),
                        target.Clone(tmp)
                    );

                    var call = Expression.Block(Expression.Assign(tmp, target.Expression), callMo.Expression);

                    if (setMem.ReturnType == typeof(bool)) {
                        memInfo.Body.AddCondition(
                            call,
                            tmp
                        );
                    } else {
                        memInfo.Body.FinishCondition(Expression.Block(call, AstUtils.Convert(tmp, typeof(object))));
                    }

                    return setMem.ReturnType != typeof(bool);
                }
            }

            return false;
        }

        private static Expression MakeGenericPropertyExpression(SetOrDeleteMemberInfo memInfo) {
            return Expression.New(
                typeof(MemberAccessException).GetConstructor(new[] { typeof(string) }),
                AstUtils.Constant(memInfo.Name)
            );
        }
    }
}
