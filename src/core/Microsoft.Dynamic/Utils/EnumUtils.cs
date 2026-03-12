// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Utils {
    public static class EnumUtils {        
        public static object BitwiseOr(object self, object other) {
            if (self is Enum && other is Enum) {
                Type selfType = self.GetType();
                Type otherType = other.GetType();

                if (selfType == otherType) {
                    Type underType = Enum.GetUnderlyingType(selfType);
                    if (underType == typeof(int)) {
                        return Enum.ToObject(selfType, (int)self | (int)other);
                    } else if (underType == typeof(long)) {
                        return Enum.ToObject(selfType, (long)self | (long)other);
                    } else if (underType == typeof(short)) {
                        return Enum.ToObject(selfType, (short)self | (short)other);
                    } else if (underType == typeof(byte)) {
                        return Enum.ToObject(selfType, (byte)self | (byte)other);
                    } else if (underType == typeof(sbyte)) {
#pragma warning disable 675 // msc
                        return Enum.ToObject(selfType, (sbyte)self | (sbyte)other);
#pragma warning restore 675
                    } else if (underType == typeof(uint)) {
                        return Enum.ToObject(selfType, (uint)self | (uint)other);
                    } else if (underType == typeof(ulong)) {
                        return Enum.ToObject(selfType, (ulong)self | (ulong)other);
                    } else if (underType == typeof(ushort)) {
                        return Enum.ToObject(selfType, (ushort)self | (ushort)other);
                    } else {
                        throw Assert.Unreachable;
                    }
                }
            }

            return null;
        }

        public static object BitwiseAnd(object self, object other) {
            if (self is Enum && other is Enum) {
                Type selfType = self.GetType();
                Type otherType = other.GetType();

                if (selfType == otherType) {
                    Type underType = Enum.GetUnderlyingType(selfType);
                    if (underType == typeof(int)) {
                        return Enum.ToObject(selfType, (int)self & (int)other);
                    } else if (underType == typeof(long)) {
                        return Enum.ToObject(selfType, (long)self & (long)other);
                    } else if (underType == typeof(short)) {
                        return Enum.ToObject(selfType, (short)self & (short)other);
                    } else if (underType == typeof(byte)) {
                        return Enum.ToObject(selfType, (byte)self & (byte)other);
                    } else if (underType == typeof(sbyte)) {
                        return Enum.ToObject(selfType, (sbyte)self & (sbyte)other);
                    } else if (underType == typeof(uint)) {
                        return Enum.ToObject(selfType, (uint)self & (uint)other);
                    } else if (underType == typeof(ulong)) {
                        return Enum.ToObject(selfType, (ulong)self & (ulong)other);
                    } else if (underType == typeof(ushort)) {
                        return Enum.ToObject(selfType, (ushort)self & (ushort)other);
                    } else {
                        throw Assert.Unreachable;
                    }
                }
            }
            return null;
        }

        public static object ExclusiveOr(object self, object other) {
            if (self is Enum && other is Enum) {
                Type selfType = self.GetType();
                Type otherType = other.GetType();

                if (selfType == otherType) {
                    Type underType = Enum.GetUnderlyingType(selfType);
                    if (underType == typeof(int)) {
                        return Enum.ToObject(selfType, (int)self ^ (int)other);
                    } else if (underType == typeof(long)) {
                        return Enum.ToObject(selfType, (long)self ^ (long)other);
                    } else if (underType == typeof(short)) {
                        return Enum.ToObject(selfType, (short)self ^ (short)other);
                    } else if (underType == typeof(byte)) {
                        return Enum.ToObject(selfType, (byte)self ^ (byte)other);
                    } else if (underType == typeof(sbyte)) {
                        return Enum.ToObject(selfType, (sbyte)self ^ (sbyte)other);
                    } else if (underType == typeof(uint)) {
                        return Enum.ToObject(selfType, (uint)self ^ (uint)other);
                    } else if (underType == typeof(ulong)) {
                        return Enum.ToObject(selfType, (ulong)self ^ (ulong)other);
                    } else if (underType == typeof(ushort)) {
                        return Enum.ToObject(selfType, (ushort)self ^ (ushort)other);
                    } else {
                        throw Assert.Unreachable;
                    }
                }
            }

            return null;
        }

        public static object OnesComplement(object self) {
            if (self is Enum) {
                Type selfType = self.GetType();
                Type underType = Enum.GetUnderlyingType(selfType);
                if (underType == typeof(int)) {
                    return Enum.ToObject(selfType, ~(int)self);
                } else if (underType == typeof(long)) {
                    return Enum.ToObject(selfType, ~(long)self);
                } else if (underType == typeof(short)) {
                    return Enum.ToObject(selfType, ~(short)self);
                } else if (underType == typeof(byte)) {
                    return Enum.ToObject(selfType, ~(byte)self);
                } else if (underType == typeof(sbyte)) {
                    return Enum.ToObject(selfType, ~(sbyte)self);
                } else if (underType == typeof(uint)) {
                    return Enum.ToObject(selfType, ~(uint)self);
                } else if (underType == typeof(ulong)) {
                    return Enum.ToObject(selfType, ~(ulong)self);
                } else if (underType == typeof(ushort)) {
                    return Enum.ToObject(selfType, ~(ushort)self);
                } else {
                    throw Assert.Unreachable;
                }
            }

            return null;
        }
    }
}
