// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    internal abstract class DivInstruction : Instruction {
        private static Instruction _Int16, _Int32, _Int64, _UInt16, _UInt32, _UInt64, _Single, _Double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;

        private DivInstruction() {
        }

        private sealed class DivInt16 : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((short)((short)left / (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class DivInt32 : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] =
                        right == null ? null : ScriptingRuntimeHelpers.Int32ToObject((int)left / (int)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class DivInt64 : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((long)left / (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class DivUInt16 : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ushort)((ushort)left / (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class DivUInt32 : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((uint)left / (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class DivUInt64 : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((ulong)left / (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class DivSingle : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((float)left / (float)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class DivDouble : DivInstruction {
            public override int Run(InterpretedFrame frame) {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null) {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((double)left / (double)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type) {
            Debug.Assert(!type.IsEnum);
            switch (type.GetNonNullableType().GetTypeCode()) {
                case TypeCode.Int16: return _Int16 ?? (_Int16 = new DivInt16());
                case TypeCode.Int32: return _Int32 ?? (_Int32 = new DivInt32());
                case TypeCode.Int64: return _Int64 ?? (_Int64 = new DivInt64());
                case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new DivUInt16());
                case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new DivUInt32());
                case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new DivUInt64());
                case TypeCode.Single: return _Single ?? (_Single = new DivSingle());
                case TypeCode.Double: return _Double ?? (_Double = new DivDouble());
                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString() {
            return "Div()";
        }
    }
}
