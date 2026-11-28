// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using System.Linq.Expressions;

using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;

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

    [Test]
    public void ReturnMemberTrackerBindsToRuntimeInstance() {
        var target = new Target(40);
        var method = MemberTracker.FromMemberInfo(typeof(Target).GetMethod(nameof(Target.Add)));
        var instance = new DynamicMetaObject(Expression.Constant(target), BindingRestrictions.Empty, target);

        var result = new DefaultBinder().ReturnMemberTracker(typeof(Target), new BoundMemberTracker(method, instance));
        object value = Expression.Lambda<Func<object>>(result.Expression).Compile()();

        Assert.That(value, Is.TypeOf<RuntimeBoundMemberTracker>());
        var bound = (RuntimeBoundMemberTracker)value;
        Assert.That(bound.BoundTo, Is.SameAs(method));
        Assert.That(bound.Instance, Is.SameAs(target));
        Assert.That(bound.MemberType, Is.EqualTo(TrackerTypes.Bound));
        Assert.That(bound.Name, Is.EqualTo(nameof(Target.Add)));
        Assert.That(bound.DeclaringType, Is.EqualTo(typeof(Target)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CallInvokesBoundMethodOnRuntimeInstance(bool useObsoleteTracker) {
        var add = MemberTracker.FromMemberInfo(typeof(Target).GetMethod(nameof(Target.Add)));
        var subtract = MemberTracker.FromMemberInfo(typeof(Target).GetMethod(nameof(Target.Subtract)));
        var bound = BindToRuntimeInstance(add, new Target(40), useObsoleteTracker);
        var self = Expression.Parameter(typeof(object), "self");

        var result = new DefaultBinder().Call(
            new CallSignature(1),
            new DynamicMetaObject(self, BindingRestrictions.Empty, bound),
            new DynamicMetaObject(Expression.Constant(2), BindingRestrictions.Empty, 2)
        );
        var test = Expression.Lambda<Func<object, bool>>(result.Restrictions.ToExpression(), self).Compile();
        var body = Expression.Lambda<Func<object, object>>(result.Expression, self).Compile();

        Assert.That(test(bound), Is.True);
        Assert.That(body(bound), Is.EqualTo(42));

        // the instance is read from the tracker at run time
        var boundToOther = BindToRuntimeInstance(add, new Target(10), useObsoleteTracker);
        Assert.That(test(boundToOther), Is.True);
        Assert.That(body(boundToOther), Is.EqualTo(12));

        Assert.That(test(BindToRuntimeInstance(subtract, new Target(40), useObsoleteTracker)), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SetEventValidatesRuntimeBoundEvent(bool useObsoleteTracker) {
        var changed = (EventTracker)MemberTracker.FromMemberInfo(typeof(Target).GetEvent(nameof(Target.Changed)));
        var renamed = (EventTracker)MemberTracker.FromMemberInfo(typeof(Target).GetEvent(nameof(Target.Renamed)));
        var bound = BindToRuntimeInstance(changed, new Target(0), useObsoleteTracker);

        Assert.That(() => BinderOps.SetEvent(changed, bound), Throws.Nothing);
        Assert.That(() => ScriptingRuntimeHelpers.SetEvent(changed, bound), Throws.Nothing);
        Assert.That(() => BinderOps.SetEvent(renamed, bound), Throws.ArgumentException);
        Assert.That(() => ScriptingRuntimeHelpers.SetEvent(renamed, bound), Throws.ArgumentException);
    }

    private static MemberTracker BindToRuntimeInstance(MemberTracker tracker, object instance, bool useObsoleteTracker) {
#pragma warning disable CS0618 // Type or member is obsolete
        return useObsoleteTracker ? new BoundMemberTracker(tracker, instance) : new RuntimeBoundMemberTracker(tracker, instance);
#pragma warning restore CS0618
    }

    public sealed class Target {
        private readonly int _value;

        public Target(int value) {
            _value = value;
        }

        public int Add(int x) => _value + x;

        public int Subtract(int x) => _value - x;

        public event EventHandler Changed { add { } remove { } }

        public event EventHandler Renamed { add { } remove { } }
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
