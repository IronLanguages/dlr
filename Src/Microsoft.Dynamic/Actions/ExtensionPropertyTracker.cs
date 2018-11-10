// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions {
    public class ExtensionPropertyTracker : PropertyTracker {
        private MethodInfo _getter, _setter, _deleter;

        public ExtensionPropertyTracker(string name, MethodInfo getter, MethodInfo setter, MethodInfo deleter, Type declaringType) {
            Name = name; 
            _getter = getter; 
            _setter = setter;
            _deleter = deleter;
            DeclaringType = declaringType;
        }

        public override string Name { get; }

        public override Type DeclaringType { get; }

        public override bool IsStatic => IsStaticProperty(GetGetMethod(true) ?? GetSetMethod(true));

        public override MethodInfo GetGetMethod() {
            if (_getter != null && _getter.IsPrivate) return null;

            return _getter;
        }

        public override MethodInfo GetSetMethod() {
            if (_setter != null && _setter.IsPrivate) return null;

            return _setter;
        }

        public override MethodInfo GetGetMethod(bool privateMembers) {
            if (privateMembers) return _getter;

            return GetGetMethod();
        }

        public override MethodInfo GetSetMethod(bool privateMembers) {
            if (privateMembers) return _setter;

            return GetSetMethod();
        }

        public override MethodInfo GetDeleteMethod() {
            if (_deleter != null && _deleter.IsPrivate) return null;

            return _deleter;
        }

        public override MethodInfo GetDeleteMethod(bool privateMembers) {
            return privateMembers ? _deleter : GetDeleteMethod();
        }

        public override ParameterInfo[] GetIndexParameters() {
            return new ParameterInfo[0];
        }

        private static bool IsStaticProperty(MethodInfo method) {
            return method.IsDefined(typeof(StaticExtensionMethodAttribute), false);
        }

        public override Type PropertyType {
            get {
                if (_getter != null) return _getter.ReturnType;

                ParameterInfo[] pis = _setter.GetParameters();
                return pis[pis.Length - 1].ParameterType;
            }
        }
    }
}
