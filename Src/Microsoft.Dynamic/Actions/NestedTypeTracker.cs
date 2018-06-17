// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
