// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Implements explicit casts supported by the runtime.
    /// </summary>
    public static partial class Cast {

        /// <summary>
        /// Explicitly casts the object to a given type (and returns it as object)
        /// </summary>
        public static object Explicit(object o, Type to) {
            if (o == null) {
                // Null objects can be only cast to Nullable<T> or any reference type
                if (to.IsValueType()) {
                    if (to.IsGenericType() && to.GetGenericTypeDefinition() == NullableType) {
                        return NewNullableInstance(to.GetGenericArguments()[0]);
                    }

                    if (to == typeof(void)) {
                        return null;
                    }

                    throw new InvalidCastException($"Cannot cast null to a value type {to.Name}");
                    
                }

                // Explicit cast to reference type is simply null
                return null;
            }

            if (to.IsValueType()) {
                return ExplicitCastToValueType(o, to);
            } 
            
            Type type = o.GetType();
            if (to.IsAssignableFrom(type)) {
                return o;
            }
 
            throw new InvalidCastException($"Cannot cast {type.Name} to {to.Name}");
        }

        public static T Explicit<T>(object o) {
            return (T)Explicit(o, typeof(T));
        }

        private static object ExplicitCastToValueType(object o, Type to) {
            Debug.Assert(o != null);
            Debug.Assert(to.IsValueType());

            if (to == Int32Type) return ScriptingRuntimeHelpers.Int32ToObject(ExplicitCastToInt32(o));
            if (to == DoubleType) return ExplicitCastToDouble(o);
            if (to == BooleanType) return ScriptingRuntimeHelpers.BooleanToObject(ExplicitCastToBoolean(o));
            if (to == ByteType) return ExplicitCastToByte(o);
            if (to == CharType) return ExplicitCastToChar(o);
            if (to == DecimalType) return ExplicitCastToDecimal(o);
            if (to == Int16Type) return ExplicitCastToInt16(o);
            if (to == Int64Type) return ExplicitCastToInt64(o);
            if (to == SByteType) return ExplicitCastToSByte(o);
            if (to == SingleType) return ExplicitCastToSingle(o);
            if (to == UInt16Type) return ExplicitCastToUInt16(o);
            if (to == UInt32Type) return ExplicitCastToUInt32(o);
            if (to == UInt64Type) return ExplicitCastToUInt64(o);

            if (to == NullableBooleanType) return ExplicitCastToNullableBoolean(o);
            if (to == NullableByteType) return ExplicitCastToNullableByte(o);
            if (to == NullableCharType) return ExplicitCastToNullableChar(o);
            if (to == NullableDecimalType) return ExplicitCastToNullableDecimal(o);
            if (to == NullableDoubleType) return ExplicitCastToNullableDouble(o);
            if (to == NullableInt16Type) return ExplicitCastToNullableInt16(o);
            if (to == NullableInt32Type) return ExplicitCastToNullableInt32(o);
            if (to == NullableInt64Type) return ExplicitCastToNullableInt64(o);
            if (to == NullableSByteType) return ExplicitCastToNullableSByte(o);
            if (to == NullableSingleType) return ExplicitCastToNullableSingle(o);
            if (to == NullableUInt16Type) return ExplicitCastToNullableUInt16(o);
            if (to == NullableUInt32Type) return ExplicitCastToNullableUInt32(o);
            if (to == NullableUInt64Type) return ExplicitCastToNullableUInt64(o);

            if (to.IsInstanceOfType(o)) {
                return o;
            }

            throw new InvalidCastException();
        }

        private static object NewNullableInstanceSlow(Type type) {
            Type concrete = NullableType.MakeGenericType(type);
            return Activator.CreateInstance(concrete);
        }

        private static InvalidCastException InvalidCast(object o, string typeName) {
            return new InvalidCastException($"Cannot cast {o?.GetType().Name ?? "(null)"} to {typeName}");
        }
    }
}
