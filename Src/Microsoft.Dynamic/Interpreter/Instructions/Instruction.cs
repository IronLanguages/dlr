// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter {
    public interface IInstructionProvider {
        void AddInstructions(LightCompiler compiler);
    }

    public abstract partial class Instruction {
        public virtual int ConsumedStack => 0;
        public virtual int ProducedStack => 0;
        public virtual int ConsumedContinuations => 0;
        public virtual int ProducedContinuations => 0;

        public int StackBalance => ProducedStack - ConsumedStack;

        public int ContinuationsBalance => ProducedContinuations - ConsumedContinuations;

        public abstract int Run(InterpretedFrame frame);

        public virtual string InstructionName => GetType().Name.Replace("Instruction", string.Empty);

        public override string ToString() {
            return InstructionName + "()";
        }

        public virtual string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) {
            return ToString();
        }

        public virtual object GetDebugCookie(LightCompiler compiler) {
            return null;
        }
    }

    internal sealed class NotInstruction : Instruction {
        public static readonly Instruction Instance = new NotInstruction();

        private NotInstruction() { }
        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Push((bool)frame.Pop() ? ScriptingRuntimeHelpers.False : ScriptingRuntimeHelpers.True);
            return +1;
        }
    }
}
