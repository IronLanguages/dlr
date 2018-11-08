// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.Scripting.Utils {
    [Serializable]
    internal readonly struct AssemblyQualifiedTypeName : IEquatable<AssemblyQualifiedTypeName> {
        public readonly string TypeName;
        public readonly AssemblyName AssemblyName;

        public AssemblyQualifiedTypeName(string typeName, AssemblyName assemblyName) {
            ContractUtils.RequiresNotNull(typeName, nameof(typeName));
            ContractUtils.RequiresNotNull(assemblyName, nameof(assemblyName));

            TypeName = typeName;
            AssemblyName = assemblyName;
        }

        public AssemblyQualifiedTypeName(Type type) {
            TypeName = type.FullName;
            AssemblyName = type.Assembly.GetName();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public AssemblyQualifiedTypeName(string assemblyQualifiedTypeName) {
            ContractUtils.RequiresNotNull(assemblyQualifiedTypeName, nameof(assemblyQualifiedTypeName));

            int firstColon = assemblyQualifiedTypeName.IndexOf(",");
            if (firstColon != -1) {
                TypeName = assemblyQualifiedTypeName.Substring(0, firstColon).Trim();
                var assemblyNameStr = assemblyQualifiedTypeName.Substring(firstColon + 1).Trim();
                if (TypeName.Length > 0 && assemblyNameStr.Length > 0) {
                    try {
                        AssemblyName = new AssemblyName(assemblyNameStr);
                        return;
                    } catch (Exception e) {
                        throw new ArgumentException(
                            $"Invalid assembly qualified name '{assemblyQualifiedTypeName}': {e.Message}", e);
                    }
                }
            }

            throw new ArgumentException($"Invalid assembly qualified name '{assemblyQualifiedTypeName}'");
        }

        internal static AssemblyQualifiedTypeName ParseArgument(string str, string argumentName) {
            Assert.NotEmpty(argumentName);           
            try {
                return new AssemblyQualifiedTypeName(str);
            } catch (ArgumentException e) {
                throw new ArgumentException(e.Message, argumentName, e.InnerException);
            }
        }

        public bool Equals(AssemblyQualifiedTypeName other) =>
            TypeName == other.TypeName && AssemblyName.FullName == other.AssemblyName.FullName;

        public override bool Equals(object obj) =>
            obj is AssemblyQualifiedTypeName name && Equals(name);

        public override int GetHashCode() {
            return TypeName.GetHashCode() ^ AssemblyName.FullName.GetHashCode();
        }

        public override string ToString() {
            return TypeName + ", " + AssemblyName.FullName;
        }

        public static bool operator ==(AssemblyQualifiedTypeName name, AssemblyQualifiedTypeName other) => name.Equals(other);

        public static bool operator !=(AssemblyQualifiedTypeName name, AssemblyQualifiedTypeName other) => !name.Equals(other);
    }
}
