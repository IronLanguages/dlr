// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.Scripting.Actions {
    public class ConstructorTracker : MemberTracker {
        private readonly ConstructorInfo _ctor;

        public ConstructorTracker(ConstructorInfo ctor) {
            _ctor = ctor;
        }

        public override Type DeclaringType => _ctor.DeclaringType;

        public override TrackerTypes MemberType => TrackerTypes.Constructor;

        public override string Name => _ctor.Name;

        public bool IsPublic => _ctor.IsPublic;

        public override string ToString() {
            return _ctor.ToString();
        }
    }
}
