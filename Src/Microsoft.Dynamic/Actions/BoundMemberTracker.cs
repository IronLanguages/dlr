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
using System.Dynamic;
using Microsoft.Scripting.Actions.Calls;

namespace Microsoft.Scripting.Actions {
    public class BoundMemberTracker : MemberTracker {
        public BoundMemberTracker(MemberTracker tracker, DynamicMetaObject instance) {
            BoundTo = tracker;
            Instance = instance;
        }

        public BoundMemberTracker(MemberTracker tracker, object instance) {
            BoundTo = tracker;
            ObjectInstance = instance;
        }

        public override TrackerTypes MemberType {
            get { return TrackerTypes.Bound; }
        }

        public override Type DeclaringType {
            get { return BoundTo.DeclaringType; }
        }

        public override string Name {
            get { return BoundTo.Name; }
        }

        public DynamicMetaObject Instance { get; }

        public object ObjectInstance { get; }

        public MemberTracker BoundTo { get; }

        public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType) {
            return BoundTo.GetBoundValue(resolverFactory, binder, instanceType, Instance);
        }

        public override ErrorInfo GetError(ActionBinder binder, Type instanceType) {
            return BoundTo.GetBoundError(binder, Instance, instanceType);
        }

        public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType, DynamicMetaObject value) {
            return BoundTo.SetBoundValue(resolverFactory, binder, instanceType, value, Instance);
        }

        public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
            return BoundTo.SetBoundValue(resolverFactory, binder, instanceType, value, Instance, errorSuggestion);
        }
    }
}
