// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Interpreter {
    public sealed class NewArrayInitInstruction<TElement> : Instruction {
        private readonly int _elementCount;

        internal NewArrayInitInstruction(int elementCount) {
            _elementCount = elementCount;
        }

        public override int ConsumedStack => _elementCount;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            TElement[] array = new TElement[_elementCount];
            for (int i = _elementCount - 1; i >= 0; i--) {
                array[i] = (TElement)frame.Pop();
            }
            frame.Push(array);
            return +1;
        }
    }

    public sealed class NewArrayInstruction<TElement> : Instruction {
        internal NewArrayInstruction() { }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            int length = (int)frame.Pop();
            frame.Push(new TElement[length]);
            return +1;
        }
    }

    public sealed class NewArrayBoundsInstruction : Instruction {
        private readonly Type _elementType;
        private readonly int _rank;

        internal NewArrayBoundsInstruction(Type elementType, int rank) {
            _elementType = elementType;
            _rank = rank;
        }

        public override int ConsumedStack => _rank;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            var lengths = new int[_rank];
            for (int i = _rank - 1; i >= 0; i--) {
                lengths[i] = (int)frame.Pop();
            }
            var array = Array.CreateInstance(_elementType, lengths);
            frame.Push(array);
            return +1;
        }
    }

    public sealed class GetArrayItemInstruction<TElement> : Instruction {
        internal GetArrayItemInstruction() { }

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            int index = (int)frame.Pop();
            TElement[] array = (TElement[])frame.Pop();
            frame.Push(array[index]);
            return +1;
        }

        public override string InstructionName => "GetArrayItem";
    }

    public sealed class SetArrayItemInstruction<TElement> : Instruction {
        internal SetArrayItemInstruction() { }

        public override int ConsumedStack => 3;
        public override int ProducedStack => 0;

        public override int Run(InterpretedFrame frame) {
            TElement value = (TElement)frame.Pop();
            int index = (int)frame.Pop();
            TElement[] array = (TElement[])frame.Pop();
            array[index] = value;
            return +1;
        }

        public override string InstructionName => "SetArrayItem";
    }
}
