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
