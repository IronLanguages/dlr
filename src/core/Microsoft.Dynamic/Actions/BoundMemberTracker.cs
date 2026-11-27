// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Dynamic;

using Microsoft.Scripting.Actions.Calls;

namespace Microsoft.Scripting.Actions {
    // TODO: Split into binding-time (over DynamicMetaObject) and run-time (over object? value) bound member trackers
    public class BoundMemberTracker : MemberTracker {
        public BoundMemberTracker(MemberTracker tracker, DynamicMetaObject instance) {
            BoundTo = tracker;
            Instance = instance;
        }

        public BoundMemberTracker(MemberTracker tracker, object? instance) {
            BoundTo = tracker;
            ObjectInstance = instance;
        }

        public override TrackerTypes MemberType => TrackerTypes.Bound;

        public override Type? DeclaringType => BoundTo.DeclaringType;

        public override string Name => BoundTo.Name;

        public DynamicMetaObject? Instance { get; }

        public object? ObjectInstance { get; }

        public MemberTracker BoundTo { get; }

        public override DynamicMetaObject? GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType) {
            return BoundTo.GetBoundValue(resolverFactory, binder, instanceType, BoundInstance);
        }

        public override ErrorInfo? GetError(ActionBinder binder, Type instanceType) {
            return BoundTo.GetBoundError(binder, BoundInstance, instanceType);
        }

        public override DynamicMetaObject? SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType, DynamicMetaObject value) {
            return BoundTo.SetBoundValue(resolverFactory, binder, instanceType, value, BoundInstance);
        }

        public override DynamicMetaObject? SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType, DynamicMetaObject value, DynamicMetaObject? errorSuggestion) {
            return BoundTo.SetBoundValue(resolverFactory, binder, instanceType, value, BoundInstance, errorSuggestion);
        }

        private DynamicMetaObject BoundInstance => Instance ?? throw new InvalidOperationException("The tracker is bound to an object instance, not to a DynamicMetaObject.");
    }
}
