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
using System.Reflection;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    public class ReflectedPropertyTracker : PropertyTracker {
        private readonly PropertyInfo _propInfo;

        public ReflectedPropertyTracker(PropertyInfo property) {
            _propInfo = property;
        }

        public override string Name => _propInfo.Name;

        public override Type DeclaringType => _propInfo.DeclaringType;

        public override bool IsStatic {
            get {
                MethodInfo mi = GetGetMethod(true) ?? GetSetMethod(true);

                return mi.IsStatic;
            }
        }

        public override Type PropertyType => _propInfo.PropertyType;

        public override MethodInfo GetGetMethod() {
            return _propInfo.GetGetMethod();
        }

        public override MethodInfo GetSetMethod() {
            return _propInfo.GetSetMethod();
        }

        public override MethodInfo GetGetMethod(bool privateMembers) {
            return _propInfo.GetGetMethod(privateMembers);
        }

        public override MethodInfo GetSetMethod(bool privateMembers) {
            return _propInfo.GetSetMethod(privateMembers);
        }

        public override MethodInfo GetDeleteMethod() {
            return GetDeleteMethod(false);
        }

        public override MethodInfo GetDeleteMethod(bool privateMembers) {
            MethodInfo res = _propInfo.DeclaringType.GetMethod("Delete" + _propInfo.Name, (privateMembers ? BindingFlags.NonPublic : 0) | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            if (res != null && res.IsSpecialName && res.IsDefined(typeof(PropertyMethodAttribute), true)) {
                return res;
            }

            return null;
        }


        public override ParameterInfo[] GetIndexParameters() {
            return _propInfo.GetIndexParameters();
        }

        public PropertyInfo Property => _propInfo;

        public override string ToString() {
            return _propInfo.ToString();
        }
    }
}
