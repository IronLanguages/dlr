// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Scripting.Interpreter {
    internal sealed class LoadObjectInstruction : Instruction {
        private readonly object _value;

        internal LoadObjectInstruction(object value) {
            _value = value;
        }

        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Data[frame.StackIndex++] = _value;
            return +1;
        }

        public override string ToString() {
            return "LoadObject(" + (_value ?? "null") + ")";
        }
    }

    internal sealed class LoadCachedObjectInstruction : Instruction {
        private readonly uint _index;

        internal LoadCachedObjectInstruction(uint index) {
            _index = index;
        }

        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Data[frame.StackIndex++] = frame.Interpreter._objects[_index];
            return +1;
        }

        public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) {
            return $"LoadCached({_index}: {objects[(int)_index]})";
        }
        
        public override string ToString() {
            return "LoadCached(" + _index + ")";
        }
    }

    internal sealed class PopInstruction : Instruction {
        internal static readonly PopInstruction Instance = new PopInstruction();

        private PopInstruction() { }

        public override int ConsumedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Pop();
            return +1;
        }

        public override string ToString() {
            return "Pop()";
        }
    }

    // NOTE: Consider caching if used frequently
    internal sealed class PopNInstruction : Instruction {
        private readonly int _n;

        internal PopNInstruction(int n) {
            _n = n;
        }

        public override int ConsumedStack => _n;

        public override int Run(InterpretedFrame frame) {
            frame.Pop(_n);
            return +1;
        }

        public override string ToString() {
            return "Pop(" + _n + ")";
        }
    }

    internal sealed class DupInstruction : Instruction {
        internal static readonly DupInstruction Instance = new DupInstruction();

        private DupInstruction() { }

        public override int ConsumedStack => 0;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Data[frame.StackIndex++] = frame.Peek();
            return +1;
        }

        public override string ToString() {
            return "Dup()";
        }
    }
}
