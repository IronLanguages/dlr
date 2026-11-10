// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Scripting.Runtime;

using NUnit.Framework;

using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Dynamic.Test {
    [TestFixture]
    public class AsyncInterruptTests {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

        private static Expression Await(Task task) => AstUtils.Await(Expression.Constant(task, typeof(Task)));

        private static Expression Call(Action action) => Expression.Invoke(Expression.Constant(action));

        private static Expression Value(object value) => Expression.Constant(value, typeof(object));

        private static Expression CatchInvalidOperation(Expression body, Expression handler)
            => Expression.TryCatch(body, Expression.Catch(typeof(InvalidOperationException), handler));

        private static Task<object> Run(Expression body, AsyncInterruptSource source, CancellationToken cancellationToken = default) {
            var lambda = Expression.Lambda<Func<Task<object>>>(
                AstUtils.Async("test", body, Expression.Constant(source, typeof(AsyncInterruptSource)), Expression.Constant(cancellationToken)));
            return lambda.Compile()();
        }

        private static void WaitFor(Task task) {
            Assert.That(((IAsyncResult)task).AsyncWaitHandle.WaitOne(s_timeout), "the body didn't finish");
        }

        private static Task Pending() => new TaskCompletionSource<object>().Task;

        [Test]
        public void InterruptAtPendingAwait() {
            var source = new AsyncInterruptSource();
            Task<object> task = Run(CatchInvalidOperation(Await(Pending()), Value("caught")), source);
            Assert.That(task.IsCompleted, Is.False);

            Assert.That(source.Interrupt(new InvalidOperationException()), Is.True);
            WaitFor(task);
            Assert.That(task.Result, Is.EqualTo("caught"));
        }

        [Test]
        public void InterruptRepeatedly() {
            var source = new AsyncInterruptSource();
            var log = new List<string>();
            var atSecond = new ManualResetEventSlim();
            Task<object> task = Run(
                Expression.Block(
                    CatchInvalidOperation(Await(Pending()), Expression.Block(Call(() => log.Add("first")), Value(null))),
                    Call(atSecond.Set),
                    CatchInvalidOperation(Await(Pending()), Expression.Block(Call(() => log.Add("second")), Value(null))),
                    Value("done")),
                source);

            Assert.That(source.Interrupt(new InvalidOperationException()), Is.True);
            Assert.That(atSecond.Wait(s_timeout));
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.True);
            WaitFor(task);
            Assert.That(task.Result, Is.EqualTo("done"));
            Assert.That(log, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void InterruptWhileRunningIsRaisedAtNextCompletedAwait() {
            var source = new AsyncInterruptSource();
            Task<object> task = Run(
                Expression.Block(
                    Call(() => source.Interrupt(new InvalidOperationException())),
                    CatchInvalidOperation(Await(Task.FromResult<object>(1)), Value("caught"))),
                source);
            WaitFor(task);
            Assert.That(task.Result, Is.EqualTo("caught"));
        }

        [Test]
        public void InterruptWhileRunningIsRaisedAtNextPendingAwait() {
            var source = new AsyncInterruptSource();
            Task<object> task = Run(
                Expression.Block(
                    Await(Task.Delay(1)),   // continue on another thread
                    Call(() => source.Interrupt(new InvalidOperationException())),
                    CatchInvalidOperation(Await(Pending()), Value("caught"))),
                source);
            WaitFor(task);
            Assert.That(task.Result, Is.EqualTo("caught"));
        }

        [Test]
        public void SecondInterruptWhilePendingIsMerged() {
            var source = new AsyncInterruptSource();
            var first = new InvalidOperationException("first");
            Task<object> task = Run(
                Expression.Block(
                    Call(() => {
                        Assert.That(source.Interrupt(first), Is.True);
                        Assert.That(source.Interrupt(new InvalidOperationException("second")), Is.True);
                    }),
                    Await(Task.FromResult<object>(1)),
                    Value("not interrupted")),
                source);
            WaitFor(task);
            Assert.That(task.Exception!.InnerException, Is.SameAs(first));
        }

        [Test]
        public void InterruptPendingWhenBodyReturnsFailsTask() {
            var source = new AsyncInterruptSource();
            var interrupt = new InvalidOperationException();
            Task<object> task = Run(
                Expression.Block(
                    Call(() => source.Interrupt(interrupt)),
                    Value("result")),
                source);
            WaitFor(task);
            Assert.That(task.IsFaulted);
            Assert.That(task.Exception!.InnerException, Is.SameAs(interrupt));
        }

        [Test]
        public void BodyExceptionWinsOverPendingInterrupt() {
            var source = new AsyncInterruptSource();
            Task<object> task = Run(
                Expression.Block(
                    Call(() => source.Interrupt(new InvalidOperationException())),
                    Expression.Throw(Expression.Constant(new ArgumentException())),
                    Value(null)),
                source);
            WaitFor(task);
            Assert.That(task.Exception!.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.False);
        }

        [Test]
        public void InterruptBeforeStartFailsTaskWithoutRunningBody() {
            var source = new AsyncInterruptSource();
            var interrupt = new InvalidOperationException();
            var log = new List<string>();
            Assert.That(source.Interrupt(interrupt), Is.True);

            Task<object> task = Run(Expression.Block(Call(() => log.Add("ran")), Value(null)), source);
            WaitFor(task);
            Assert.That(task.Exception!.InnerException, Is.SameAs(interrupt));
            Assert.That(log, Is.Empty);
        }

        [Test]
        public void InterruptAfterEndIsRefused() {
            var source = new AsyncInterruptSource();
            Task<object> task = Run(Value("done"), source);
            WaitFor(task);
            Assert.That(task.Result, Is.EqualTo("done"));
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.False);
        }

        [Test]
        public void SourceServesOneBody() {
            var source = new AsyncInterruptSource();
            var lambda = Expression.Lambda<Func<Task<object>>>(
                AstUtils.Async("test", Await(Pending()), Expression.Constant(source, typeof(AsyncInterruptSource)))).Compile();
            Task<object> first = lambda();
            Task<object> second = lambda();
            WaitFor(second);
            Assert.That(second.Exception!.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(first.IsCompleted, Is.False);
        }

        [Test]
        public void CancellationTokenIsReportedAtPendingAwait() {
            var source = new AsyncInterruptSource();
            using var cts = new CancellationTokenSource();
            CancellationToken seen = default;
            var oce = Expression.Parameter(typeof(OperationCanceledException), "oce");
            Task<object> task = Run(
                Expression.TryCatch(
                    Await(Pending()),
                    Expression.Catch(oce, Expression.Block(
                        Expression.Invoke(Expression.Constant((Action<OperationCanceledException>)(e => seen = e.CancellationToken)), oce),
                        Value("cancelled")))),
                source, cts.Token);

            cts.Cancel();
            WaitFor(task);
            Assert.That(task.Result, Is.EqualTo("cancelled"));
            Assert.That(seen, Is.EqualTo(cts.Token));
        }

        [Test]
        public void CancellationTokenCancelsTask() {
            var source = new AsyncInterruptSource();
            using var cts = new CancellationTokenSource();
            Task<object> task = Run(Await(Pending()), source, cts.Token);

            cts.Cancel();
            WaitFor(task);
            Assert.That(task.IsCanceled);
        }

        [Test]
        public void CancellationTokenStaysCancelledAfterInterrupt() {
            var source = new AsyncInterruptSource();
            using var cts = new CancellationTokenSource();
            Task<object> task = Run(
                Expression.Block(
                    CatchInvalidOperation(Await(Pending()), Value(null)),
                    Await(Pending())),
                source, cts.Token);

            Assert.That(source.Interrupt(new InvalidOperationException()), Is.True);
            cts.Cancel();
            WaitFor(task);
            Assert.That(task.IsCanceled);
        }

#if NET
        private static IAsyncEnumerator<object> Enumerate(Func<LabelTarget, Expression> body, AsyncInterruptSource source) {
            LabelTarget yieldLabel = Expression.Label(typeof(object), "yield");
            var lambda = Expression.Lambda<Func<IAsyncEnumerable<object>>>(
                AstUtils.AsyncEnumerable("test", body(yieldLabel), yieldLabel, Expression.Constant(source, typeof(AsyncInterruptSource))));
            return lambda.Compile()().GetAsyncEnumerator();
        }

        private static bool MoveNext(IAsyncEnumerator<object> enumerator) {
            Task<bool> next = enumerator.MoveNextAsync().AsTask();
            WaitFor(next);
            return next.Result;
        }

        [Test]
        public void AsyncEnumerableInterruptAtPendingAwait() {
            var source = new AsyncInterruptSource();
            IAsyncEnumerator<object> enumerator = Enumerate(
                label => Expression.Block(
                    CatchInvalidOperation(Await(Pending()), Value(null)),
                    AstUtils.YieldReturn(label, Value("caught"))),
                source);

            Task<bool> next = enumerator.MoveNextAsync().AsTask();
            Assert.That(next.IsCompleted, Is.False);
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.True);
            WaitFor(next);
            Assert.That(next.Result);
            Assert.That(enumerator.Current, Is.EqualTo("caught"));
        }

        [Test]
        public void AsyncEnumerableInterruptAtYieldIsRaisedAtNextAwait() {
            var source = new AsyncInterruptSource();
            IAsyncEnumerator<object> enumerator = Enumerate(
                label => Expression.Block(
                    AstUtils.YieldReturn(label, Value("first")),
                    AstUtils.YieldReturn(label, Value("second")),
                    CatchInvalidOperation(Await(Task.FromResult<object>(1)), Value(null)),
                    AstUtils.YieldReturn(label, Value("after await"))),
                source);

            Assert.That(MoveNext(enumerator));
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.True);
            Assert.That(MoveNext(enumerator));
            Assert.That(enumerator.Current, Is.EqualTo("second"));
            Assert.That(MoveNext(enumerator));
            Assert.That(enumerator.Current, Is.EqualTo("after await"));
            Assert.That(MoveNext(enumerator), Is.False);
        }

        [Test]
        public void AsyncEnumerableInterruptPendingAtEndFailsEnumeration() {
            var source = new AsyncInterruptSource();
            var interrupt = new InvalidOperationException();
            IAsyncEnumerator<object> enumerator = Enumerate(label => AstUtils.YieldReturn(label, Value("only")), source);

            Assert.That(MoveNext(enumerator));
            Assert.That(source.Interrupt(interrupt), Is.True);
            Task<bool> next = enumerator.MoveNextAsync().AsTask();
            WaitFor(next);
            Assert.That(next.Exception!.InnerException, Is.SameAs(interrupt));
        }

        [Test]
        public void AsyncEnumerableDisposedEarlyRefusesInterrupts() {
            var source = new AsyncInterruptSource();
            IAsyncEnumerator<object> enumerator = Enumerate(
                label => Expression.Block(
                    AstUtils.YieldReturn(label, Value("first")),
                    AstUtils.YieldReturn(label, Value("second"))),
                source);

            Assert.That(MoveNext(enumerator));
            WaitFor(enumerator.DisposeAsync().AsTask());
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.False);
        }
#endif
    }
}
