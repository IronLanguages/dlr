// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    internal abstract class AddInstruction : Instruction {
        private static Instruction _Int16, _Int32, _Int64, _UInt16, _UInt32, _UInt64, _Single, _Double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;

        private AddInstruction() {
        }

        private sealed class AddInt16 : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((short)((short)left + (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddInt32 : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null
                        ? null
                        : ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)left + (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddInt64 : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((long)left + (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddUInt16 : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ushort)((ushort)left + (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddUInt32 : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((uint)left + (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddUInt64 : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ulong)left + (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddSingle : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((float)left + (float)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddDouble : AddInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((double)left + (double)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type) {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode()) {
                case TypeCode.Int16: return _Int16 ?? (_Int16 = new AddInt16());
                case TypeCode.Int32: return _Int32 ?? (_Int32 = new AddInt32());
                case TypeCode.Int64: return _Int64 ?? (_Int64 = new AddInt64());
                case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new AddUInt16());
                case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new AddUInt32());
                case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new AddUInt64());
                case TypeCode.Single: return _Single ?? (_Single = new AddSingle());
                case TypeCode.Double: return _Double ?? (_Double = new AddDouble());
                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString() {
            return "Add()";
        }
    }

    internal abstract class AddOvfInstruction : Instruction {
        private static Instruction _Int16, _Int32, _Int64, _UInt16, _UInt32, _UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;

        private AddOvfInstruction() {
        }

        private sealed class AddOvfInt16 : AddOvfInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((short)((short)left + (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfInt32 : AddOvfInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null
                        ? null
                        : ScriptingRuntimeHelpers.Int32ToObject(checked((int)left + (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfInt64 : AddOvfInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((long)left + (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfUInt16 : AddOvfInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ushort)((ushort)left + (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfUInt32 : AddOvfInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((uint)left + (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfUInt64 : AddOvfInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ulong)left + (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type) {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode()) {
                case TypeCode.Int16: return _Int16 ?? (_Int16 = new AddOvfInt16());
                case TypeCode.Int32: return _Int32 ?? (_Int32 = new AddOvfInt32());
                case TypeCode.Int64: return _Int64 ?? (_Int64 = new AddOvfInt64());
                case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new AddOvfUInt16());
                case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new AddOvfUInt32());
                case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new AddOvfUInt64());
                default:
                    return AddInstruction.Create(type);
            }
        }

        public override string ToString() {
            return "AddOvf()";
        }
    }
}
