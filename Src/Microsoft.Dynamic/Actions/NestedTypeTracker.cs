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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    public class NestedTypeTracker : TypeTracker {
        private readonly Type _type;

        public NestedTypeTracker(Type type) {
            _type = type;
        }

        public override Type DeclaringType => _type.DeclaringType;

        public override TrackerTypes MemberType => TrackerTypes.Type;

        public override string Name => _type.Name;

        public override bool IsPublic => _type.IsPublic();

        public override Type Type => _type;

        public override bool IsGenericType => _type.IsGenericType();

        public override string ToString() {
            return _type.ToString();
        }
    }
}
