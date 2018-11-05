// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Text;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Used as the key for the LanguageContext.GetDelegate method caching system
    /// </summary>
    internal sealed class DelegateSignatureInfo {
        internal DelegateSignatureInfo(MethodInfo invoke) {
            Assert.NotNull(invoke);

            ParameterInfo[] parameters = invoke.GetParameters();
            Type[] parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                parameterTypes[i] = parameters[i].ParameterType;
            }

            ParameterTypes = parameterTypes;
            ReturnType = invoke.ReturnType;
        }

        internal Type ReturnType { get; }
        internal Type[] ParameterTypes { get; }

        public override bool Equals(object obj) {
            DelegateSignatureInfo dsi = obj as DelegateSignatureInfo;

            if (dsi == null ||
                dsi.ParameterTypes.Length != ParameterTypes.Length ||
                dsi.ReturnType != ReturnType) {
                return false;
            }

            for (int i = 0; i < ParameterTypes.Length; i++) {
                if (dsi.ParameterTypes[i] != ParameterTypes[i]) {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() {
            int hashCode = 5331;

            for (int i = 0; i < ParameterTypes.Length; i++) {
                hashCode ^= ParameterTypes[i].GetHashCode();
            }
            hashCode ^= ReturnType.GetHashCode();
            return hashCode;
        }

        public override string ToString() {
            StringBuilder text = new StringBuilder();
            text.Append(ReturnType);
            text.Append("(");
            for (int i = 0; i < ParameterTypes.Length; i++) {
                if (i != 0) text.Append(", ");
                text.Append(ParameterTypes[i].Name);
            }
            text.Append(")");
            return text.ToString();
        }
    }
}
