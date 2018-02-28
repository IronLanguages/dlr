// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting.Debugging {
    using Ast = MSAst.Expression;

    internal static class InvokeTargets {
        internal static Type GetGeneratorFactoryTarget(Type[] parameterTypes) {
            Type[] typeArgs = new Type[parameterTypes.Length + 2];
            typeArgs[0] = typeof(DebugFrame);
            parameterTypes.CopyTo(typeArgs, 1);
            typeArgs[parameterTypes.Length + 1] = typeof(IEnumerator);

            if (typeArgs.Length <= 16) {
                return Ast.GetFuncType(typeArgs);
            } else {
#if FEATURE_REFEMIT
                return DelegateHelpers.MakeNewCustomDelegateType(typeArgs);
#else
                throw new NotSupportedException("Signature not supported on this platform.");
#endif
            }
        }
    }
}
