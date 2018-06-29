// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter {
    internal sealed class CreateDelegateInstruction : Instruction {
        private readonly LightDelegateCreator _creator;

        internal CreateDelegateInstruction(LightDelegateCreator delegateCreator) {
            _creator = delegateCreator;
        }

        public override int ConsumedStack => _creator.Interpreter.ClosureSize;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            StrongBox<object>[] closure;
            if (ConsumedStack > 0) {
                closure = new StrongBox<object>[ConsumedStack];
                for (int i = closure.Length - 1; i >= 0; i--) {
                    closure[i] = (StrongBox<object>)frame.Pop();
                }
            } else {
                closure = null;
            }

            Delegate d = _creator.CreateDelegate(closure);

            frame.Push(d);
            return +1;
        }
    }

    internal sealed class NewInstruction : Instruction {
        private readonly ConstructorInfo _constructor;
        private readonly int _argCount;

        public NewInstruction(ConstructorInfo constructor) {
            _constructor = constructor;
            _argCount = constructor.GetParameters().Length;

        }
        public override int ConsumedStack => _argCount;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            object[] args = new object[_argCount];
            for (int i = _argCount - 1; i >= 0; i--) {
                args[i] = frame.Pop();
            }

            object ret;
            try {
                ret = _constructor.Invoke(args);
            } catch (TargetInvocationException e) {
                ExceptionHelpers.UpdateForRethrow(e.InnerException);
                throw e.InnerException;
            }

            frame.Push(ret);
            return +1;
        }

        public override string ToString() {
            return "New " + _constructor.DeclaringType.Name + "(" + _constructor + ")";
        }
    }

    internal sealed class DefaultValueInstruction<T> : Instruction {

        public override int ConsumedStack => 0;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            frame.Push(default(T));
            return +1;
        }

        public override string ToString() {
            return "New " + typeof(T);
        }
    }

    internal sealed class TypeIsInstruction<T> : Instruction {

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            // unfortunately Type.IsInstanceOfType() is 35-times slower than "is T" so we use generic code:
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(frame.Pop() is T));
            return +1;
        }

        public override string ToString() {
            return "TypeIs " + typeof(T).Name; 
        }
    }

    internal sealed class TypeAsInstruction<T> : Instruction {

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame) {
            // can't use as w/o generic constraint
            object value = frame.Pop();
            if (value is T) {
                frame.Push(value);
            } else {
                frame.Push(null);
            }
            return +1;
        }

        public override string ToString() {
            return "TypeAs " + typeof(T).Name;
        }
    }

    internal sealed class TypeEqualsInstruction : Instruction {
        public static readonly TypeEqualsInstruction Instance = new TypeEqualsInstruction();

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;

        private TypeEqualsInstruction() {
        }

        public override int Run(InterpretedFrame frame) {
            object type = frame.Pop();
            object obj = frame.Pop();
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null && (object)obj.GetType() == type));
            return +1;
        }

        public override string InstructionName => "TypeEquals()";
    }
}
