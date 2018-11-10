// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Represents the top reflected package which contains extra information such as
    /// all the assemblies loaded and the built-in modules.
    /// </summary>
    public class TopNamespaceTracker : NamespaceTracker {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")] // TODO: fix
        private int _lastDiscovery = 0;

        internal readonly object HierarchyLock;

#if FEATURE_COM
        private static readonly Dictionary<Guid, Type> _comTypeCache = new Dictionary<Guid, Type>();
#endif

        public TopNamespaceTracker(ScriptDomainManager manager)
            : base(null) {
            ContractUtils.RequiresNotNull(manager, nameof(manager));
            SetTopPackage(this);
            DomainManager = manager;
            HierarchyLock = new object();
        }

        #region Public API Surface

        /// <summary>
        /// returns the package associated with the specified namespace and
        /// updates the associated module to mark the package as imported.
        /// </summary>
        public NamespaceTracker TryGetPackage(string name) {
            if (TryGetPackageAny(name) is NamespaceTracker pm) {
                return pm;
            }
            return null;
        }

        public MemberTracker TryGetPackageAny(string name) {
            if (TryGetValue(name, out MemberTracker ret)) {
                return ret;
            }
            return null;
        }

        public MemberTracker TryGetPackageLazy(string name) {
            lock (HierarchyLock) {
                if (_dict.TryGetValue(name, out MemberTracker ret)) {
                    return ret;
                }
                return null;
            }
        }

        /// <summary>
        /// Ensures that the assembly is loaded
        /// </summary>
        /// <param name="assem"></param>
        /// <returns>true if the assembly was loaded for the first time. 
        /// false if the assembly had already been loaded before</returns>
        public bool LoadAssembly(Assembly assem) {
            ContractUtils.RequiresNotNull(assem, nameof(assem));

            lock (HierarchyLock) {
                if (_packageAssemblies.Contains(assem)) {
                    // The assembly is already loaded. There is nothing more to do
                    return false;
                }

                _packageAssemblies.Add(assem);
                UpdateSubtreeIds();
                PublishComTypes(assem);
            }

            return true;
        }

        #endregion

        /// <summary>
        /// When an (interop) assembly is loaded, we scan it to discover the GUIDs of COM interfaces so that we can
        /// associate the type definition with COM objects with that GUID.
        /// Since scanning all loaded assemblies can be expensive, in the future, we might consider a more explicit 
        /// user binder to trigger scanning of COM types.
        /// </summary>
        public static void PublishComTypes(Assembly interopAssembly) {
#if FEATURE_COM
            lock (_comTypeCache) { // We lock over the entire operation so that we can publish a consistent view

                foreach (Type type in ReflectionUtils.GetAllTypesFromAssembly(interopAssembly, false)) {
                    if (type.IsImport && type.IsInterface) {
                        if (_comTypeCache.TryGetValue(type.GUID, out Type existing)) {
                            if (!existing.IsDefined(typeof(CoClassAttribute), false)) {
                                // prefer the type w/ CoClassAttribute on it.  Example:
                                //    MS.Office.Interop.Excel.Worksheet 
                                //          vs
                                //    MS.Office.Interop.Excel._Worksheet
                                //  Worksheet defines all the interfaces that the type supports and has CoClassAttribute.
                                //  _Worksheet is just the interface for the worksheet.
                                //
                                // They both have the same GUID though.
                                _comTypeCache[type.GUID] = type;
                            }
                        } else {
                            _comTypeCache[type.GUID] = type;
                        }
                    }
                }
            }
#endif
        }

        protected override void LoadNamespaces() {
            lock (HierarchyLock) {
                for (int i = _lastDiscovery; i < _packageAssemblies.Count; i++) {
                    DiscoverAllTypes(_packageAssemblies[i]);
                }
                _lastDiscovery = _packageAssemblies.Count;
            }
        }

        public ScriptDomainManager DomainManager { get; }
    }
}
