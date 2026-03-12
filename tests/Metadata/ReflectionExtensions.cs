// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metadata {
    public static class ReflectionExtensions {
#if !CCI
        public static MetadataTables GetMetadataTables(this Module module) {
            return module.ModuleHandle.GetMetadataTables();
        }
#endif

        public static IEnumerable<MethodInfo> GetVisibleExtensionMethods(this Module module) {
            var ea = typeof(ExtensionAttribute);
            if (module.Assembly.IsDefined(ea, false)) {
                foreach (Type type in module.GetTypes()) {
                    var tattrs = type.Attributes;
                    if (((tattrs & TypeAttributes.VisibilityMask) == TypeAttributes.Public ||
                        (tattrs & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic) &&
                        (tattrs & TypeAttributes.Abstract) != 0 &&
                        (tattrs & TypeAttributes.Sealed) != 0 &&
                        type.IsDefined(ea, false)) {

                        foreach (MethodInfo method in type.GetMethods()) {
                            if (method.IsPublic && method.IsStatic && method.IsDefined(ea, false)) {
                                yield return method;
                            }
                        }
                    }
                }
            }
        }
    }
}
