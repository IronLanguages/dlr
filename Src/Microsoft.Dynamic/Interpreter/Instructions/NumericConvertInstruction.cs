// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    internal abstract class NumericConvertInstruction : Instruction {
        internal readonly TypeCode _from, _to;

        protected NumericConvertInstruction(TypeCode from, TypeCode to) {
            _from = from;
            _to = to;
        }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override string ToString() {
            return InstructionName + "(" + _from + "->" + _to + ")";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Unchecked : NumericConvertInstruction {
            public override string InstructionName => "UncheckedConvert";

            public Unchecked(TypeCode from, TypeCode to)
                : base(from, to) {
            }

            public override int Run(InterpretedFrame frame) {
                frame.Push(Convert(frame.Pop()));
                return +1;
            }

            private object Convert(object obj) {
                switch (_from) {
                    case TypeCode.Byte: return ConvertInt32((Byte)obj);
                    case TypeCode.SByte: return ConvertInt32((SByte)obj);
                    case TypeCode.Int16: return ConvertInt32((Int16)obj);
                    case TypeCode.Char: return ConvertInt32((Char)obj);
                    case TypeCode.Int32: return ConvertInt32((Int32)obj);
                    case TypeCode.Int64: return ConvertInt64((Int64)obj);
                    case TypeCode.UInt16: return ConvertInt32((UInt16)obj);
                    case TypeCode.UInt32: return ConvertInt64((UInt32)obj);
                    case TypeCode.UInt64: return ConvertUInt64((UInt64)obj);
                    case TypeCode.Single: return ConvertDouble((Single)obj);
                    case TypeCode.Double: return ConvertDouble((Double)obj);
                    default: throw Assert.Unreachable;
                }
            }

            private object ConvertInt32(int obj) {
                unchecked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertInt64(Int64 obj) {
                unchecked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(UInt64 obj) {
                unchecked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertDouble(Double obj) {
                unchecked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Checked : NumericConvertInstruction {
            public override string InstructionName => "CheckedConvert";

            public Checked(TypeCode from, TypeCode to)
                : base(from, to) {
            }

            public override int Run(InterpretedFrame frame) {
                frame.Push(Convert(frame.Pop()));
                return +1;
            }

            private object Convert(object obj) {
                switch (_from) {
                    case TypeCode.Byte: return ConvertInt32((Byte)obj);
                    case TypeCode.SByte: return ConvertInt32((SByte)obj);
                    case TypeCode.Int16: return ConvertInt32((Int16)obj);
                    case TypeCode.Char: return ConvertInt32((Char)obj);
                    case TypeCode.Int32: return ConvertInt32((Int32)obj);
                    case TypeCode.Int64: return ConvertInt64((Int64)obj);
                    case TypeCode.UInt16: return ConvertInt32((UInt16)obj);
                    case TypeCode.UInt32: return ConvertInt64((UInt32)obj);
                    case TypeCode.UInt64: return ConvertUInt64((UInt64)obj);
                    case TypeCode.Single: return ConvertDouble((Single)obj);
                    case TypeCode.Double: return ConvertDouble((Double)obj);
                    default: throw Assert.Unreachable;
                }
            }

            private object ConvertInt32(int obj) {
                checked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertInt64(Int64 obj) {
                checked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(UInt64 obj) {
                checked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertDouble(Double obj) {
                checked {
                    switch (_to) {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }
        }
    }
}