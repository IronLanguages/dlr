// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Metadata;

namespace Metadata {
    public static class MetadataTablesExtensions {
#if !CCI
        public static MetadataTables GetMetadataTables(this Module module) {
            return module.ModuleHandle.GetMetadataTables();
        }
#endif

        public static bool IsNested(this TypeAttributes attrs) {
            switch (attrs & TypeAttributes.VisibilityMask) {
                case TypeAttributes.Public:
                case TypeAttributes.NotPublic:
                    return false;
            
                default:
                    return true;
            }
        }

        public static void GetName(this CustomAttributeDef ca, out MetadataName name, out MetadataName @namespace) {
            var ctor = ca.Constructor;
            if (ctor.IsMemberRef) {
                var cls = ctor.MemberRef.Class;
                if (cls.IsTypeRef) {
                    name = cls.TypeRef.TypeName;
                    @namespace = cls.TypeRef.TypeNamespace;
                } else {
                    name = cls.TypeDef.Name;
                    @namespace = cls.TypeDef.Namespace;
                }
            } else {
                var ctorDef = ctor.MethodDef;
                TypeDef typeDef = ctorDef.FindDeclaringType();
                name = typeDef.Name;
                @namespace = typeDef.Namespace;
            }
        }

        private static readonly byte[] _ExtensionAttributeNameUtf8 = Encoding.UTF8.GetBytes("ExtensionAttribute");
        private static readonly byte[] _ExtensionAttributeNamespaceUtf8 = Encoding.UTF8.GetBytes("System.Runtime.CompilerServices");

        public static bool IsExtensionAttribute(this CustomAttributeDef ca) {
            ca.GetName(out MetadataName name, out MetadataName ns);
            return name.Equals(_ExtensionAttributeNameUtf8, 0, _ExtensionAttributeNameUtf8.Length)
                && ns.Equals(_ExtensionAttributeNamespaceUtf8, 0, _ExtensionAttributeNamespaceUtf8.Length);
        }

        public static MetadataRecord GetExtensionAttributeCtor(this MetadataTables tables) {
            AssemblyDef adef = tables.AssemblyDef;
            if (!adef.Record.IsNull) {
                foreach (CustomAttributeDef ca in adef.CustomAttributes) {
                    if (IsExtensionAttribute(ca)) {
                        return ca.Constructor;
                    }
                }
            }
            // TODO: other modules can contain extension methods too
            return default(MetadataRecord);
        }

        public static IEnumerable<MethodDef> GetVisibleExtensionMethods(this MetadataTables tables) {
            MetadataRecord eaCtor = GetExtensionAttributeCtor(tables);
            if (!eaCtor.IsNull) {
                foreach (CustomAttributeDef ca in tables.CustomAttributes) {
                    if (ca.Constructor.Equals(eaCtor) && ca.Parent.IsMethodDef) {
                        MethodDef mdef = ca.Parent.MethodDef;
                        var mattrs = mdef.Attributes;
                        if ((mattrs & MethodAttributes.MemberAccessMask) == MethodAttributes.Public && (mattrs & MethodAttributes.Static) != 0) {
                            var declType = mdef.FindDeclaringType();
                            var tattrs = declType.Attributes;
                            if (((tattrs & TypeAttributes.VisibilityMask) == TypeAttributes.Public || 
                                (tattrs & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic) &&
                                (tattrs & TypeAttributes.Abstract) != 0 &&
                                (tattrs & TypeAttributes.Sealed) != 0) {
                                yield return mdef;
                            }
                        }
                    }
                }
            }
        }

        // TODO: move TypeNesting to MetadataTables
        private static Dictionary<MetadataTables, TypeNestings> TypeNestings;

        public static TypeNestings GetTypeNesting(this MetadataTables tables) {
            if (TypeNestings == null) {
                TypeNestings = new Dictionary<MetadataTables, TypeNestings>();
            }

            lock (TypeNestings) {
                if (TypeNestings.TryGetValue(tables, out TypeNestings nestings)) {
                    nestings = new TypeNestings(tables);
                }
                return nestings;
            }
        }
    }
}
