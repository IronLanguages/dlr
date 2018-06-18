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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {

    internal struct TypeName {
        internal TypeName(Type type) {
            Debug.Assert(!type.IsNested());
            Namespace = type.Namespace;
            Name = type.Name;
        }

        internal TypeName(string nameSpace, string typeName) {
            Namespace = nameSpace;
            Name = typeName;
        }

        internal string Namespace { get; private set; }
        internal string Name { get; private set; }

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

        public static bool operator ==(TypeName a, TypeName b) {
            return a.Namespace == b.Namespace && a.Name == b.Name;
        }

        public static bool operator !=(TypeName a, TypeName b) {
            return !(a == b);
        }
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
