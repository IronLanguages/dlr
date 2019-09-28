// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Microsoft.Scripting.Actions.Calls;
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
    public partial class DefaultBinder : ActionBinder {
        internal static readonly DefaultBinder Instance = new DefaultBinder();

        public override bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level) {
            return toType.IsAssignableFrom(fromType);
        }

        public override Candidate PreferConvert(Type t1, Type t2) {
            return Candidate.Ambiguous;
        }

        /// <summary>
        /// Provides a way for the binder to provide a custom error message when lookup fails.  Just
        /// doing this for the time being until we get a more robust error return mechanism.
        /// </summary>
        public virtual ErrorInfo MakeUndeletableMemberError(Type type, string name) {
            return MakeReadOnlyMemberError(type, name);
        }

        /// <summary>
        /// Called when the user is accessing a protected or private member on a get.
        /// 
        /// The default implementation allows access to the fields or properties using reflection.
        /// </summary>
        public virtual ErrorInfo MakeNonPublicMemberGetError(OverloadResolverFactory resolverFactory, MemberTracker member, Type type, DynamicMetaObject instance) {
            switch (member.MemberType) {
                case TrackerTypes.Field:
                    FieldTracker ft = (FieldTracker)member;

                    return ErrorInfo.FromValueNoError(
                        Expression.Call(
                            AstUtils.Convert(AstUtils.Constant(ft.Field), typeof(FieldInfo)),
                            typeof(FieldInfo).GetMethod("GetValue"),
                            AstUtils.Convert(instance.Expression, typeof(object))
                        )
                    );
                case TrackerTypes.Property:
                    PropertyTracker pt = (PropertyTracker)member;

                    return ErrorInfo.FromValueNoError(
                        MemberTracker.FromMemberInfo(pt.GetGetMethod(true)).Call(resolverFactory, this, instance).Expression
                    );
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Provides a way for the binder to provide a custom error message when lookup fails.  Just
        /// doing this for the time being until we get a more robust error return mechanism.
        /// </summary>
        public virtual ErrorInfo MakeReadOnlyMemberError(Type type, string name) {
            return ErrorInfo.FromException(
                Expression.New(
                    typeof(MissingMemberException).GetConstructor(new[] { typeof(string) }),
                    AstUtils.Constant(name)
                )
            );
        }

        public virtual ErrorInfo MakeEventValidation(MemberGroup members, DynamicMetaObject eventObject, DynamicMetaObject value, OverloadResolverFactory resolverFactory) {
            EventTracker ev = (EventTracker)members[0];

            // handles in place addition of events - this validates the user did the right thing.
            return ErrorInfo.FromValueNoError(
                Expression.Call(
                    typeof(BinderOps).GetMethod("SetEvent"),
                    AstUtils.Constant(ev),
                    value.Expression
                )
            );
        }

        public static DynamicMetaObject MakeError(ErrorInfo error, Type type) {
            return MakeError(error, BindingRestrictions.Empty, type);
        }

        public static DynamicMetaObject MakeError(ErrorInfo error, BindingRestrictions restrictions, Type type) {
            switch (error.Kind) {
                case ErrorInfoKind.Error:
                    return new ErrorMetaObject(AstUtils.Convert(error.Expression, type), restrictions);
                case ErrorInfoKind.Exception:
                    return new ErrorMetaObject(AstUtils.Convert(Expression.Throw(error.Expression), type), restrictions);
                case ErrorInfoKind.Success:
                    return new ErrorMetaObject(AstUtils.Convert(error.Expression, type), restrictions);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static Expression MakeAmbiguousMatchError(MemberGroup members) {
            StringBuilder sb = new StringBuilder();
            foreach (MemberTracker mi in members) {
                if (sb.Length != 0) sb.Append(", ");
                sb.Append(mi.MemberType);
                sb.Append(" : ");
                sb.Append(mi);
            }

            return Expression.Throw(
                Expression.New(
                    typeof(AmbiguousMatchException).GetConstructor(new Type[] { typeof(string) }),
                    AstUtils.Constant(sb.ToString())
                ),
                typeof(object)
            );
        }

        public TrackerTypes GetMemberType(MemberGroup members, out Expression error) {
            error = null;
            TrackerTypes memberType = TrackerTypes.All;
            for (int i = 0; i < members.Count; i++) {
                MemberTracker mi = members[i];
                if (mi.MemberType != memberType) {
                    if (memberType != TrackerTypes.All) {
                        error = MakeAmbiguousMatchError(members);
                        return TrackerTypes.All;
                    }
                    memberType = mi.MemberType;
                }
            }
            return memberType;
        }

        public MethodInfo GetMethod(Type type, string name) {
            // declaring type takes precedence
            MethodInfo mi = GetSpecialNameMethod(type, name);
            if (mi != null) {
                return mi;
            }

            // then search extension types.
            Type curType = type;
            do {
                IList<Type> extTypes = GetExtensionTypes(curType);
                foreach (Type t in extTypes) {
                    MethodInfo next = GetSpecialNameMethod(t, name);
                    if (next != null) {
                        if (mi != null) {
                            throw AmbiguousMatch(type, name);
                        }

                        mi = next;
                    }
                }

                if (mi != null) {
                    return mi;
                }

                curType = curType.BaseType;
            } while (curType != null);

            return null;
        }
        
        private static MethodInfo GetSpecialNameMethod(Type type, string name) {
            MethodInfo res = null;
            
            foreach (MethodInfo candidate in type.GetInheritedMethods(name).WithBindingFlags(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
                if (candidate.IsSpecialName) {
                    if (object.ReferenceEquals(res, null)) {
                        res = candidate;
                    } else {
                        throw AmbiguousMatch(type, name);
                    }
                }
            }

            return res;
        }
        
        private static Exception AmbiguousMatch(Type type, string name) {
            throw new AmbiguousMatchException(
                $"Found multiple SpecialName methods for {name} on type {type}"
            );
        }
    }
}
