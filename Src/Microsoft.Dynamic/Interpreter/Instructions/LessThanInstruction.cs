// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    internal abstract class LessThanInstruction : Instruction {
        public static readonly object BoxedFalse = false;
        public static readonly object BoxedTrue = true;
        private readonly object _nullValue;
        private static Instruction _SByte, _Int16, _Char, _Int32, _Int64, _Byte, _UInt16, _UInt32, _UInt64, _Single, _Double;
        private static Instruction _liftedToNullSByte, _liftedToNullInt16, _liftedToNullChar, _liftedToNullInt32, _liftedToNullInt64, _liftedToNullByte, _liftedToNullUInt16, _liftedToNullUInt32, _liftedToNullUInt64, _liftedToNullSingle, _liftedToNullDouble;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;

        private LessThanInstruction(object nullValue) {
            _nullValue = nullValue;
        }

        private sealed class LessThanSByte : LessThanInstruction {
            public LessThanSByte(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((sbyte)left < (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class LessThanInt16 : LessThanInstruction {
            public LessThanInt16(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((short)left < (short)right);
                }
                return 1;
            }
        }

        private sealed class LessThanChar : LessThanInstruction {
            public LessThanChar(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((char)left < (char)right);
                }
                return 1;
            }
        }

        private sealed class LessThanInt32 : LessThanInstruction {
            public LessThanInt32(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((int)left < (int)right);
                }
                return 1;
            }
        }

        private sealed class LessThanInt64 : LessThanInstruction {
            public LessThanInt64(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((long)left < (long)right);
                }
                return 1;
            }
        }

        private sealed class LessThanByte : LessThanInstruction {
            public LessThanByte(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((byte)left < (byte)right);
                }
                return 1;
            }
        }

        private sealed class LessThanUInt16 : LessThanInstruction {
            public LessThanUInt16(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((ushort)left < (ushort)right);
                }
                return 1;
            }
        }

        private sealed class LessThanUInt32 : LessThanInstruction {
            public LessThanUInt32(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((uint)left < (uint)right);
                }
                return 1;
            }
        }

        private sealed class LessThanUInt64 : LessThanInstruction {
            public LessThanUInt64(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((ulong)left < (ulong)right);
                }
                return 1;
            }
        }

        private sealed class LessThanSingle : LessThanInstruction {
            public LessThanSingle(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((float)left < (float)right);
                }
                return 1;
            }
        }

        private sealed class LessThanDouble : LessThanInstruction {
            public LessThanDouble(object nullValue)
                : base(nullValue) {
            }

            public override int Run(InterpretedFrame frame) {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null) {
                    frame.Push(_nullValue);
                } else {
                    frame.Push((double)left < (double)right);
                }
                return 1;
            }
        }
        public static Instruction Create(Type type, bool liftedToNull = false) {
            Debug.Assert(!type.IsEnum);
            if (liftedToNull) {
                switch (type.GetNonNullableType().GetTypeCode()) {
                    case TypeCode.SByte: return _liftedToNullSByte ?? (_liftedToNullSByte = new LessThanSByte(null));
                    case TypeCode.Int16: return _liftedToNullInt16 ?? (_liftedToNullInt16 = new LessThanInt16(null));
                    case TypeCode.Char: return _liftedToNullChar ?? (_liftedToNullChar = new LessThanChar(null));
                    case TypeCode.Int32: return _liftedToNullInt32 ?? (_liftedToNullInt32 = new LessThanInt32(null));
                    case TypeCode.Int64: return _liftedToNullInt64 ?? (_liftedToNullInt64 = new LessThanInt64(null));
                    case TypeCode.Byte: return _liftedToNullByte ?? (_liftedToNullByte = new LessThanByte(null));
                    case TypeCode.UInt16: return _liftedToNullUInt16 ?? (_liftedToNullUInt16 = new LessThanUInt16(null));
                    case TypeCode.UInt32: return _liftedToNullUInt32 ?? (_liftedToNullUInt32 = new LessThanUInt32(null));
                    case TypeCode.UInt64: return _liftedToNullUInt64 ?? (_liftedToNullUInt64 = new LessThanUInt64(null));
                    case TypeCode.Single: return _liftedToNullSingle ?? (_liftedToNullSingle = new LessThanSingle(null));
                    case TypeCode.Double: return _liftedToNullDouble ?? (_liftedToNullDouble = new LessThanDouble(null));
                    default:
                        throw Assert.Unreachable;
                }
            }

            switch (type.GetNonNullableType().GetTypeCode()) {
                case TypeCode.SByte: return _SByte ?? (_SByte = new LessThanSByte(BoxedFalse));
                case TypeCode.Int16: return _Int16 ?? (_Int16 = new LessThanInt16(BoxedFalse));
                case TypeCode.Char: return _Char ?? (_Char = new LessThanChar(BoxedFalse));
                case TypeCode.Int32: return _Int32 ?? (_Int32 = new LessThanInt32(BoxedFalse));
                case TypeCode.Int64: return _Int64 ?? (_Int64 = new LessThanInt64(BoxedFalse));
                case TypeCode.Byte: return _Byte ?? (_Byte = new LessThanByte(BoxedFalse));
                case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new LessThanUInt16(BoxedFalse));
                case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new LessThanUInt32(BoxedFalse));
                case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new LessThanUInt64(BoxedFalse));
                case TypeCode.Single: return _Single ?? (_Single = new LessThanSingle(BoxedFalse));
                case TypeCode.Double: return _Double ?? (_Double = new LessThanDouble(BoxedFalse));
                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString() {
            return "LessThan()";
        }
    }
}
