// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    internal sealed class LoadStaticFieldInstruction : Instruction {
        private readonly FieldInfo _field;

        public LoadStaticFieldInstruction(FieldInfo field) {
            Debug.Assert(field.IsStatic);
            _field = field;
        }

        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Push(_field.GetValue(null));
            return +1;
        }
    }

    internal sealed class LoadFieldInstruction : Instruction {
        private readonly FieldInfo _field;

        public LoadFieldInstruction(FieldInfo field) {
            Assert.NotNull(field);
            _field = field;
        }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Push(_field.GetValue(frame.Pop()));
            return +1;
        }
    }

    internal sealed class StoreFieldInstruction : Instruction {
        private readonly FieldInfo _field;

        public StoreFieldInstruction(FieldInfo field) {
            Assert.NotNull(field);
            _field = field;
        }

        public override int ConsumedStack => 2;
        public override int ProducedStack => 0;

        public override int Run(InterpretedFrame frame) {
            object value = frame.Pop();
            object self = frame.Pop();
            _field.SetValue(self, value);
            return +1;
        }
    }

    internal sealed class StoreStaticFieldInstruction : Instruction {
        private readonly FieldInfo _field;

        public StoreStaticFieldInstruction(FieldInfo field) {
            Assert.NotNull(field);
            _field = field;
        }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 0;

        public override int Run(InterpretedFrame frame) {
            object value = frame.Pop();
            _field.SetValue(null, value);
            return +1;
        }
    }
}