// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Utils {
    public static class TypeUtils {
        public static bool IsNested(this Type t) {
            return t.DeclaringType != null;
        }

        internal static Type GetNonNullableType(this Type type) => IsNullableType(type) ? type.GetGenericArguments()[0] : type;

        internal static Type GetNullableType(this Type type) {
            Debug.Assert(type != null, "type cannot be null");
            if (type.IsValueType && !IsNullableType(type)) {
                return typeof(Nullable<>).MakeGenericType(type);
            }

            return type;
        }

        internal static bool IsNullableType(this Type type) => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        internal static bool IsBool(this Type type) => GetNonNullableType(type) == typeof(bool);

        internal static bool IsNumeric(this Type type) {
            type = GetNonNullableType(type);
            if (!type.IsEnum) {
                return IsNumeric(type.GetTypeCode());
            }

            return false;
        }

        internal static bool IsNumeric(TypeCode typeCode) {
            switch (typeCode) {
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
            }
            return false;
        }

        internal static bool IsArithmetic(this Type type) {
            type = GetNonNullableType(type);
            if (!type.IsEnum) {
                switch (type.GetTypeCode()) {
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        internal static bool IsUnsignedInt(this Type type) {
            type = GetNonNullableType(type);
            if (!type.IsEnum) {
                switch (type.GetTypeCode()) {
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        internal static bool IsIntegerOrBool(this Type type) {
            type = GetNonNullableType(type);
            if (!type.IsEnum) {
                switch (type.GetTypeCode()) {
                    case TypeCode.Int64:
                    case TypeCode.Int32:
                    case TypeCode.Int16:
                    case TypeCode.UInt64:
                    case TypeCode.UInt32:
                    case TypeCode.UInt16:
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        return true;
                }
            }

            return false;
        }

        internal static bool CanAssign(Type to, Expression from) {
            if (CanAssign(to, from.Type)) return true;

            if (to.IsValueType() && 
                to.IsGenericType() && 
                to.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                ConstantCheck.Check(from, null)) {
                return true;
            }

            return false;
        }

        internal static bool CanAssign(Type to, Type from) {
            if (to == from) {
                return true;
            }
            // Reference types
            if (!to.IsValueType() && !from.IsValueType()) {
                if (to.IsAssignableFrom(from)) {
                    return true;
                }
                // Arrays can be assigned if they have same rank and assignable element types.
                if (to.IsArray && from.IsArray &&
                    to.GetArrayRank() == from.GetArrayRank() &&
                    CanAssign(to.GetElementType(), from.GetElementType())) {
                    return true;
                }
            } 

            return false;
        }

        internal static bool IsGeneric(Type type) {
            return type.ContainsGenericParameters() || type.IsGenericTypeDefinition();
        }

        internal static bool CanCompareToNull(Type type) {
            // This is a bit too conservative.
            return !type.IsValueType();
        }

        /// <summary>
        /// Returns a numerical code of the size of a type.  All types get both a horizontal
        /// and vertical code.  Types that are lower in both dimensions have implicit conversions
        /// to types that are higher in both dimensions.
        /// </summary>
        internal static bool GetNumericConversionOrder(TypeCode code, out int x, out int y) {
            // implicit conversions:
            //     0     1     2     3     4
            // 0:       U1 -> U2 -> U4 -> U8
            //          |     |     |
            //          v     v     v
            // 1: I1 -> I2 -> I4 -> I8
            //          |     |     
            //          v     v     
            // 2:       R4 -> R8

            switch (code) {
                case TypeCode.Byte: x = 0; y = 0; break;
                case TypeCode.UInt16: x = 1; y = 0; break;
                case TypeCode.UInt32: x = 2; y = 0; break;
                case TypeCode.UInt64: x = 3; y = 0; break;

                case TypeCode.SByte: x = 0; y = 1; break;
                case TypeCode.Int16: x = 1; y = 1; break;
                case TypeCode.Int32: x = 2; y = 1; break;
                case TypeCode.Int64: x = 3; y = 1; break;

                case TypeCode.Single: x = 1; y = 2; break;
                case TypeCode.Double: x = 2; y = 2; break;

                default:
                    x = y = 0;
                    return false;
            }
            return true;
        }

        internal static bool IsImplicitlyConvertible(int fromX, int fromY, int toX, int toY) {
            return fromX <= toX && fromY <= toY;
        }

        internal static bool HasBuiltinEquality(Type left, Type right) {
            // Reference type can be compared to interfaces
            if (left.IsInterface() && !right.IsValueType() ||
                right.IsInterface() && !left.IsValueType()) {
                return true;
            }

            // Reference types compare if they are assignable
            if (!left.IsValueType() && !right.IsValueType()) {
                if (CanAssign(left, right) || CanAssign(right, left)) {
                    return true;
                }
            }

            // Nullable<T> vs null
            if (NullVsNullable(left, right) || NullVsNullable(right, left)) {
                return true;
            }

            if (left != right) {
                return false;
            }

            if (left == typeof(bool) || IsNumeric(left) || left.IsEnum()) {
                return true;
            }

            return false;
        }

        private static bool NullVsNullable(Type left, Type right) {
            return IsNullableType(left) && right == typeof(DynamicNull);
        }

        // keep in sync with System.Core version
        internal static bool AreEquivalent(Type t1, Type t2) {
#if FEATURE_TYPE_EQUIVALENCE
            return t1 == t2 || t1.IsEquivalentTo(t2);
#else
            return t1 == t2;
#endif
        }

        // keep in sync with System.Core version
        internal static bool AreReferenceAssignable(Type dest, Type src) {
            // WARNING: This actually implements "Is this identity assignable and/or reference assignable?"
            if (dest == src) {
                return true;
            }
            if (!dest.IsValueType() && !src.IsValueType() && AreAssignable(dest, src)) {
                return true;
            }
            return false;
        }

        // keep in sync with System.Core version
        internal static bool AreAssignable(Type dest, Type src) {
            if (dest == src) {
                return true;
            }
            if (dest.IsAssignableFrom(src)) {
                return true;
            }
            if (dest.IsArray && src.IsArray && dest.GetArrayRank() == src.GetArrayRank() && AreReferenceAssignable(dest.GetElementType(), src.GetElementType())) {
                return true;
            }
            if (src.IsArray && dest.IsGenericType() &&
                (dest.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>)
                || dest.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IList<>)
                || dest.GetGenericTypeDefinition() == typeof(System.Collections.Generic.ICollection<>))
                && dest.GetGenericArguments()[0] == src.GetElementType()) {
                return true;
            }
            return false;
        }

        // keep in sync with System.Core version
        internal static Type GetConstantType(Type type) {
            // If it's a visible type, we're done
            if (type.IsVisible()) {
                return type;
            }

            // Get the visible base type
            Type bt = type;
            do {
                bt = bt.GetBaseType();
            } while (!bt.IsVisible());

            // If it's one of the known reflection types,
            // return the known type.
            if (bt == typeof(Type) ||
                bt == typeof(ConstructorInfo) ||
                bt == typeof(EventInfo) ||
                bt == typeof(FieldInfo) ||
                bt == typeof(MethodInfo) ||
                bt == typeof(PropertyInfo)) {
                return bt;
            }

            // else return the original type
            return type;
        }

        internal static bool IsConvertible(Type type) {
            type = GetNonNullableType(type);
            if (type.IsEnum()) {
                return true;
            }
            switch (type.GetTypeCode()) {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsFloatingPoint(Type type) {
            type = GetNonNullableType(type);
            switch (type.GetTypeCode()) {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

#if FEATURE_COM
        public static readonly Type ComObjectType = typeof(object).Assembly.GetType("System.__ComObject");

        public static bool IsComObjectType(Type/*!*/ type) {
            return ComObjectType.IsAssignableFrom(type);
        }

        // we can't use System.Runtime.InteropServices.Marshal.IsComObject(obj) since it doesn't work in partial trust
        public static bool IsComObject(object obj) {
            return obj != null && IsComObjectType(obj.GetType());
        }
#else
        public static bool IsComObjectType(Type/*!*/ type) {
            return false;
        }

        public static bool IsComObject(object obj) {
            return false;
        }
#endif
    }
}
