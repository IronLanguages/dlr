// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_METADATA_READER
using Microsoft.Scripting.Metadata;
#endif

using TypeInfo = System.Type;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Utils {
    public static class ReflectionUtils {
        #region Accessibility

        public static readonly BindingFlags AllMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static bool IsPublic(this PropertyInfo property) {
            return property.GetGetMethod(nonPublic: false) != null
                || property.GetSetMethod(nonPublic: false) != null;
        }

        public static bool IsStatic(this PropertyInfo property) {
            var getter = property.GetGetMethod(nonPublic: true);
            var setter = property.GetSetMethod(nonPublic: true);

            return getter != null && getter.IsStatic
                || setter != null && setter.IsStatic;
        }

        public static bool IsStatic(this EventInfo evnt) {
            var add = evnt.GetAddMethod(nonPublic: true);
            var remove = evnt.GetRemoveMethod(nonPublic: true);

            return add != null && add.IsStatic
                || remove != null && remove.IsStatic;
        }

        public static bool IsPrivate(this PropertyInfo property) {
            var getter = property.GetGetMethod(nonPublic: true);
            var setter = property.GetSetMethod(nonPublic: true);

            return (getter == null || getter.IsPrivate)
                && (setter == null || setter.IsPrivate);
        }

        public static bool IsPrivate(this EventInfo evnt) {
            var add = evnt.GetAddMethod(nonPublic: true);
            var remove = evnt.GetRemoveMethod(nonPublic: true);

            return (add == null || add.IsPrivate)
                && (remove == null || remove.IsPrivate);
        }

        private static bool MatchesFlags(ConstructorInfo member, BindingFlags flags) {
            return
                ((member.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic) & flags) != 0 &&
                ((member.IsStatic ? BindingFlags.Static : BindingFlags.Instance) & flags) != 0;
        }

        private static bool MatchesFlags(MethodInfo member, BindingFlags flags) {
            return
                ((member.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic) & flags) != 0 &&
                ((member.IsStatic ? BindingFlags.Static : BindingFlags.Instance) & flags) != 0;
        }

        private static bool MatchesFlags(FieldInfo member, BindingFlags flags) {
            return
                ((member.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic) & flags) != 0 &&
                ((member.IsStatic ? BindingFlags.Static : BindingFlags.Instance) & flags) != 0;
        }

        private static bool MatchesFlags(PropertyInfo member, BindingFlags flags) {
            return
                ((member.IsPublic() ? BindingFlags.Public : BindingFlags.NonPublic) & flags) != 0 &&
                ((member.IsStatic() ? BindingFlags.Static : BindingFlags.Instance) & flags) != 0;
        }

        private static bool MatchesFlags(EventInfo member, BindingFlags flags) {
            var add = member.GetAddMethod();
            var remove = member.GetRemoveMethod();
            var raise = member.GetRaiseMethod();

            bool isPublic = add != null && add.IsPublic || remove != null && remove.IsPublic || raise != null && raise.IsPublic;
            bool isStatic = add != null && add.IsStatic || remove != null && remove.IsStatic || raise != null && raise.IsStatic;

            return
                ((isPublic ? BindingFlags.Public : BindingFlags.NonPublic) & flags) != 0 &&
                ((isStatic ? BindingFlags.Static : BindingFlags.Instance) & flags) != 0;
        }

        private static bool MatchesFlags(TypeInfo member, BindingFlags flags) {
            // Static/Instance are ignored
            return (((member.IsPublic || member.IsNestedPublic) ? BindingFlags.Public : BindingFlags.NonPublic) & flags) != 0;
        }

        private static bool MatchesFlags(MemberInfo member, BindingFlags flags) {
            ConstructorInfo ctor;
            MethodInfo method;
            FieldInfo field;
            EventInfo evnt;
            PropertyInfo property;

            if ((method = member as MethodInfo) != null) {
                return MatchesFlags(method, flags);
            }

            if ((field = member as FieldInfo) != null) {
                return MatchesFlags(field, flags);
            }

            if ((ctor = member as ConstructorInfo) != null) {
                return MatchesFlags(ctor, flags);
            }

            if ((evnt = member as EventInfo) != null) {
                return MatchesFlags(evnt, flags);
            }

            if ((property = member as PropertyInfo) != null) {
                return MatchesFlags(property, flags);
            }

            return MatchesFlags((TypeInfo)member, flags);
        }

        private static IEnumerable<T> WithBindingFlags<T>(this IEnumerable<T> members, Func<T, BindingFlags, bool> matchFlags, BindingFlags flags)
            where T : MemberInfo {
            return members.Where(member => matchFlags(member, flags));
        }

        public static IEnumerable<MemberInfo> WithBindingFlags(this IEnumerable<MemberInfo> members, BindingFlags flags) {
            return members.WithBindingFlags(MatchesFlags, flags);
        }

        public static IEnumerable<MethodInfo> WithBindingFlags(this IEnumerable<MethodInfo> members, BindingFlags flags) {
            return members.WithBindingFlags(MatchesFlags, flags);
        }

        public static IEnumerable<ConstructorInfo> WithBindingFlags(this IEnumerable<ConstructorInfo> members, BindingFlags flags) {
            return members.WithBindingFlags(MatchesFlags, flags);
        }

        public static IEnumerable<FieldInfo> WithBindingFlags(this IEnumerable<FieldInfo> members, BindingFlags flags) {
            return members.WithBindingFlags(MatchesFlags, flags);
        }

        public static IEnumerable<PropertyInfo> WithBindingFlags(this IEnumerable<PropertyInfo> members, BindingFlags flags) {
            return members.WithBindingFlags(MatchesFlags, flags);
        }

        public static IEnumerable<EventInfo> WithBindingFlags(this IEnumerable<EventInfo> members, BindingFlags flags) {
            return members.WithBindingFlags(MatchesFlags, flags);
        }

        public static IEnumerable<TypeInfo> WithBindingFlags(this IEnumerable<TypeInfo> members, BindingFlags flags) {
            return members.WithBindingFlags(MatchesFlags, flags);
        }

        public static MemberInfo WithBindingFlags(this MemberInfo member, BindingFlags flags) {
            return member != null && MatchesFlags(member, flags) ? member : null;
        }

        public static MethodInfo WithBindingFlags(this MethodInfo member, BindingFlags flags) {
            return member != null && MatchesFlags(member, flags) ? member : null;
        }

        public static ConstructorInfo WithBindingFlags(this ConstructorInfo member, BindingFlags flags) {
            return member != null && MatchesFlags(member, flags) ? member : null;
        }

        public static FieldInfo WithBindingFlags(this FieldInfo member, BindingFlags flags) {
            return member != null && MatchesFlags(member, flags) ? member : null;
        }

        public static PropertyInfo WithBindingFlags(this PropertyInfo member, BindingFlags flags) {
            return member != null && MatchesFlags(member, flags) ? member : null;
        }

        public static EventInfo WithBindingFlags(this EventInfo member, BindingFlags flags) {
            return member != null && MatchesFlags(member, flags) ? member : null;
        }

        public static TypeInfo WithBindingFlags(this TypeInfo member, BindingFlags flags) {
            return member != null && MatchesFlags(member, flags) ? member : null;
        }

        #endregion

        #region Signatures

        public static IEnumerable<MethodInfo> WithSignature(this IEnumerable<MethodInfo> members, Type[] parameterTypes) {
            return members.Where(c => {
                var ps = c.GetParameters();
                if (ps.Length != parameterTypes.Length) {
                    return false;
                }

                for (int i = 0; i < ps.Length; i++) {
                    if (parameterTypes[i] != ps[i].ParameterType) {
                        return false;
                    }
                }

                return true;
            });
        }

        public static IEnumerable<ConstructorInfo> WithSignature(this IEnumerable<ConstructorInfo> members, Type[] parameterTypes) {
            return members.Where(c => {
                var ps = c.GetParameters();
                if (ps.Length != parameterTypes.Length) {
                    return false;
                }

                for (int i = 0; i < ps.Length; i++) {
                    if (parameterTypes[i] != ps[i].ParameterType) {
                        return false;
                    }
                }

                return true;
            });
        }

        #endregion

        #region Member Inheritance

        // CLI specification, partition I, 8.10.4: Hiding, overriding, and layout
        // ----------------------------------------------------------------------
        // While hiding applies to all members of a type, overriding deals with object layout and is applicable only to instance fields 
        // and virtual methods. The CTS provides two forms of member overriding, new slot and expect existing slot. A member of a derived 
        // type that is marked as a new slot will always get a new slot in the object's layout, guaranteeing that the base field or method 
        // is available in the object by using a qualified reference that combines the name of the base type with the name of the member 
        // and its type or signature. A member of a derived type that is marked as expect existing slot will re-use (i.e., share or override) 
        // a slot that corresponds to a member of the same kind (field or method), name, and type if one already exists from the base type; 
        // if no such slot exists, a new slot is allocated and used.
        //
        // The general algorithm that is used for determining the names in a type and the layout of objects of the type is roughly as follows:
        // - Flatten the inherited names (using the hide by name or hide by name-and-signature rule) ignoring accessibility rules. 
        // - For each new member that is marked "expect existing slot", look to see if an exact match on kind (i.e., field or method), 
        //   name, and signature exists and use that slot if it is found, otherwise allocate a new slot. 
        // - After doing this for all new members, add these new member-kind/name/signatures to the list of members of this type 
        // - Finally, remove any inherited names that match the new members based on the hide by name or hide by name-and-signature rules.

        // NOTE: Following GetXxx only implement overriding, not hiding specified by hide-by-name or hide-by-name-and-signature flags.

        public static IEnumerable<MethodInfo> GetInheritedMethods(this Type type, string name = null, bool flattenHierarchy = false) {
            while (type.IsGenericParameter) {
                type = type.BaseType;
            }

            var baseDefinitions = new HashSet<MethodInfo>(ReferenceEqualityComparer<MethodInfo>.Instance);
            foreach (var ancestor in type.Ancestors()) {
                foreach (var declaredMethod in ancestor.GetDeclaredMethods(name)) {
                    if (declaredMethod != null && IncludeMethod(declaredMethod, type, baseDefinitions, flattenHierarchy)) {
                        yield return declaredMethod;
                    }
                }
            }
        }

        private static bool IncludeMethod(MethodInfo member, Type reflectedType, HashSet<MethodInfo> baseDefinitions, bool flattenHierarchy) {
            if (member.IsVirtual) {
                if (baseDefinitions.Add(RuntimeReflectionExtensions.GetRuntimeBaseDefinition(member))) {
                    return true;
                }
            } else if (member.DeclaringType == reflectedType) {
                return true;
            } else if (!member.IsPrivate && (!member.IsStatic || flattenHierarchy)) {
                return true;
            }

            return false;
        }

        public static IEnumerable<PropertyInfo> GetInheritedProperties(this Type type, string name = null, bool flattenHierarchy = false) {
            while (type.IsGenericParameter) {
                type = type.BaseType;
            }

            var baseDefinitions = new HashSet<MethodInfo>(ReferenceEqualityComparer<MethodInfo>.Instance);
            foreach (var ancestor in type.Ancestors()) {
                if (name != null) {
                    var declaredProperty = ancestor.GetDeclaredProperty(name);
                    if (declaredProperty != null && IncludeProperty(declaredProperty, type, baseDefinitions, flattenHierarchy)) {
                        yield return declaredProperty;
                    }
                } else {
                    foreach (var declaredProperty in ancestor.GetDeclaredProperties()) {
                        if (IncludeProperty(declaredProperty, type, baseDefinitions, flattenHierarchy)) {
                            yield return declaredProperty;
                        }
                    }
                }
            }
        }

        // CLI spec 22.34 Properties
        // -------------------------
        // [Note: The CLS (see Partition I) refers to instance, virtual, and static properties.  
        // The signature of a property (from the Type column) can be used to distinguish a static property, 
        // since instance and virtual properties will have the "HASTHIS" bit set in the signature (§23.2.1)
        // while a static property will not.  The distinction between an instance and a virtual property 
        // depends on the signature of the getter and setter methods, which the CLS requires to be either 
        // both virtual or both instance. end note]
        private static bool IncludeProperty(PropertyInfo member, Type reflectedType, HashSet<MethodInfo> baseDefinitions, bool flattenHierarchy) {
            var getter = member.GetGetMethod(nonPublic: true);
            var setter = member.GetSetMethod(nonPublic: true);

            MethodInfo virtualAccessor;
            if (getter != null && getter.IsVirtual) {
                virtualAccessor = getter;
            } else if (setter != null && setter.IsVirtual) {
                virtualAccessor = setter;
            } else {
                virtualAccessor = null;
            }

            if (virtualAccessor != null) {
                if (baseDefinitions.Add(RuntimeReflectionExtensions.GetRuntimeBaseDefinition(virtualAccessor))) {
                    return true;
                }
            } else if (member.DeclaringType == reflectedType) {
                return true;
            } else if (!member.IsPrivate() && (!member.IsStatic() || flattenHierarchy)) {
                return true;
            }

            return false;
        }

        public static IEnumerable<EventInfo> GetInheritedEvents(this Type type, string name = null, bool flattenHierarchy = false) {
            while (type.IsGenericParameter) {
                type = type.BaseType;
            }

            var baseDefinitions = new HashSet<MethodInfo>(ReferenceEqualityComparer<MethodInfo>.Instance);
            foreach (var ancestor in type.Ancestors()) {
                if (name != null) {
                    var declaredEvent = ancestor.GetDeclaredEvent(name);
                    if (declaredEvent != null && IncludeEvent(declaredEvent, type, baseDefinitions, flattenHierarchy)) {
                        yield return declaredEvent;
                    }
                } else {
                    foreach (var declaredEvent in ancestor.GetDeclaredEvents()) {
                        if (IncludeEvent(declaredEvent, type, baseDefinitions, flattenHierarchy)) {
                            yield return declaredEvent;
                        }
                    }
                }
            }
        }

        private static bool IncludeEvent(EventInfo member, Type reflectedType, HashSet<MethodInfo> baseDefinitions, bool flattenHierarchy) {
            var add = member.GetAddMethod(nonPublic: true);
            var remove = member.GetRemoveMethod(nonPublic: true);

            // TOOD: fire method?

            MethodInfo virtualAccessor;
            if (add != null && add.IsVirtual) {
                virtualAccessor = add;
            } else if (remove != null && remove.IsVirtual) {
                virtualAccessor = remove;
            } else {
                virtualAccessor = null;
            }

            if (virtualAccessor != null) {
                if (baseDefinitions.Add(RuntimeReflectionExtensions.GetRuntimeBaseDefinition(virtualAccessor))) {
                    return true;
                }
            } else if (member.DeclaringType == reflectedType) {
                return true;
            } else if (!member.IsPrivate() && (!member.IsStatic() || flattenHierarchy)) {
                return true;
            }

            return false;
        }

        public static IEnumerable<FieldInfo> GetInheritedFields(this Type type, string name = null, bool flattenHierarchy = false) {
            while (type.IsGenericParameter) {
                type = type.BaseType;
            }

            foreach (var ancestor in type.Ancestors()) {
                if (name != null) {
                    var declaredField = ancestor.GetDeclaredField(name);
                    if (declaredField != null && IncludeField(declaredField, type, flattenHierarchy)) {
                        yield return declaredField;
                    }
                } else {
                    foreach (var declaredField in ancestor.GetDeclaredFields()) {
                        if (IncludeField(declaredField, type, flattenHierarchy)) {
                            yield return declaredField;
                        }
                    }
                }
            }
        }

        private static bool IncludeField(FieldInfo member, Type reflectedType, bool flattenHierarchy) {
            if (member.DeclaringType == reflectedType) {
                return true;
            }

            if (!member.IsPrivate && (!member.IsStatic || flattenHierarchy)) {
                return true;
            }

            return false;
        }

        public static IEnumerable<MemberInfo> GetInheritedMembers(this Type type, string name = null, bool flattenHierarchy = false) {
            var result =
                type.GetInheritedMethods(name, flattenHierarchy).Cast<MethodInfo, MemberInfo>().Concat(
                type.GetInheritedProperties(name, flattenHierarchy).Cast<PropertyInfo, MemberInfo>().Concat(
                type.GetInheritedEvents(name, flattenHierarchy).Cast<EventInfo, MemberInfo>().Concat(
                type.GetInheritedFields(name, flattenHierarchy).Cast<FieldInfo, MemberInfo>())));

            if (name == null) {
                return result.Concat<MemberInfo>(
                    type.GetDeclaredConstructors().Cast<ConstructorInfo, MemberInfo>().Concat(
                    type.GetDeclaredNestedTypes().Cast<TypeInfo, MemberInfo>()));
            }

            var nestedType = type.GetDeclaredNestedType(name);
            return (nestedType != null) ? result.Concat(new[] { nestedType }) : result;
        }

        #endregion

        #region Declared Members

        public static IEnumerable<ConstructorInfo> GetDeclaredConstructors(this Type type) {
            return type.GetConstructors(BindingFlags.DeclaredOnly | AllMembers);
        }

        public static IEnumerable<MethodInfo> GetDeclaredMethods(this Type type, string name = null) {
            if (name == null) {
                return type.GetMethods(BindingFlags.DeclaredOnly | AllMembers);
            }

            return type.GetMember(name, MemberTypes.Method, BindingFlags.DeclaredOnly | AllMembers).OfType<MethodInfo>();
        }

        public static IEnumerable<PropertyInfo> GetDeclaredProperties(this Type type) {
            return type.GetProperties(BindingFlags.DeclaredOnly | AllMembers);
        }

        public static PropertyInfo GetDeclaredProperty(this Type type, string name) {
            Debug.Assert(name != null);
            return type.GetProperty(name, BindingFlags.DeclaredOnly | AllMembers);
        }

        public static IEnumerable<EventInfo> GetDeclaredEvents(this Type type) {
            return type.GetEvents(BindingFlags.DeclaredOnly | AllMembers);
        }

        public static EventInfo GetDeclaredEvent(this Type type, string name) {
            Debug.Assert(name != null);
            return type.GetEvent(name, BindingFlags.DeclaredOnly | AllMembers);
        }

        public static IEnumerable<FieldInfo> GetDeclaredFields(this Type type) {
            return type.GetFields(BindingFlags.DeclaredOnly | AllMembers);
        }

        public static FieldInfo GetDeclaredField(this Type type, string name) {
            Debug.Assert(name != null);
            return type.GetField(name, BindingFlags.DeclaredOnly | AllMembers);
        }

        public static IEnumerable<TypeInfo> GetDeclaredNestedTypes(this Type type) {
            return type.GetNestedTypes(BindingFlags.DeclaredOnly | AllMembers);
        }

        public static TypeInfo GetDeclaredNestedType(this Type type, string name) {
            Debug.Assert(name != null);
            return type.GetNestedType(name, BindingFlags.DeclaredOnly | AllMembers);
        }

        public static IEnumerable<MemberInfo> GetDeclaredMembers(this Type type, string name = null) {
            if (name == null) {
                return type.GetMembers(BindingFlags.DeclaredOnly | AllMembers);
            }

            return type.GetMember(name, BindingFlags.DeclaredOnly | AllMembers);
        }

        #endregion

        public static Type[] GetGenericTypeArguments(this Type type) {
            return type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericArguments() : null;
        }

        public static Type[] GetGenericTypeParameters(this Type type) {
            return type.IsGenericTypeDefinition ? type.GetGenericArguments() : null;
        }

        [Obsolete("Use Assembly.GetModules directly instead.")]
        public static IEnumerable<Module> GetModules(this Assembly assembly) {
            return assembly.GetModules();
        }

        [Obsolete("Use Type.GetInterfaces directly instead.")]
        public static IEnumerable<Type> GetImplementedInterfaces(this Type type) {
            return type.GetInterfaces();
        }

        public static TypeCode GetTypeCode(this Type type) {
            return Type.GetTypeCode(type);
        }

        [Obsolete("Use Delegate.GetMethodInfo directly instead.")]
        public static MethodInfo GetMethod(this Delegate d) {
            return d.GetMethodInfo();
        }

        public static bool IsDefined(this Assembly assembly, Type attributeType) {
            return assembly.IsDefined(attributeType, false);
        }

        public static T GetCustomAttribute<T>(this Assembly assembly, bool inherit = false) where T : Attribute {
            return (T)Attribute.GetCustomAttribute(assembly, typeof(T), inherit);
        }

        public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit = false) where T : Attribute {
            return (T)Attribute.GetCustomAttribute(member, typeof(T), inherit);
        }

        [Obsolete("Use Type.ContainsGenericParameters directly instead.")]
        public static bool ContainsGenericParameters(this Type type) {
            return type.ContainsGenericParameters;
        }

        [Obsolete("Use Type.IsInterface directly instead.")]
        public static bool IsInterface(this Type type) {
            return type.IsInterface;
        }

        [Obsolete("Use Type.IsClass directly instead.")]
        public static bool IsClass(this Type type) {
            return type.IsClass;
        }

        [Obsolete("Use Type.IsGenericType directly instead.")]
        public static bool IsGenericType(this Type type) {
            return type.IsGenericType;
        }

        [Obsolete("Use Type.IsGenericTypeDefinition directly instead.")]
        public static bool IsGenericTypeDefinition(this Type type) {
            return type.IsGenericTypeDefinition;
        }

        [Obsolete("Use Type.IsSealed directly instead.")]
        public static bool IsSealed(this Type type) {
            return type.IsSealed;
        }

        [Obsolete("Use Type.IsAbstract directly instead.")]
        public static bool IsAbstract(this Type type) {
            return type.IsAbstract;
        }

        [Obsolete("Use Type.IsPublic directly instead.")]
        public static bool IsPublic(this Type type) {
            return type.IsPublic;
        }

        [Obsolete("Use Type.IsVisible directly instead.")]
        public static bool IsVisible(this Type type) {
            return type.IsVisible;
        }

        [Obsolete("Use Type.BaseType directly instead.")]
        public static Type GetBaseType(this Type type) {
            return type.BaseType;
        }

        [Obsolete("Use Type.IsValueType directly instead.")]
        public static bool IsValueType(this Type type) {
            return type.IsValueType;
        }

        [Obsolete("Use Type.IsEnum directly instead.")]
        public static bool IsEnum(this Type type) {
            return type.IsEnum;
        }

        [Obsolete("Use Type.IsPrimitive directly instead.")]
        public static bool IsPrimitive(this Type type) {
            return type.IsPrimitive;
        }

        [Obsolete("Use Type.GenericParameterAttributes directly instead.")]
        public static GenericParameterAttributes GetGenericParameterAttributes(this Type type) {
            return type.GenericParameterAttributes;
        }

        public static readonly Type[] EmptyTypes = Array.Empty<TypeInfo>();

        public static object GetRawConstantValue(this FieldInfo field) {
            if (!field.IsLiteral) {
                throw new ArgumentException(field + " not a literal.");
            }

            object value = field.GetValue(null);
            return field.FieldType.IsEnum ? UnwrapEnumValue(value) : value;
        }

        /// <summary>
        /// Converts a boxed enum value to the underlying integer value.
        /// </summary>
        public static object UnwrapEnumValue(object value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            switch (value.GetType().GetTypeCode()) {
                case TypeCode.Byte:
                    return Convert.ToByte(value);

                case TypeCode.Int16:
                    return Convert.ToInt16(value);

                case TypeCode.Int32:
                    return Convert.ToInt32(value);

                case TypeCode.Int64:
                    return Convert.ToInt64(value);

                case TypeCode.SByte:
                    return Convert.ToSByte(value);

                case TypeCode.UInt16:
                    return Convert.ToUInt16(value);

                case TypeCode.UInt32:
                    return Convert.ToUInt32(value);

                case TypeCode.UInt64:
                    return Convert.ToUInt64(value);

                default:
                    throw new ArgumentException("Value must be a boxed enum.", nameof(value));
            }
        }

#if FEATURE_REFEMIT
#if FEATURE_ASSEMBLYBUILDER_DEFINEDYNAMICASSEMBLY
        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access) {
            return AssemblyBuilder.DefineDynamicAssembly(name, access);
        }
#else
        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access) {
            return AppDomain.CurrentDomain.DefineDynamicAssembly(name, access);
        }
#endif
#if !FEATURE_PDBEMIT
        public static ModuleBuilder DefineDynamicModule(this AssemblyBuilder assembly, string name, bool emitDebugInfo) {
            // ignore the flag
            return assembly.DefineDynamicModule(name);
        }
#endif
#endif

        #region Signature and Type Formatting

        // Generic type names have the arity (number of generic type paramters) appended at the end. 
        // For eg. the mangled name of System.List<T> is "List`1". This mangling is done to enable multiple 
        // generic types to exist as long as they have different arities.
        public const char GenericArityDelimiter = '`';

        public static StringBuilder FormatSignature(StringBuilder result, MethodBase method) {
            return FormatSignature(result, method, t => t.FullName);
        }

        public static StringBuilder FormatSignature(StringBuilder result, MethodBase method, Func<Type, string> nameDispenser) {
            ContractUtils.RequiresNotNull(result, nameof(result));
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(nameDispenser, nameof(nameDispenser));

            MethodInfo methodInfo = method as MethodInfo;
            if (methodInfo != null) {
                FormatTypeName(result, methodInfo.ReturnType, nameDispenser);
                result.Append(' ');
            }

#if FEATURE_REFEMIT && FEATURE_REFEMIT_FULL
            MethodBuilder builder = method as MethodBuilder;
            if (builder != null) {
                result.Append(builder.Signature);
                return result;
            }

            ConstructorBuilder cb = method as ConstructorBuilder;
            if (cb != null) {
                result.Append(cb.Signature);
                return result;
            }
#endif
            FormatTypeName(result, method.DeclaringType, nameDispenser);
            result.Append("::");
            result.Append(method.Name);

            if (!method.IsConstructor) {
                FormatTypeArgs(result, method.GetGenericArguments(), nameDispenser);
            }

            result.Append("(");

            if (!method.ContainsGenericParameters) {
                ParameterInfo[] ps = method.GetParameters();
                for (int i = 0; i < ps.Length; i++) {
                    if (i > 0) result.Append(", ");
                    FormatTypeName(result, ps[i].ParameterType, nameDispenser);
                    if (!System.String.IsNullOrEmpty(ps[i].Name)) {
                        result.Append(" ");
                        result.Append(ps[i].Name);
                    }
                }
            } else {
                result.Append("?");
            }

            result.Append(")");
            return result;
        }

        public static StringBuilder FormatTypeName(StringBuilder result, Type type) {
            return FormatTypeName(result, type, t => t.FullName);
        }

        public static StringBuilder FormatTypeName(StringBuilder result, Type type, Func<Type, string> nameDispenser) {
            ContractUtils.RequiresNotNull(result, nameof(result));
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(nameDispenser, nameof(nameDispenser));
            if (type.IsGenericType) {
                Type genType = type.GetGenericTypeDefinition();
                string genericName = nameDispenser(genType).Replace('+', '.');
                int tickIndex = genericName.IndexOf('`');
                result.Append(tickIndex != -1 ? genericName.Substring(0, tickIndex) : genericName);

                Type[] typeArgs = type.GetGenericArguments();
                if (type.IsGenericTypeDefinition) {
                    result.Append('<');
                    result.Append(',', typeArgs.Length - 1);
                    result.Append('>');
                } else {
                    FormatTypeArgs(result, typeArgs, nameDispenser);
                }
            } else if (type.IsGenericParameter) {
                result.Append(type.Name);
            } else {
                // cut namespace off:
                result.Append(nameDispenser(type).Replace('+', '.'));
            }
            return result;
        }

        public static StringBuilder FormatTypeArgs(StringBuilder result, Type[] types) {
            return FormatTypeArgs(result, types, (t) => t.FullName);
        }

        public static StringBuilder FormatTypeArgs(StringBuilder result, Type[] types, Func<Type, string> nameDispenser) {
            ContractUtils.RequiresNotNull(result, nameof(result));
            ContractUtils.RequiresNotNullItems(types, nameof(types));
            ContractUtils.RequiresNotNull(nameDispenser, nameof(nameDispenser));

            if (types.Length > 0) {
                result.Append("<");

                for (int i = 0; i < types.Length; i++) {
                    if (i > 0) result.Append(", ");
                    FormatTypeName(result, types[i], nameDispenser);
                }

                result.Append(">");
            }
            return result;
        }

        internal static string ToValidTypeName(string str) {
            if (String.IsNullOrEmpty(str)) {
                return "_";
            }

            StringBuilder sb = new StringBuilder(str);
            for (int i = 0; i < str.Length; i++) {
                if (str[i] == '\0' || str[i] == '.' || str[i] == '*' || str[i] == '+' || str[i] == '[' || str[i] == ']' || str[i] == '\\') {
                    sb[i] = '_';
                }
            }
            return sb.ToString();
        }

        public static string GetNormalizedTypeName(Type type) {
            string name = type.Name;
            if (type.IsGenericType) {
                return GetNormalizedTypeName(name);
            }
            return name;
        }

        public static string GetNormalizedTypeName(string typeName) {
            Debug.Assert(typeName.IndexOf('.') == -1); // This is the simple name, not the full name
            int backtick = typeName.IndexOf(GenericArityDelimiter);
            if (backtick != -1) return typeName.Substring(0, backtick);
            return typeName;
        }

        #endregion

        #region Delegates and Dynamic Methods

        /// <summary>
        /// Creates an open delegate for the given (dynamic)method.
        /// </summary>
        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType) {
            return CreateDelegate(methodInfo, delegateType, null);
        }

        /// <summary>
        /// Creates a closed delegate for the given (dynamic)method.
        /// </summary>
        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType, object target) {
#if FEATURE_LCG
            if (methodInfo is DynamicMethod dm) {
                return dm.CreateDelegate(delegateType, target);
            }
#endif
            return Delegate.CreateDelegate(delegateType, target, methodInfo);
        }

#if FEATURE_LCG
        public static bool IsDynamicMethod(MethodBase method) => IsDynamicMethodInternal(method);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsDynamicMethodInternal(MethodBase method) {
            return method is DynamicMethod;
        }
#else
        public static bool IsDynamicMethod(MethodBase method) {
            return false;
        }
#endif

        public static void GetDelegateSignature(Type delegateType, out ParameterInfo[] parameterInfos, out ParameterInfo returnInfo) {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));

            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ContractUtils.Requires(invokeMethod != null, nameof(delegateType), Strings.InvalidDelegate);

            parameterInfos = invokeMethod.GetParameters();
            returnInfo = invokeMethod.ReturnParameter;
        }

        /// <summary>
        /// Gets a Func of CallSite, object * paramCnt, object delegate type
        /// that's suitable for use in a non-strongly typed call site.
        /// </summary>
        public static Type GetObjectCallSiteDelegateType(int paramCnt) {
            switch (paramCnt) {
                case 0: return typeof(Func<CallSite, object, object>);
                case 1: return typeof(Func<CallSite, object, object, object>);
                case 2: return typeof(Func<CallSite, object, object, object, object>);
                case 3: return typeof(Func<CallSite, object, object, object, object, object>);
                case 4: return typeof(Func<CallSite, object, object, object, object, object, object>);
                case 5: return typeof(Func<CallSite, object, object, object, object, object, object, object>);
                case 6: return typeof(Func<CallSite, object, object, object, object, object, object, object, object>);
                case 7: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object>);
                case 8: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object>);
                case 9: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object, object>);
                case 10: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object, object, object>);
                case 11: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                case 12: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                case 13: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                case 14: return typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                default:
#if FEATURE_REFEMIT
                    Type[] paramTypes = new Type[paramCnt + 2];
                    paramTypes[0] = typeof(CallSite);
                    paramTypes[1] = typeof(object);
                    for (int i = 0; i < paramCnt; i++) {
                        paramTypes[i + 2] = typeof(object);
                    }
                    return Snippets.Shared.DefineDelegate("InvokeDelegate" + paramCnt, typeof(object), paramTypes);
#else
                    throw new NotSupportedException("Signature not supported on this platform.");
#endif
            }
        }

#if FEATURE_LCG
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework")]
        internal static DynamicMethod RawCreateDynamicMethod(string name, Type returnType, Type[] parameterTypes) {

            //
            // WARNING: we set restrictedSkipVisibility == true  (last parameter)
            //          setting this bit will allow accessing nonpublic members
            //          for more information see http://msdn.microsoft.com/en-us/library/bb348332.aspx
            //
            return new DynamicMethod(name, returnType, parameterTypes, true);
        }
#endif

        #endregion

        #region Methods and Parameters

        public static MethodBase[] GetMethodInfos(MemberInfo[] members) {
            return ArrayUtils.ConvertAll<MemberInfo, MethodBase>(
                members,
                delegate (MemberInfo inp) { return (MethodBase)inp; });
        }

        public static Type[] GetParameterTypes(ParameterInfo[] parameterInfos) {
            return GetParameterTypes((IList<ParameterInfo>)parameterInfos);
        }

        public static Type[] GetParameterTypes(IList<ParameterInfo> parameterInfos) {
            Type[] result = new Type[parameterInfos.Count];
            for (int i = 0; i < result.Length; i++) {
                result[i] = parameterInfos[i].ParameterType;
            }
            return result;
        }

        public static Type GetReturnType(this MethodBase mi) {
            return (mi.IsConstructor) ? mi.DeclaringType : ((MethodInfo)mi).ReturnType;
        }

        public static bool SignatureEquals(MethodInfo method, params Type[] requiredSignature) {
            ContractUtils.RequiresNotNull(method, nameof(method));

            Type[] actualTypes = GetParameterTypes(method.GetParameters());
            Debug.Assert(actualTypes.Length == requiredSignature.Length - 1);
            int i = 0;
            while (i < actualTypes.Length) {
                if (actualTypes[i] != requiredSignature[i]) return false;
                i++;
            }

            return method.ReturnType == requiredSignature[i];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool IsExtension(this MemberInfo member) {
            var dlrExtension = typeof(ExtensionAttribute);
            if (member.IsDefined(dlrExtension, false)) {
                return true;
            }

            return false;
        }

        public static bool IsOutParameter(this ParameterInfo pi) {
            // not using IsIn/IsOut properties as they are not available in Silverlight:
            return pi.ParameterType.IsByRef && (pi.Attributes & (ParameterAttributes.Out | ParameterAttributes.In)) == ParameterAttributes.Out;
        }

        /// <summary>
        /// Returns <c>true</c> if the specified parameter is mandatory, i.e. is not optional and doesn't have a default value.
        /// </summary>
        public static bool IsMandatory(this ParameterInfo pi) {
            return (pi.Attributes & ParameterAttributes.Optional) == 0 && !pi.HasDefaultValue();
        }

        public static bool HasDefaultValue(this ParameterInfo pi) {
            return (pi.Attributes & ParameterAttributes.HasDefault) != 0;
        }

        public static bool ProhibitsNull(this ParameterInfo parameter) {
            return parameter.IsDefined(typeof(NotNullAttribute), false);
        }

        public static bool ProhibitsNullItems(this ParameterInfo parameter) {
            return parameter.IsDefined(typeof(NotNullItemsAttribute), false);
        }

        public static bool IsParamArray(this ParameterInfo parameter) {
            return parameter.IsDefined(typeof(ParamArrayAttribute), false);
        }

        public static bool IsParamDictionary(this ParameterInfo parameter) {
            return parameter.IsDefined(typeof(ParamDictionaryAttribute), false);
        }

        public static bool IsParamsMethod(MethodBase method) {
            return IsParamsMethod(method.GetParameters());
        }

        public static bool IsParamsMethod(ParameterInfo[] pis) {
            foreach (ParameterInfo pi in pis) {
                if (pi.IsParamArray() || pi.IsParamDictionary()) return true;
            }
            return false;
        }

        public static object GetDefaultValue(this ParameterInfo info) {
            return info.DefaultValue;
        }

        #endregion

        #region Types

        /// <summary>
        /// Yields all ancestors of the given type including the type itself.
        /// Does not include implemented interfaces.
        /// </summary>
        public static IEnumerable<Type> Ancestors(this Type type) {
            do {
                yield return type;
                type = type.BaseType;
            } while (type != null);
        }

        /// <summary>
        /// Like Type.GetInterfaces, but only returns the interfaces implemented by this type
        /// and not its parents.
        /// </summary>
        public static List<Type> GetDeclaredInterfaces(Type type) {
            IEnumerable<Type> baseInterfaces = (type.BaseType != null) ? type.BaseType.GetInterfaces() : EmptyTypes;
            List<Type> interfaces = new List<Type>();
            foreach (Type iface in type.GetInterfaces()) {
                if (!baseInterfaces.Contains(iface)) {
                    interfaces.Add(iface);
                }
            }
            return interfaces;
        }

        internal static IEnumerable<TypeInfo> GetAllTypesFromAssembly(Assembly asm) {
            foreach (Module module in asm.GetModules()) {
                Type[] moduleTypes;
                try {
                    moduleTypes = module.GetTypes();
                } catch (ReflectionTypeLoadException e) {
                    moduleTypes = e.Types;
                }

                foreach (var type in moduleTypes) {
                    if (type != null) {
                        yield return type;
                    }
                }
            }
        }


#if !FEATURE_ASSEMBLY_GETFORWARDEDTYPES
#if NETSTANDARD2_0
        private static readonly MethodInfo GetForwardedTypesMethodInfo = typeof(Assembly).GetMethod("GetForwardedTypes", Array.Empty<Type>());

        internal static Type[] GetForwardedTypes(this Assembly assembly) {
            if (GetForwardedTypesMethodInfo != null) {
                // just in case we're running on .NET Core 2.1...
                try {
                    return GetForwardedTypesMethodInfo.Invoke(assembly, null) as Type[] ?? Array.Empty<Type>();
                } catch (TargetInvocationException ex) {
                    throw ex.InnerException;
                }
            }
            return Array.Empty<Type>();
        }
#else
        internal static Type[] GetForwardedTypes(this Assembly assembly) => EmptyTypes;
#endif
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static IEnumerable<TypeInfo> GetAllTypesFromAssembly(Assembly assembly, bool includePrivateTypes) {
            ContractUtils.RequiresNotNull(assembly, nameof(assembly));

            if (includePrivateTypes) {
                return GetAllTypesFromAssembly(assembly);
            }

            try {
                var exportedTypes = assembly.GetExportedTypes();
                try {
                    var forwardedTypes = assembly.GetForwardedTypes();
                    return Enumerable.Concat(exportedTypes, forwardedTypes);
                } catch (ReflectionTypeLoadException ex) {
                    // GetForwardedTypes can throw if an assembly failed to load. In this case add the types
                    // which successfully loaded. Note that Types may include null so we need to filter it out.
                    return Enumerable.Concat(exportedTypes, ex.Types.OfType<Type>());
                }
            } catch (NotSupportedException) {
                // GetExportedTypes does not work with dynamic assemblies
            } catch (Exception) {
                // Some type loads may cause exceptions. Unfortunately, there is no way to ask GetExportedTypes
                // for just the list of types that we successfully loaded.
            }

            return GetAllTypesFromAssembly(assembly).Where(type => type.IsPublic);
        }

        #endregion

        #region Type Builder
#if FEATURE_REFEMIT

        private const MethodAttributes MethodAttributesToEraseInOveride = MethodAttributes.Abstract | MethodAttributes.ReservedMask;

        public static MethodBuilder DefineMethodOverride(TypeBuilder tb, MethodAttributes extra, MethodInfo decl) {
            MethodAttributes finalAttrs = (decl.Attributes & ~MethodAttributesToEraseInOveride) | extra;
            if (!decl.DeclaringType.IsInterface) {
                finalAttrs &= ~MethodAttributes.NewSlot;
            }

            if ((extra & MethodAttributes.MemberAccessMask) != 0) {
                // remove existing member access, add new member access
                finalAttrs &= ~MethodAttributes.MemberAccessMask;
                finalAttrs |= extra;
            }

            MethodBuilder impl = tb.DefineMethod(decl.Name, finalAttrs, decl.CallingConvention);
            CopyMethodSignature(decl, impl, false);
            return impl;
        }

        public static void CopyMethodSignature(MethodInfo from, MethodBuilder to, bool substituteDeclaringType) {
            ParameterInfo[] paramInfos = from.GetParameters();
            Type[] parameterTypes = new Type[paramInfos.Length];
            Type[][] parameterRequiredModifiers = null, parameterOptionalModifiers = null;
            Type[] returnRequiredModifiers = null, returnOptionalModifiers = null;

            returnRequiredModifiers = from.ReturnParameter.GetRequiredCustomModifiers();
            returnOptionalModifiers = from.ReturnParameter.GetOptionalCustomModifiers();
            for (int i = 0; i < paramInfos.Length; i++) {
                if (substituteDeclaringType && paramInfos[i].ParameterType == from.DeclaringType) {
                    parameterTypes[i] = to.DeclaringType;
                } else {
                    parameterTypes[i] = paramInfos[i].ParameterType;
                }

                var mods = paramInfos[i].GetRequiredCustomModifiers();
                if (mods.Length > 0) {
                    if (parameterRequiredModifiers == null) {
                        parameterRequiredModifiers = new Type[paramInfos.Length][];
                    }

                    parameterRequiredModifiers[i] = mods;
                }

                mods = paramInfos[i].GetOptionalCustomModifiers();
                if (mods.Length > 0) {
                    if (parameterOptionalModifiers == null) {
                        parameterOptionalModifiers = new Type[paramInfos.Length][];
                    }

                    parameterOptionalModifiers[i] = mods;
                }
            }

            to.SetSignature(
                from.ReturnType, returnRequiredModifiers, returnOptionalModifiers,
                parameterTypes, parameterRequiredModifiers, parameterOptionalModifiers
            );

            CopyGenericMethodAttributes(from, to);

            for (int i = 0; i < paramInfos.Length; i++) {
                var parameterBuilder = to.DefineParameter(i + 1, paramInfos[i].Attributes, paramInfos[i].Name);
                try { // ParameterBuilder.SetConstant is buggy and may fail on Mono
                    if (paramInfos[i].HasDefaultValue) parameterBuilder.SetConstant(paramInfos[i].RawDefaultValue);
                } catch { }
            }
        }

        private static void CopyGenericMethodAttributes(MethodInfo from, MethodBuilder to) {
            if (from.IsGenericMethodDefinition) {
                Type[] args = from.GetGenericArguments();
                string[] names = new string[args.Length];
                for (int i = 0; i < args.Length; i++) {
                    names[i] = args[i].Name;
                }
                var builders = to.DefineGenericParameters(names);
                for (int i = 0; i < args.Length; i++) {
                    // Copy template parameter attributes
                    builders[i].SetGenericParameterAttributes(args[i].GenericParameterAttributes);

                    // Copy template parameter constraints
                    Type[] constraints = args[i].GetGenericParameterConstraints();
                    List<Type> interfaces = new List<Type>(constraints.Length);
                    foreach (Type constraint in constraints) {
                        if (constraint.IsInterface) {
                            interfaces.Add(constraint);
                        } else {
                            builders[i].SetBaseTypeConstraint(constraint);
                        }
                    }
                    if (interfaces.Count > 0) {
                        builders[i].SetInterfaceConstraints(interfaces.ToArray());
                    }
                }
            }
        }
#endif
        #endregion

        #region Extension Methods

        public static IEnumerable<MethodInfo> GetVisibleExtensionMethods(Assembly assembly) {
#if FEATURE_METADATA_READER
            if (!assembly.IsDynamic && AppDomain.CurrentDomain.IsFullyTrusted) {
                try {
                    return GetVisibleExtensionMethodsFast(assembly);
                } catch (SecurityException) {
                    // full-demand can still fail if there is a partial trust domain on the stack
                }
            }
#endif
            return GetVisibleExtensionMethodsSlow(assembly);
        }

#if FEATURE_METADATA_READER
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2116:AptcaMethodsShouldOnlyCallAptcaMethods")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<MethodInfo> GetVisibleExtensionMethodsFast(Assembly assembly) {
            // Security: link demand
            return MetadataServices.GetVisibleExtensionMethodInfos(assembly);
        }
#endif

        // TODO: make internal
        // TODO: handle type load exceptions
        public static IEnumerable<MethodInfo> GetVisibleExtensionMethodsSlow(Assembly assembly) {
            var ea = typeof(ExtensionAttribute);
            if (assembly.IsDefined(ea)) {
                foreach (TypeInfo type in GetAllTypesFromAssembly(assembly)) {
                    if ((type.IsPublic || type.IsNestedPublic) &&
                        type.IsAbstract &&
                        type.IsSealed &&
                        type.IsDefined(ea, false)) {

                        foreach (MethodInfo method in type.GetDeclaredMethods()) {
                            if (method.IsPublic && method.IsStatic && method.IsDefined(ea, false)) {
                                yield return method;
                            }
                        }
                    }
                }
            }
        }

        // Value is null if there are no extension methods in the assembly.
        private static Dictionary<Assembly, Dictionary<string, List<ExtensionMethodInfo>>> _extensionMethodsCache;

        /// <summary>
        /// Enumerates extension methods in given assembly. Groups the methods by declaring namespace.
        /// Uses a global cache if <paramref name="useCache"/> is true.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, IEnumerable<ExtensionMethodInfo>>> GetVisibleExtensionMethodGroups(Assembly/*!*/ assembly, bool useCache) {
#if FEATURE_REFEMIT
            useCache &= !assembly.IsDynamic;
#endif
            if (useCache) {
                if (_extensionMethodsCache == null) {
                    _extensionMethodsCache = new Dictionary<Assembly, Dictionary<string, List<ExtensionMethodInfo>>>();
                }

                lock (_extensionMethodsCache) {
                    if (_extensionMethodsCache.TryGetValue(assembly, out Dictionary<string, List<ExtensionMethodInfo>> existing)) {
                        return EnumerateExtensionMethods(existing);
                    }
                }
            }

            Dictionary<string, List<ExtensionMethodInfo>> result = null;
            foreach (MethodInfo method in GetVisibleExtensionMethodsSlow(assembly)) {
                if (method.DeclaringType == null || method.DeclaringType.IsGenericTypeDefinition) {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 0) {
                    continue;
                }

                Type type = parameters[0].ParameterType;
                if (type.IsByRef || type.IsPointer) {
                    continue;
                }

                string ns = method.DeclaringType.Namespace ?? string.Empty;

                if (result == null) {
                    result = new Dictionary<string, List<ExtensionMethodInfo>>();
                }

                if (!result.TryGetValue(ns, out List<ExtensionMethodInfo> extensions)) {
                    result.Add(ns, extensions = new List<ExtensionMethodInfo>());
                }

                extensions.Add(new ExtensionMethodInfo(type, method));
            }

            if (useCache) {
                lock (_extensionMethodsCache) {
                    _extensionMethodsCache[assembly] = result;
                }
            }

            return EnumerateExtensionMethods(result);
        }

        // TODO: GetVisibleExtensionMethods(Hashset<string> namespaces, Type type, string methodName) : IEnumerable<MethodInfo> {}

        private static IEnumerable<KeyValuePair<string, IEnumerable<ExtensionMethodInfo>>> EnumerateExtensionMethods(Dictionary<string, List<ExtensionMethodInfo>> dict) {
            if (dict != null) {
                foreach (var entry in dict) {
                    yield return new KeyValuePair<string, IEnumerable<ExtensionMethodInfo>>(entry.Key, new ReadOnlyCollection<ExtensionMethodInfo>(entry.Value));
                }
            }
        }

        #endregion

        #region Generic Types

        internal static Dictionary<Type, Type> BindGenericParameters(Type/*!*/ openType, Type/*!*/ closedType, bool ignoreUnboundParameters) {
            var binding = new Dictionary<Type, Type>();
            BindGenericParameters(openType, closedType, (parameter, type) => {
                if (binding.TryGetValue(parameter, out Type existing)) {
                    return type == existing;
                }

                binding[parameter] = type;

                return true;
            });

            return ConstraintsViolated(binding, ignoreUnboundParameters) ? null : binding;
        }

        /// <summary>
        /// Binds occurances of generic parameters in <paramref name="openType"/> against corresponding types in <paramref name="closedType"/>.
        /// Invokes <paramref name="binder"/>(parameter, type) for each such binding.
        /// Returns false if the <paramref name="openType"/> is structurally different from <paramref name="closedType"/> or if the binder returns false.
        /// </summary>
        internal static bool BindGenericParameters(Type/*!*/ openType, Type/*!*/ closedType, Func<Type, Type, bool>/*!*/ binder) {
            if (openType.IsGenericParameter) {
                return binder(openType, closedType);
            }

            if (openType.IsArray) {
                if (!closedType.IsArray) {
                    return false;
                }
                return BindGenericParameters(openType.GetElementType(), closedType.GetElementType(), binder);
            }

            if (!openType.IsGenericType || !closedType.IsGenericType) {
                return openType == closedType;
            }

            if (openType.GetGenericTypeDefinition() != closedType.GetGenericTypeDefinition()) {
                return false;
            }

            Type[] closedArgs = closedType.GetGenericArguments();
            Type[] openArgs = openType.GetGenericArguments();

            for (int i = 0; i < openArgs.Length; i++) {
                if (!BindGenericParameters(openArgs[i], closedArgs[i], binder)) {
                    return false;
                }
            }

            return true;
        }

        internal static bool ConstraintsViolated(Dictionary<Type, Type>/*!*/ binding, bool ignoreUnboundParameters) {
            foreach (var entry in binding) {
                if (ConstraintsViolated(entry.Key, entry.Value, binding, ignoreUnboundParameters)) {
                    return true;
                }
            }

            return false;
        }

        internal static bool ConstraintsViolated(Type/*!*/ genericParameter, Type/*!*/ closedType, Dictionary<Type, Type>/*!*/ binding, bool ignoreUnboundParameters) {
            if ((genericParameter.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && closedType.IsValueType) {
                // value type to parameter type constrained as class
                return true;
            }

            if ((genericParameter.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 &&
                (!closedType.IsValueType || (closedType.IsGenericType && closedType.GetGenericTypeDefinition() == typeof(Nullable<>)))) {
                // nullable<T> or class/interface to parameter type constrained as struct
                return true;
            }

            if ((genericParameter.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                (!closedType.IsValueType && closedType.GetConstructor(EmptyTypes) == null)) {
                // reference type w/o a default constructor to type constrianed as new()
                return true;
            }

            Type[] constraints = genericParameter.GetGenericParameterConstraints();
            for (int i = 0; i < constraints.Length; i++) {
                Type instantiation = InstantiateConstraint(constraints[i], binding);

                if (instantiation == null) {
                    if (ignoreUnboundParameters) {
                        continue;
                    }

                    return true;
                }

                if (!instantiation.IsAssignableFrom(closedType)) {
                    return true;
                }
            }

            return false;
        }

        internal static Type InstantiateConstraint(Type/*!*/ constraint, Dictionary<Type, Type>/*!*/ binding) {
            Debug.Assert(!constraint.IsArray && !constraint.IsByRef && !constraint.IsGenericTypeDefinition);
            if (!constraint.ContainsGenericParameters) {
                return constraint;
            }

            Type closedType;
            if (constraint.IsGenericParameter) {
                return binding.TryGetValue(constraint, out closedType) ? closedType : null;
            }

            Type[] args = constraint.GetGenericArguments();
            for (int i = 0; i < args.Length; i++) {
                if ((args[i] = InstantiateConstraint(args[i], binding)) == null) {
                    return null;
                }
            }

            return constraint.GetGenericTypeDefinition().MakeGenericType(args);
        }

        #endregion
    }

    public struct ExtensionMethodInfo : IEquatable<ExtensionMethodInfo> {
        private readonly Type/*!*/ _extendedType; // cached type of the first parameter
        private readonly MethodInfo/*!*/ _method;

        internal ExtensionMethodInfo(Type/*!*/ extendedType, MethodInfo/*!*/ method) {
            Assert.NotNull(extendedType, method);
            _extendedType = extendedType;
            _method = method;
        }

        public Type/*!*/ ExtendedType {
            get { return _extendedType; }
        }

        public MethodInfo/*!*/ Method {
            get { return _method; }
        }

        public override bool Equals(object obj) =>
            obj is ExtensionMethodInfo info && Equals(info);

        public bool Equals(ExtensionMethodInfo other) =>
            _method.Equals(other._method);

        public static bool operator ==(ExtensionMethodInfo self, ExtensionMethodInfo other) => self.Equals(other);

        public static bool operator !=(ExtensionMethodInfo self, ExtensionMethodInfo other) => !self.Equals(other);

        public override int GetHashCode() {
            return _method.GetHashCode();
        }

        /// <summary>
        /// Determines if a given type matches the type that the method extends. 
        /// The match might be non-trivial if the extended type is an open generic type with constraints.
        /// </summary>
        public bool IsExtensionOf(Type/*!*/ type) {
            ContractUtils.RequiresNotNull(type, nameof(type));
#if FEATURE_TYPE_EQUIVALENCE
            if (type.IsEquivalentTo(ExtendedType)) {
                return true;
            }
#else
            if (type == _extendedType) {
                return true;
            }
#endif
            if (!_extendedType.ContainsGenericParameters) {
                return false;
            }

            //
            // Ignores constraints that can't be instantiated given the information we have (type of the first parameter).
            //
            // For example, 
            // void Foo<S, T>(this S x, T y) where S : T;
            //
            // We make such methods available on all types. 
            // If they are not called with arguments that satisfy the constraint the overload resolver might fail.
            //
            return ReflectionUtils.BindGenericParameters(_extendedType, type, true) != null;
        }
    }
}
