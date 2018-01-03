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
