// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Numerics;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    public abstract class InstructionFactory {
        // TODO: weak table for types in a collectible assembly?
        private static Dictionary<Type, InstructionFactory> _factories;

        internal static InstructionFactory GetFactory(Type type) {
            if (_factories == null) {
                _factories = new Dictionary<Type, InstructionFactory>() {
                    { typeof(object), InstructionFactory<object>.Factory },
                    { typeof(bool), InstructionFactory<bool>.Factory },
                    { typeof(byte), InstructionFactory<byte>.Factory },
                    { typeof(sbyte), InstructionFactory<sbyte>.Factory },
                    { typeof(short), InstructionFactory<short>.Factory },
                    { typeof(ushort), InstructionFactory<ushort>.Factory },
                    { typeof(int), InstructionFactory<int>.Factory },
                    { typeof(uint), InstructionFactory<uint>.Factory },
                    { typeof(long), InstructionFactory<long>.Factory },
                    { typeof(ulong), InstructionFactory<ulong>.Factory },
                    { typeof(float), InstructionFactory<float>.Factory },
                    { typeof(double), InstructionFactory<double>.Factory },
                    { typeof(char), InstructionFactory<char>.Factory },
                    { typeof(string), InstructionFactory<string>.Factory },
                    { typeof(BigInteger), InstructionFactory<BigInteger>.Factory }
                };
            }

            lock (_factories) {
                if (!_factories.TryGetValue(type, out InstructionFactory factory)) {
                    factory = (InstructionFactory)typeof(InstructionFactory<>).MakeGenericType(type).GetDeclaredField("Factory").GetValue(null);
                    _factories[type] = factory;
                }
                return factory;
            }
        }

        protected internal abstract Instruction GetArrayItem();
        protected internal abstract Instruction SetArrayItem();
        protected internal abstract Instruction TypeIs();
        protected internal abstract Instruction TypeAs();
        protected internal abstract Instruction DefaultValue();
        protected internal abstract Instruction NewArray();
        protected internal abstract Instruction NewArrayInit(int elementCount);
    }

    public sealed class InstructionFactory<T> : InstructionFactory {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly InstructionFactory Factory = new InstructionFactory<T>();

        private Instruction _getArrayItem;
        private Instruction _setArrayItem;
        private Instruction _typeIs;
        private Instruction _defaultValue;
        private Instruction _newArray;
        private Instruction _typeAs;

        private InstructionFactory() { }

        protected internal override Instruction GetArrayItem() {
            return _getArrayItem ?? (_getArrayItem = new GetArrayItemInstruction<T>());
        }

        protected internal override Instruction SetArrayItem() {
            return _setArrayItem ?? (_setArrayItem = new SetArrayItemInstruction<T>());
        }

        protected internal override Instruction TypeIs() {
            return _typeIs ?? (_typeIs = new TypeIsInstruction<T>());
        }

        protected internal override Instruction TypeAs() {
            return _typeAs ?? (_typeAs = new TypeAsInstruction<T>());
        }

        protected internal override Instruction DefaultValue() {
            return _defaultValue ?? (_defaultValue = new DefaultValueInstruction<T>());
        }

        protected internal override Instruction NewArray() {
            return _newArray ?? (_newArray = new NewArrayInstruction<T>());
        }

        protected internal override Instruction NewArrayInit(int elementCount) {
            return new NewArrayInitInstruction<T>(elementCount);
        }
    }
}
