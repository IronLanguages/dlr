// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_LCG

using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation {
    internal sealed class ReturnFixer {
        private readonly LocalBuilder _refSlot;
        private readonly int _argIndex;
        private readonly Type _argType;

        private ReturnFixer(LocalBuilder refSlot, int argIndex, Type argType) {
            Debug.Assert(refSlot.LocalType.IsGenericType() && refSlot.LocalType.GetGenericTypeDefinition() == typeof(StrongBox<>));
            _refSlot = refSlot;
            _argIndex = argIndex;
            _argType = argType;
        }

        internal static ReturnFixer EmitArgument(ILGen cg, int argIndex, Type argType) {
            cg.EmitLoadArg(argIndex);

            if (!argType.IsByRef) {
                cg.EmitBoxing(argType);
                return null;
            }

            Type elementType = argType.GetElementType();
            cg.EmitLoadValueIndirect(elementType);
            Type concreteType = typeof(StrongBox<>).MakeGenericType(elementType);
            cg.EmitNew(concreteType, new Type[] { elementType });

            LocalBuilder refSlot = cg.DeclareLocal(concreteType);
            cg.Emit(OpCodes.Dup);
            cg.Emit(OpCodes.Stloc, refSlot);
            return new ReturnFixer(refSlot, argIndex, argType);
        }

        internal void FixReturn(ILGen cg) {
            cg.EmitLoadArg(_argIndex);
            cg.Emit(OpCodes.Ldloc, _refSlot);
            cg.Emit(OpCodes.Ldfld, _refSlot.LocalType.GetDeclaredField("Value"));
            cg.EmitStoreValueIndirect(_argType.GetElementType());
        }
    }
}
#endif
