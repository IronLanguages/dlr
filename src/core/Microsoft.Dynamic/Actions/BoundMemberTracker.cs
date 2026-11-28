// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Dynamic;

using Microsoft.Scripting.Actions.Calls;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    ///   Represents a member bound to an instance at binding time.
    /// </summary>
    /// <remarks>
    ///   The instance is a <see cref="DynamicMetaObject"/>, so getting or setting the value of the tracker forwards
    ///   to the bound member with that instance. Use <see cref="RuntimeBoundMemberTracker"/> to bind a member
    ///   to a run-time instance object.
    /// </remarks>
    public class BoundMemberTracker : MemberTracker {
        public BoundMemberTracker(MemberTracker tracker, DynamicMetaObject instance) {
            BoundTo = tracker;
            Instance = instance;
        }

        [Obsolete("Use RuntimeBoundMemberTracker to bind a member to a run-time instance object.")]
        public BoundMemberTracker(MemberTracker tracker, object? instance) {
            BoundTo = tracker;
            ObjectInstance = instance;
        }

        public override TrackerTypes MemberType => TrackerTypes.Bound;

        public override Type? DeclaringType => BoundTo.DeclaringType;

        public override string Name => BoundTo.Name;

        /// <summary>
        ///   Gets the instance the member is bound to.
        /// </summary>
        /// <remarks>
        ///   It is <see langword="null"/> only if the tracker was created over a run-time instance object
        ///   with the obsolete constructor.
        /// </remarks>
        public DynamicMetaObject? Instance { get; }

        [Obsolete("Use RuntimeBoundMemberTracker.Instance to get the run-time instance object a member is bound to.")]
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
