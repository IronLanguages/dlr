// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Dynamic;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    ///   Represents a member bound to a run-time instance object.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is the run-time counterpart of <see cref="BoundMemberTracker"/>, which binds a member to
    ///     a <see cref="DynamicMetaObject"/> at binding time. <see cref="ActionBinder.ReturnMemberTracker"/>
    ///     creates it by default as the value of a member bound to an instance, such as an instance method or event,
    ///     that is returned to user code. <see cref="DefaultBinder"/> binds calls on it to the bound member,
    ///     with <see cref="Instance"/> as the instance.
    ///   </para>
    ///   <para>
    ///     Since the instance is a run-time object, this tracker provides no binding-time operations
    ///     on the bound member, such as getting or setting its value.
    ///   </para>
    /// </remarks>
    public sealed class RuntimeBoundMemberTracker : MemberTracker {
        public RuntimeBoundMemberTracker(MemberTracker tracker, object? instance) {
            BoundTo = tracker;
            Instance = instance;
        }

        public override TrackerTypes MemberType => TrackerTypes.Bound;

        public override Type? DeclaringType => BoundTo.DeclaringType;

        public override string Name => BoundTo.Name;

        /// <summary>
        ///   Gets the run-time instance object the member is bound to.
        /// </summary>
        public object? Instance { get; }

        /// <summary>
        ///   Gets the member bound to the instance.
        /// </summary>
        public MemberTracker BoundTo { get; }
    }
}
