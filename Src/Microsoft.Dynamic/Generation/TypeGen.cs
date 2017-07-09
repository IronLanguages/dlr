/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/
#if FEATURE_REFEMIT

using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation {
    public sealed class TypeGen {
        private ILGen _initGen;                        // The IL generator for the .cctor()

        /// <summary>
        /// Gets the Compiler associated with the Type Initializer (cctor) creating it if necessary.
        /// </summary>
        public ILGen TypeInitializer {
            get {
                if (_initGen == null) {
                    _initGen = new ILGen(TypeBuilder.DefineTypeInitializer().GetILGenerator());
                }
                return _initGen;
            }
        }

        internal AssemblyGen AssemblyGen { get; }

        public TypeBuilder TypeBuilder { get; }

        public TypeGen(AssemblyGen myAssembly, TypeBuilder myType) {
            Assert.NotNull(myAssembly, myType);

            AssemblyGen = myAssembly;
            TypeBuilder = myType;
        }

        public override string ToString() {
            return TypeBuilder.ToString();
        }

        public Type FinishType() {
            _initGen?.Emit(OpCodes.Ret);
            Type ret = TypeBuilder.CreateType();
            return ret;
        }

        public FieldBuilder AddStaticField(Type fieldType, string name) {
            return TypeBuilder.DefineField(name, fieldType, FieldAttributes.Public | FieldAttributes.Static);
        }

        public FieldBuilder AddStaticField(Type fieldType, FieldAttributes attributes, string name) {
            return TypeBuilder.DefineField(name, fieldType, attributes | FieldAttributes.Static);
        }

        public ILGen DefineExplicitInterfaceImplementation(MethodInfo baseMethod) {
            ContractUtils.RequiresNotNull(baseMethod, "baseMethod");

            MethodAttributes attrs = baseMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.Public);
            attrs |= MethodAttributes.NewSlot | MethodAttributes.Final;

            Type[] baseSignature = baseMethod.GetParameters().Map(p => p.ParameterType);
            MethodBuilder mb = TypeBuilder.DefineMethod(
                baseMethod.DeclaringType.Name + "." + baseMethod.Name,
                attrs,
                baseMethod.ReturnType,
                baseSignature);

            TypeBuilder.DefineMethodOverride(mb, baseMethod);
            return new ILGen(mb.GetILGenerator());
        }

        private const MethodAttributes MethodAttributesToEraseInOveride = MethodAttributes.Abstract | MethodAttributes.ReservedMask;

        // TODO: Use ReflectionUtils.DefineMethodOverride?
        public ILGen DefineMethodOverride(MethodInfo baseMethod) {
            MethodAttributes finalAttrs = baseMethod.Attributes & ~MethodAttributesToEraseInOveride;
            Type[] baseSignature = baseMethod.GetParameters().Map(p => p.ParameterType);
            MethodBuilder mb = TypeBuilder.DefineMethod(baseMethod.Name, finalAttrs, baseMethod.ReturnType, baseSignature);

            TypeBuilder.DefineMethodOverride(mb, baseMethod);
            return new ILGen(mb.GetILGenerator());
        }
    }
}
#endif