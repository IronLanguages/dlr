// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {

    internal struct TypeName : IEquatable<TypeName> {
        internal TypeName(Type type) {
            Debug.Assert(!type.IsNested());
            Namespace = type.Namespace;
            Name = type.Name;
        }

        internal TypeName(string nameSpace, string typeName) {
            Namespace = nameSpace;
            Name = typeName;
        }

        internal string Namespace { get; }
        internal string Name { get; }

        public override int GetHashCode() {
            int hash = 13 << 20;
            if (Namespace != null) hash ^= Namespace.GetHashCode();
            if (Name != null) hash ^= Name.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj) {
            if (obj is TypeName tn) {
                return tn.Namespace == Namespace && tn.Name == Name;
            }
            return false;
        }

        public bool Equals(TypeName other) =>
            Namespace == other.Namespace && Name == other.Name;

        public static bool operator ==(TypeName a, TypeName b) => a.Equals(b);

        public static bool operator !=(TypeName a, TypeName b) => !a.Equals(b);
    }

    internal static class AssemblyTypeNames {
        public static IEnumerable<TypeName> GetTypeNames(Assembly assem, bool includePrivateTypes) {
            return from t in ReflectionUtils.GetAllTypesFromAssembly(assem, includePrivateTypes)
                   where !t.IsNested
                   select new TypeName(t);
        }

        static IEnumerable<TypeName> GetTypeNames(string[] namespaces, string[][] types) {
            Debug.Assert(namespaces.Length == types.Length);

            for (int i = 0; i < namespaces.Length; i++) {
                for (int j = 0; j < types[i].Length; j++) {
                    TypeName typeName = new TypeName(namespaces[i], types[i][j]);
                    yield return typeName;
                }
            }
        }
    }
}
