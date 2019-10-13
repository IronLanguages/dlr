// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace Microsoft.Scripting.ComInterop {

    public class ComTypeDesc : ComTypeLibMemberDesc {
        private readonly string _typeName;
        private readonly string _documentation;
        private ComMethodDesc _getItem;
        private ComMethodDesc _setItem;
        private static readonly Dictionary<string, ComEventDesc> _EmptyEventsDict = new Dictionary<string, ComEventDesc>();

        internal ComTypeDesc(ITypeInfo typeInfo, ComType memberType, ComTypeLibDesc typeLibDesc) : base(memberType) {
            if (typeInfo != null) {
                ComRuntimeHelpers.GetInfoFromType(typeInfo, out _typeName, out _documentation);
            }
            TypeLib = typeLibDesc;
        }

        
        internal static ComTypeDesc FromITypeInfo(ITypeInfo typeInfo, TYPEATTR typeAttr)
        {
            switch (typeAttr.typekind) {
                case TYPEKIND.TKIND_COCLASS:
                    return new ComTypeClassDesc(typeInfo, null);
                case TYPEKIND.TKIND_ENUM:
                    return new ComTypeEnumDesc(typeInfo, null);
                case TYPEKIND.TKIND_DISPATCH:
                case TYPEKIND.TKIND_INTERFACE:
                    ComTypeDesc typeDesc = new ComTypeDesc(typeInfo, ComType.Interface, null);
                    return typeDesc;
                default:
                    throw new InvalidOperationException("Attempting to wrap an unsupported enum type.");
            }
        }

        internal static ComTypeDesc CreateEmptyTypeDesc() {
            ComTypeDesc typeDesc = new ComTypeDesc(null, ComType.Interface, null);
            typeDesc.Funcs = new ConcurrentDictionary<string, ComMethodDesc>();
            typeDesc.Puts = new ConcurrentDictionary<string, ComMethodDesc>();
            typeDesc.PutRefs = new ConcurrentDictionary<string, ComMethodDesc>();
            typeDesc.Events = _EmptyEventsDict;

            return typeDesc;
        }

        internal static Dictionary<string, ComEventDesc> EmptyEvents => _EmptyEventsDict;

        internal ConcurrentDictionary<string, ComMethodDesc> Funcs { get; set; }

        internal ConcurrentDictionary<string, ComMethodDesc> Puts { get; set; }

        internal ConcurrentDictionary<string, ComMethodDesc> PutRefs { get; set; }

        internal Dictionary<string, ComEventDesc> Events { get; set; }

        internal bool TryGetFunc(string name, out ComMethodDesc method) {
            name = name.ToUpper(CultureInfo.InvariantCulture);
            if (Funcs.TryGetValue(name, out method)) {
                return true;
            }

            return false;
        }

        internal void AddFunc(string name, ComMethodDesc method) {
            name = name.ToUpper(CultureInfo.InvariantCulture);
            Funcs[name] = method;
        }

        internal bool TryGetPut(string name, out ComMethodDesc method) {
            name = name.ToUpper(CultureInfo.InvariantCulture);
            if (Puts.TryGetValue(name, out method)) {
                return true;
            }

            return false;
        }

        internal void AddPut(string name, ComMethodDesc method) {
            name = name.ToUpper(CultureInfo.InvariantCulture);
            Puts[name] = method;
        }

        internal bool TryGetPutRef(string name, out ComMethodDesc method) {
            name = name.ToUpper(CultureInfo.InvariantCulture);
            if (PutRefs.TryGetValue(name, out method)) {
                return true;
            }

            return false;
        }
        internal void AddPutRef(string name, ComMethodDesc method) {
            name = name.ToUpper(CultureInfo.InvariantCulture);
             PutRefs[name] = method;

        }

        internal bool TryGetEvent(string name, out ComEventDesc @event) {
            name = name.ToUpper(CultureInfo.InvariantCulture);
            return Events.TryGetValue(name, out @event);
        }

        internal string[] GetMemberNames(bool dataOnly) {
            var names = new Dictionary<string, object>();

            lock (Funcs) {
                foreach (ComMethodDesc func in Funcs.Values) {
                    if (!dataOnly || func.IsDataMember) {
                        names.Add(func.Name, null);
                    }
                }
            }

            if (!dataOnly) {
                lock (Puts) {
                    foreach (ComMethodDesc func in Puts.Values) {
                        if (!names.ContainsKey(func.Name)) {
                            names.Add(func.Name, null);
                        }
                    }
                }

                lock (PutRefs) {
                    foreach (ComMethodDesc func in PutRefs.Values) {
                        if (!names.ContainsKey(func.Name)) {
                            names.Add(func.Name, null);
                        }
                    }
                }

                if (Events != null && Events.Count > 0) {
                    foreach (string name in Events.Keys) {
                        if (!names.ContainsKey(name)) {
                            names.Add(name, null);
                        }
                    }
                }
            }

            string[] result = new string[names.Keys.Count];
            names.Keys.CopyTo(result, 0);
            return result;
        }

        // this property is public - accessed by an AST
        public string TypeName => _typeName;

        internal string Documentation => _documentation;

        // this property is public - accessed by an AST
        public ComTypeLibDesc TypeLib { get; }

        internal Guid Guid { get; set; }

        internal ComMethodDesc GetItem => _getItem;

        internal void EnsureGetItem(ComMethodDesc candidate) {
            Interlocked.CompareExchange(ref _getItem, candidate, null);
        }

        internal ComMethodDesc SetItem => _setItem;

        internal void EnsureSetItem(ComMethodDesc candidate) {
            Interlocked.CompareExchange(ref _setItem, candidate, null);
        }
    }
}

#endif
