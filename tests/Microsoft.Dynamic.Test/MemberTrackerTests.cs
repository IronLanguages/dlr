// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using System.Linq.Expressions;

using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;

using NUnit.Framework;

namespace Microsoft.Dynamic.Test;

[TestFixture]
public class MemberTrackerTests {
    [TestCase(false)]
    [TestCase(true)]
    public void BoundSetValueForwardsAssignedValue(bool useErrorSuggestion) {
        var instance = new DynamicMetaObject(Expression.Constant(new object()), BindingRestrictions.Empty);
        var value = new DynamicMetaObject(Expression.Constant(42), BindingRestrictions.Empty);
        var error = new DynamicMetaObject(Expression.Constant("error"), BindingRestrictions.Empty);
        var tracker = new RecordingTracker();
        var bound = new BoundMemberTracker(tracker, instance);

        var result = useErrorSuggestion
            ? bound.SetValue(null, null, typeof(object), value, error)
            : bound.SetValue(null, null, typeof(object), value);

        Assert.That(result, Is.SameAs(value));
        Assert.That(tracker.ErrorSuggestion, Is.SameAs(useErrorSuggestion ? error : null));
    }

    private sealed class RecordingTracker : CustomTracker {
        public override Type DeclaringType => typeof(object);
        public override string Name => "Value";
        public DynamicMetaObject ErrorSuggestion { get; private set; }

        public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType, DynamicMetaObject value) {
            return value;
        }

        public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type instanceType, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
            ErrorSuggestion = errorSuggestion;
            return value;
        }
    }
}
