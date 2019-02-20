﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REFEMIT

using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Debugging {
    internal static class DelegateHelpers {
        private static ModuleBuilder _moduleBuilder;
        private const MethodAttributes CtorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
        private const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
        private const MethodAttributes InvokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;

        private static readonly Type[] _DelegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };

        internal static Type MakeNewCustomDelegateType(Type[] types) {
            Type returnType = types[types.Length - 1];
            Type[] parameters = types.RemoveLast();

            TypeBuilder builder = DefineDelegateType("Delegate_" + Guid.NewGuid().ToString());
            builder.DefineConstructor(CtorAttributes, CallingConventions.Standard, _DelegateCtorSignature).SetImplementationFlags(ImplAttributes);
            builder.DefineMethod("Invoke", InvokeAttributes, returnType, parameters).SetImplementationFlags(ImplAttributes);
            return builder.CreateTypeInfo();
        }

        private static TypeBuilder DefineDelegateType(string name) {
            return GetModule().DefineType(
                name,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass,
                typeof(MulticastDelegate)
            );
        }

        private static ModuleBuilder GetModule() {
            lock (_DelegateCtorSignature) {
                if (_moduleBuilder == null) {
                    AssemblyBuilder assemblyBuilder = ReflectionUtils.DefineDynamicAssembly(
                        new AssemblyName("Snippets.Microsoft.Scripting.Debugging"), AssemblyBuilderAccess.Run);
                    
                    _moduleBuilder = assemblyBuilder.DefineDynamicModule("Snippets.Microsoft.Scripting.Debugging", true);
                }
            }
            return _moduleBuilder;
        }
    }
}
#endif
