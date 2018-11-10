// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    public abstract class TypeTracker : MemberTracker, IMembersList {
        internal TypeTracker() {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public abstract Type Type { get; }

        public abstract bool IsGenericType { get; }

        public abstract bool IsPublic { get; }

        #region IMembersList Members

        public virtual IList<string> GetMemberNames() {
            var members = new HashSet<string>();
            GetMemberNames(Type, members);
            return members.ToArray();
        }

        internal static void GetMemberNames(Type type, HashSet<string> result) {
            foreach (Type ancestor in type.Ancestors()) {
                foreach (MemberInfo mi in ancestor.GetDeclaredMembers()) {
                    if (!(mi is ConstructorInfo)) {
                        result.Add(mi.Name);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Enables implicit Type to TypeTracker conversions accross dynamic languages.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static explicit operator Type(TypeTracker tracker) {
            if (tracker is TypeGroup tg) {
                if (!tg.TryGetNonGenericType(out Type res)) {
                    throw ScriptingRuntimeHelpers.SimpleTypeError("expected non-generic type, got generic-only type");
                }
                return res;
            }
            return tracker.Type;
        }

        private static readonly Dictionary<Type, TypeTracker> _typeCache = new Dictionary<Type, TypeTracker>();

        public static TypeTracker GetTypeTracker(Type type) {
            TypeTracker res;

            lock (_typeCache) {
                if (!_typeCache.TryGetValue(type, out res)) {
                    _typeCache[type] = res = new NestedTypeTracker(type);
                }
            }

            return res;
        }
    }
}
