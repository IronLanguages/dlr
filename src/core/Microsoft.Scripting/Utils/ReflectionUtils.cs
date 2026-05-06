// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Microsoft.Scripting.Utils {
    internal static class ReflectionUtils {
        public static MethodInfo GetMethodInfo(this Delegate d) {
            return d.Method;
        }

        public static IEnumerable<MethodInfo> GetDeclaredMethods(this Type type, string name) {
            return type.GetMember(name).OfType<MethodInfo>();
        }
    }
}
