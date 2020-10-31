// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Provides binding semantics for a language.  This include conversions as well as support
    /// for producing rules for actions.  These optimized rules are used for calling methods, 
    /// performing operators, and getting members using the ActionBinder's conversion semantics.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public abstract class ActionBinder {
        private ScriptDomainManager _manager;

        /// <summary>
        /// Determines if the binder should allow access to non-public members.
        /// 
        /// By default the binder does not allow access to non-public members.  Base classes
        /// can inherit and override this value to customize whether or not private binding
        /// is available.
        /// </summary>
        public virtual bool PrivateBinding => _manager != null && _manager.Configuration.PrivateBinding;

        protected ActionBinder() {
        }

        [Obsolete("ScriptDomainManager is no longer required by ActionBinder and will go away, you should call the default constructor instead.  You should also override PrivateBinding which is the only thing which previously used the ScriptDomainManager.")]
        protected ActionBinder(ScriptDomainManager manager) {
            _manager = manager;
        }

        [Obsolete("ScriptDomainManager is no longer required by ActionBinder and will no longer be available on the base class.")]
        public ScriptDomainManager Manager => _manager;

        /// <summary>
        /// Converts an object at runtime into the specified type.
        /// </summary>
        public virtual object Convert(object obj, Type toType) {
            if (obj == null) {
                if (!toType.IsValueType) {
                    return null;
                }
            } else {
                if (toType.IsValueType) {
                    if (toType == obj.GetType()) {
                        return obj;
                    }
                } else {
                    if (toType.IsAssignableFrom(obj.GetType())) {
                        return obj;
                    }
                }
            }
            throw Error.InvalidCast(obj?.GetType().Name ?? "(null)", toType.Name);
        }

        /// <summary>
        /// Determines if a conversion exists from fromType to toType at the specified narrowing level.
        /// toNotNullable is true if the target variable doesn't allow null values.
        /// </summary>
        public abstract bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level);

        /// <summary>
        /// Provides ordering for two parameter types if there is no conversion between the two parameter types.
        /// </summary>
        public abstract Candidate PreferConvert(Type t1, Type t2);

        // TODO: revisit
        /// <summary>
        /// Converts the provided expression to the given type.  The expression is safe to evaluate multiple times.
        /// </summary>
        public virtual Expression ConvertExpression(Expression expr, Type toType, ConversionResultKind kind, OverloadResolverFactory resolverFactory) {
            ContractUtils.RequiresNotNull(expr, nameof(expr));
            ContractUtils.RequiresNotNull(toType, nameof(toType));

            Type exprType = expr.Type;

            if (toType == typeof(object)) {
                if (exprType.IsValueType) {
                    return AstUtils.Convert(expr, toType);
                }

                return expr;
            }

            if (toType.IsAssignableFrom(exprType)) {
                return expr;
            }

            return Expression.Convert(expr, CompilerHelpers.GetVisibleType(toType));
        }

        /// <summary>
        /// Gets the members that are visible from the provided type of the specified name.
        /// 
        /// The default implemetnation first searches the type, then the flattened heirachy of the type, and then
        /// registered extension methods.
        /// </summary>
        public virtual MemberGroup GetMember(MemberRequestKind action, Type type, string name) {
            IEnumerable<MemberInfo> foundMembers = type.GetInheritedMembers(name);
            if (!PrivateBinding) {
                foundMembers = CompilerHelpers.FilterNonVisibleMembers(type, foundMembers);
            }

            MemberGroup members = new MemberGroup(foundMembers.ToArray());

            // check for generic types w/ arity...
            string genName = name + ReflectionUtils.GenericArityDelimiter;
            List<Type> genTypes = null;
            foreach (Type t in type.GetDeclaredNestedTypes()) {
                if (t.IsPublic && t.Name.StartsWith(genName)) {
                    if (genTypes == null) {
                        genTypes = new List<Type>();
                    }

                    genTypes.Add(t);
                }
            }

            if (genTypes != null) {
                List<MemberTracker> mt = new List<MemberTracker>(members);
                foreach (Type t in genTypes) {
                    mt.Add(MemberTracker.FromMemberInfo(t));
                }
                return MemberGroup.CreateInternal(mt.ToArray());
            }

            if (members.Count == 0) {
                members = new MemberGroup(
                    type.GetInheritedMembers(name, flattenHierarchy: true).WithBindingFlags(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).ToArray()
                );

                if (members.Count == 0) {
                    members = GetAllExtensionMembers(type, name);
                }
            }

            return members;
        }

        #region Error Production

        public virtual ErrorInfo MakeContainsGenericParametersError(MemberTracker tracker) {
            return ErrorInfo.FromException(
                Expression.New(
                    typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }),
                    AstUtils.Constant(Strings.InvalidOperation_ContainsGenericParameters(tracker.DeclaringType.Name, tracker.Name))
                )
            );
        }

        public virtual ErrorInfo MakeMissingMemberErrorInfo(Type type, string name) {
            return ErrorInfo.FromException(
                Expression.New(
                    typeof(MissingMemberException).GetConstructor(new[] { typeof(string) }),
                    AstUtils.Constant(name)
                )
            );
        }

        public virtual ErrorInfo MakeGenericAccessError(MemberTracker info) {
            return ErrorInfo.FromException(
                Expression.New(
                    typeof(MemberAccessException).GetConstructor(new[] { typeof(string) }),
                    AstUtils.Constant(info.Name)
                )
            );
        }

        public ErrorInfo MakeStaticPropertyInstanceAccessError(PropertyTracker tracker, bool isAssignment, params DynamicMetaObject[] parameters) {
            return MakeStaticPropertyInstanceAccessError(tracker, isAssignment, (IList<DynamicMetaObject>)parameters);
        }

        /// <summary>
        /// Called when a set is attempting to assign to a field or property from a derived class through the base class.
        /// 
        /// The default behavior is to allow the assignment.
        /// </summary>
        public virtual ErrorInfo MakeStaticAssignFromDerivedTypeError(Type accessingType, DynamicMetaObject self, MemberTracker assigning, DynamicMetaObject assignedValue, OverloadResolverFactory context) {
            switch (assigning.MemberType) {
                case TrackerTypes.Property:
                    PropertyTracker pt = (PropertyTracker)assigning;
                    MethodInfo setter = pt.GetSetMethod() ?? pt.GetSetMethod(true);
                    return ErrorInfo.FromValueNoError(
                        AstUtils.SimpleCallHelper(
                            setter,
                            ConvertExpression(
                                assignedValue.Expression,
                                setter.GetParameters()[0].ParameterType,
                                ConversionResultKind.ExplicitCast,
                                context
                            )
                        )
                    );
                case TrackerTypes.Field:
                    FieldTracker ft = (FieldTracker)assigning;
                    return ErrorInfo.FromValueNoError(
                        Expression.Assign(
                            Expression.Field(null, ft.Field),
                            ConvertExpression(assignedValue.Expression, ft.FieldType, ConversionResultKind.ExplicitCast, context)
                        )
                    );
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Creates an ErrorInfo object when a static property is accessed from an instance member.  The default behavior is throw
        /// an exception indicating that static members properties be accessed via an instance.  Languages can override this to 
        /// customize the exception, message, or to produce an ErrorInfo object which reads or writes to the property being accessed.
        /// </summary>
        /// <param name="tracker">The static property being accessed through an instance</param>
        /// <param name="isAssignment">True if the user is assigning to the property, false if the user is reading from the property</param>
        /// <param name="parameters">The parameters being used to access the property.  This includes the instance as the first entry, any index parameters, and the
        /// value being assigned as the last entry if isAssignment is true.</param>
        /// <returns></returns>
        public virtual ErrorInfo MakeStaticPropertyInstanceAccessError(PropertyTracker tracker, bool isAssignment, IList<DynamicMetaObject> parameters) {
            ContractUtils.RequiresNotNull(tracker, nameof(tracker));
            ContractUtils.Requires(tracker.IsStatic, nameof(tracker), Strings.ExpectedStaticProperty);
            ContractUtils.RequiresNotNull(parameters, nameof(parameters));
            ContractUtils.RequiresNotNullItems(parameters, nameof(parameters));

            string message = isAssignment ? Strings.StaticAssignmentFromInstanceError(tracker.Name, tracker.DeclaringType.Name) :
                                            Strings.StaticAccessFromInstanceError(tracker.Name, tracker.DeclaringType.Name);

            return ErrorInfo.FromException(
                Expression.New(
                    typeof(MissingMemberException).GetConstructor(new[] { typeof(string) }),
                    AstUtils.Constant(message)
                )
            );
        }

        public virtual ErrorInfo MakeSetValueTypeFieldError(FieldTracker field, DynamicMetaObject instance, DynamicMetaObject value) {
            return ErrorInfo.FromException(
                Expression.Throw(
                    Expression.New(
                        typeof(ArgumentException).GetConstructor(new[] { typeof(string) }),
                        AstUtils.Constant("cannot assign to value types")
                    ),
                    typeof(object)
                )
            );
        }

        public virtual ErrorInfo MakeConversionError(Type toType, Expression value) {
            return ErrorInfo.FromException(
                Expression.Call(
                    new Func<Type, object, Exception>(ScriptingRuntimeHelpers.CannotConvertError).GetMethodInfo(),
                    AstUtils.Constant(toType),
                    AstUtils.Convert(value, typeof(object))
               )
            );
        }

        /// <summary>
        /// Provides a way for the binder to provide a custom error message when lookup fails.  Just
        /// doing this for the time being until we get a more robust error return mechanism.
        /// 
        /// Deprecated, use the non-generic version instead
        /// </summary>
        public virtual ErrorInfo MakeMissingMemberError(Type type, DynamicMetaObject self, string name) {
            return ErrorInfo.FromException(
                Expression.New(
                    typeof(MissingMemberException).GetConstructor(new[] { typeof(string) }),
                    AstUtils.Constant(name)
                )
            );
        }

        public virtual ErrorInfo MakeMissingMemberErrorForAssign(Type type, DynamicMetaObject self, string name) {
            return MakeMissingMemberError(type, self, name);
        }

        public virtual ErrorInfo MakeMissingMemberErrorForAssignReadOnlyProperty(Type type, DynamicMetaObject self, string name) {
            return MakeMissingMemberError(type, self, name);
        }

        public virtual ErrorInfo MakeMissingMemberErrorForDelete(Type type, DynamicMetaObject self, string name) {
            return MakeMissingMemberError(type, self, name);
        }

        #endregion
       
        public virtual string GetTypeName(Type t) {
            return t.Name;
        }

        public virtual string GetObjectTypeName(object arg) {
            return GetTypeName(CompilerHelpers.GetType(arg));
        }

        /// <summary>
        /// Gets the extension members of the given name from the provided type.  Base classes are also
        /// searched for their extension members.  Once any of the types in the inheritance hierarchy
        /// provide an extension member the search is stopped.
        /// </summary>
        public MemberGroup GetAllExtensionMembers(Type type, string name) {
            foreach (Type ancestor in type.Ancestors()) {
                MemberGroup res = GetExtensionMembers(ancestor, name);
                if (res.Count != 0) {
                    return res;
                }
            }

            return MemberGroup.EmptyGroup;
        }

        /// <summary>
        /// Gets the extension members of the given name from the provided type.  Subclasses of the
        /// type and their extension members are not searched.
        /// </summary>
        public MemberGroup GetExtensionMembers(Type declaringType, string name) {
            IList<Type> extTypes = GetExtensionTypes(declaringType);
            List<MemberTracker> members = new List<MemberTracker>();

            foreach (Type ext in extTypes) {
                foreach (MemberInfo mi in ext.GetDeclaredMembers(name).WithBindingFlags(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
                    MemberInfo newMember = mi;
                    if (PrivateBinding || (newMember = CompilerHelpers.TryGetVisibleMember(ext, mi)) != null) {
                        if (IncludeExtensionMember(newMember)) {
                            if (ext != declaringType) {
                                members.Add(MemberTracker.FromMemberInfo(newMember, declaringType));
                            } else {
                                members.Add(MemberTracker.FromMemberInfo(newMember));
                            }
                        }
                    }
                }

                // TODO: Support indexed getters/setters w/ multiple methods
                MethodInfo getter = GetExtensionOperator(ext, "Get" + name);
                MethodInfo setter = GetExtensionOperator(ext, "Set" + name);
                MethodInfo deleter = GetExtensionOperator(ext, "Delete" + name);

                if (getter != null || setter != null || deleter != null) {
                    members.Add(new ExtensionPropertyTracker(name, getter, setter, deleter, declaringType));
                }
            }

            if (members.Count != 0) {
                return MemberGroup.CreateInternal(members.ToArray());
            }
            return MemberGroup.EmptyGroup;
        }

        private MethodInfo GetExtensionOperator(Type ext, string name) {
            return ext.GetInheritedMethods(name)
                   .WithBindingFlags(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                   .SingleOrDefault(mi => mi.IsDefined(typeof(PropertyMethodAttribute), false));
        }

        public virtual bool IncludeExtensionMember(MemberInfo member) {
            return true;
        }

        public virtual IList<Type> GetExtensionTypes(Type t) {
            // None are provided by default, languages need to know how to
            // provide these on their own terms.
            return EmptyArray<Type>.Instance;
        }

        /// <summary>
        /// Provides an opportunity for languages to replace all MemberTracker's with their own type.
        /// 
        /// Alternatlely a language can expose MemberTracker's directly.
        /// </summary>
        /// <param name="memberTracker">The member which is being returned to the user.</param>
        /// <param name="type">Tthe type which the memberTrack was accessed from</param>
        /// <returns></returns>
        public virtual DynamicMetaObject ReturnMemberTracker(Type type, MemberTracker memberTracker) {
            if (memberTracker.MemberType == TrackerTypes.Bound) {
                BoundMemberTracker bmt = (BoundMemberTracker)memberTracker;
                return new DynamicMetaObject(
                    Expression.New(
                        typeof(BoundMemberTracker).GetConstructor(new[] { typeof(MemberTracker), typeof(object) }),
                        AstUtils.Constant(bmt.BoundTo),
                        bmt.Instance.Expression
                    ),
                    BindingRestrictions.Empty
                );
            }

            return new DynamicMetaObject(AstUtils.Constant(memberTracker), BindingRestrictions.Empty, memberTracker);
        }

        public DynamicMetaObject MakeCallExpression(OverloadResolverFactory resolverFactory, MethodInfo method, params DynamicMetaObject[] parameters) {
            OverloadResolver resolver;
            if (method.IsStatic) {
                resolver = resolverFactory.CreateOverloadResolver(parameters, new CallSignature(parameters.Length), CallTypes.None);
            } else {
                resolver = resolverFactory.CreateOverloadResolver(parameters, new CallSignature(parameters.Length - 1), CallTypes.ImplicitInstance);
            }
            BindingTarget target = resolver.ResolveOverload(method.Name, new MethodBase[] { method }, NarrowingLevel.None, NarrowingLevel.All);

            if (!target.Success) {
                BindingRestrictions restrictions = BindingRestrictions.Combine(parameters);
                foreach (DynamicMetaObject mo in parameters) {
                    restrictions = restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(mo.Expression, mo.GetLimitType()));
                }
                return DefaultBinder.MakeError(
                    resolver.MakeInvalidParametersError(target),
                    restrictions,
                    typeof(object)
                );
            }

            return new DynamicMetaObject(target.MakeExpression(), target.RestrictedArguments.GetAllRestrictions());
        }
    }
}
