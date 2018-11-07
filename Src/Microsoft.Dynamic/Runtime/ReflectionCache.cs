// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Provides a cache of reflection members.  Only one set of values is ever handed out per a 
    /// specific request.
    /// </summary>
    public static class ReflectionCache {
        private static readonly Dictionary<MethodBaseCache, MethodGroup> _functions = new Dictionary<MethodBaseCache, MethodGroup>();
        private static readonly Dictionary<Type, TypeTracker> _typeCache = new Dictionary<Type, TypeTracker>();

        public static MethodGroup GetMethodGroup(string name, MethodBase[] methods) {
            MethodGroup res = null;
            MethodBaseCache cache = new MethodBaseCache(name, methods);
            lock (_functions) {
                if (!_functions.TryGetValue(cache, out res)) {
                    _functions[cache] = res = new MethodGroup(
                        ArrayUtils.ConvertAll<MethodBase, MethodTracker>(
                            methods,
                            delegate(MethodBase x) {
                                return (MethodTracker)MemberTracker.FromMemberInfo(x);
                            }
                        )
                    );
                }
            }
            return res;
        }

        public static MethodGroup GetMethodGroup(string name, MemberGroup mems) {
            MethodGroup res = null;

            MethodBase[] bases = new MethodBase[mems.Count];
            MethodTracker[] trackers = new MethodTracker[mems.Count];
            for (int i = 0; i < bases.Length; i++) {
                trackers[i] = (MethodTracker)mems[i];
                bases[i] = trackers[i].Method;
            }

            if (mems.Count != 0) {
                MethodBaseCache cache = new MethodBaseCache(name, bases);
                lock (_functions) {
                    if (!_functions.TryGetValue(cache, out res)) {
                        _functions[cache] = res = new MethodGroup(trackers);
                    }
                }
            }

            return res;
        }

        public static TypeTracker GetTypeTracker(Type type) {
            TypeTracker res;

            lock (_typeCache) {
                if (!_typeCache.TryGetValue(type, out res)) {
                    _typeCache[type] = res = new NestedTypeTracker(type);
                }
            }

            return res;
        }

        /// <summary>
        /// TODO: Make me private again
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")] // TODO: fix
        public class MethodBaseCache {
            private readonly MethodBase[] _members;
            private readonly string _name;

            public MethodBaseCache(string name, MethodBase[] members) {
                // sort by module ID / token so that the Equals / GetHashCode doesn't have
                // to line up members if reflection returns them in different orders.
                Array.Sort(members, CompareMethods);
                _name = name;
                _members = members;
            }

            private static int CompareMethods(MethodBase x, MethodBase y) {
                Module xModule = x.Module;
                Module yModule = y.Module;

                if (xModule == yModule) {
                    return x.MetadataToken - y.MetadataToken;
                }
                
                return xModule.ModuleVersionId.CompareTo(yModule.ModuleVersionId);
            }

            public override bool Equals(object obj) {
                MethodBaseCache other = obj as MethodBaseCache;
                if (other == null || _members.Length != other._members.Length || other._name != _name) {
                    return false;
                }

                for (int i = 0; i < _members.Length; i++) {
                    if (_members[i].DeclaringType != other._members[i].DeclaringType ||
                        _members[i].MetadataToken != other._members[i].MetadataToken ||
                        _members[i].IsGenericMethod != other._members[i].IsGenericMethod) {
                        return false;
                    }

                    if (_members[i].IsGenericMethod) {
                        Type[] args = _members[i].GetGenericArguments();
                        Type[] otherArgs = other._members[i].GetGenericArguments();

                        if (args.Length != otherArgs.Length) {
                            return false;
                        }

                        for (int j = 0; j < args.Length; j++) {
                            if (args[j] != otherArgs[j]) {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }

            public override int GetHashCode() {
                int res = 6551;
                foreach (MethodBase mi in _members) {
                    res ^= res << 5 ^ mi.DeclaringType.GetHashCode() ^ mi.MetadataToken;
                }
                res ^= _name.GetHashCode();

                return res;
            }
        }
    }
}
