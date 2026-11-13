// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Runtime;

using NUnit.Framework;

using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Dynamic.Test {
    [TestFixture]
    public class AsyncObserverTests {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

        private sealed class Recorder : IObserver<AsyncBodyEvent> {
            private readonly List<string> _log = [];
            private readonly List<AsyncBodyEvent> _events = [];

            public Exception Error { get; private set; }

            public List<string> Log { get { lock (_log) return [.. _log]; } }

            public List<AsyncBodyEvent> Events { get { lock (_log) return [.. _events]; } }

            public List<int> Threads { get; } = [];

            public Action<AsyncBodyEvent> OnEvent { get; set; }

            public void OnNext(AsyncBodyEvent value) {
                lock (_log) {
                    _log.Add(value.Kind.ToString());
                    _events.Add(value);
                    Threads.Add(Environment.CurrentManagedThreadId);
                }
                OnEvent?.Invoke(value);
            }

            public void OnError(Exception error) {
                lock (_log) {
                    _log.Add(nameof(OnError));
                    Error = error;
                }
            }

            public void OnCompleted() {
                lock (_log) _log.Add(nameof(OnCompleted));
            }
        }

        /// <summary>Runs posted callbacks only when the test asks for them.</summary>
        private sealed class QueueContext : SynchronizationContext {
            private readonly BlockingCollection<(SendOrPostCallback, object)> _queue = [];

            public int Count => _queue.Count;

            public override void Post(SendOrPostCallback d, object state) => _queue.Add((d, state));

            public void RunOne() {
                Assert.That(_queue.TryTake(out var item, s_timeout), "nothing was posted");
                Run(() => item.Item1(item.Item2));
            }

            public T Run<T>(Func<T> action) {
                SynchronizationContext saved = Current;
                SetSynchronizationContext(this);
                try {
                    return action();
                } finally {
                    SetSynchronizationContext(saved);
                }
            }

            private void Run(Action action) => Run(() => { action(); return 0; });
        }

        private static Expression Await(Task task) => AstUtils.Await(Expression.Constant(task, typeof(Task)));

        private static Expression Call(Action action) => Expression.Invoke(Expression.Constant(action));

        private static Expression Value(object value) => Expression.Constant(value, typeof(object));

        private static Expression CatchInvalidOperation(Expression body, Expression handler)
            => Expression.TryCatch(body, Expression.Catch(typeof(InvalidOperationException), handler));

        private static Func<Task<object>> Compile(Expression body, Expression observer, AsyncInterruptSource source = null) {
            AsyncExpression node = AstUtils.Async("test", body, Expression.Constant(source, typeof(AsyncInterruptSource))).WithObserver(observer);
            return Expression.Lambda<Func<Task<object>>>(node).Compile();
        }

        private static Task<object> Run(Expression body, Recorder observer, AsyncInterruptSource source = null)
            => Compile(body, Expression.Constant(observer), source)();

        private static void WaitFor(Task task) {
            Assert.That(((IAsyncResult)task).AsyncWaitHandle.WaitOne(s_timeout), "the body didn't finish");
        }

        private static Task Pending() => new TaskCompletionSource<object>().Task;

        // Completed by the test once the body is suspended at its await; the body then continues on another thread.
        private static TaskCompletionSource<object> Awaited() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        [Test]
        public void WithObserverTwiceThrows() {
            var observer = Expression.Constant(new Recorder());
            AsyncExpression node = AstUtils.Async("test", Value(1)).WithObserver(observer);
            Assert.Throws<InvalidOperationException>(() => node.WithObserver(Expression.Constant(new Recorder())));
        }

        [Test]
        public void WithObserverRequiresObserverType() {
            AsyncExpression node = AstUtils.Async("test", Value(1));
            Assert.Throws<ArgumentException>(() => node.WithObserver(Value(new Recorder())));
            Assert.Throws<ArgumentNullException>(() => node.WithObserver(null));
        }

        [Test]
        public void WithObserverKeepsOtherChildren() {
            Expression interrupts = Expression.Constant(new AsyncInterruptSource());
            Expression token = Expression.Constant(new CancellationToken(true));
            AsyncExpression node = AstUtils.Async("test", Value(1), interrupts, token).WithObserver(Expression.Constant(new Recorder()));
            Assert.That(node.Interrupts, Is.SameAs(interrupts));
            Assert.That(node.CancellationToken, Is.SameAs(token));
        }

        [Test]
        public void PendingAwait() {
            var observer = new Recorder();
            TaskCompletionSource<object> awaited = Awaited();
            int bodyThread = 0;
            Task<object> task = Run(
                Expression.Block(
                    Await(awaited.Task),
                    Call(() => bodyThread = Environment.CurrentManagedThreadId),
                    Value("result")),
                observer);
            awaited.SetResult(null);
            WaitFor(task);

            Assert.That(task.Result, Is.EqualTo("result"));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Awaiting", "Resumed", "Returning", "OnCompleted"]));
            List<AsyncBodyEvent> events = observer.Events;
            Assert.That(events[0].Payload, Is.Null);
            Assert.That(events[1].Payload, Is.SameAs(awaited.Task));
            Assert.That(events[2].Payload, Is.Null);
            Assert.That(events[3].Payload, Is.EqualTo("result"));
            Assert.That(observer.Threads[2], Is.EqualTo(bodyThread), "Resumed must come on the thread that continues the body");
        }

        [Test]
        public void CompletedAwaitDoesNotSuspend() {
            var observer = new Recorder();
            Task<object> task = Run(Expression.Block(Await(Task.FromResult<object>(1)), Value("result")), observer);
            WaitFor(task);

            Assert.That(task.Result, Is.EqualTo("result"));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Returning", "OnCompleted"]));
        }

        [Test]
        public void BodyRaises() {
            var observer = new Recorder();
            var error = new ArgumentException();
            TaskCompletionSource<object> awaited = Awaited();
            Task<object> task = Run(
                Expression.Block(
                    Await(awaited.Task),
                    Expression.Throw(Expression.Constant(error)),
                    Value(null)),
                observer);
            awaited.SetResult(null);
            WaitFor(task);

            Assert.That(task.Exception!.InnerException, Is.SameAs(error));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Awaiting", "Resumed", "OnError"]));
            Assert.That(observer.Error, Is.SameAs(error));
        }

        [Test]
        public void CaughtExceptionIsNotReported() {
            var observer = new Recorder();
            Task<object> task = Run(
                CatchInvalidOperation(Expression.Throw(Expression.Constant(new InvalidOperationException()), typeof(object)), Value("caught")),
                observer);
            WaitFor(task);

            Assert.That(task.Result, Is.EqualTo("caught"));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Returning", "OnCompleted"]));
        }

        [Test]
        public void InterruptBeforeStartIsOnlyError() {
            var observer = new Recorder();
            var source = new AsyncInterruptSource();
            var interrupt = new InvalidOperationException();
            Assert.That(source.Interrupt(interrupt), Is.True);

            Task<object> task = Run(Value(null), observer, source);
            WaitFor(task);

            Assert.That(task.Exception!.InnerException, Is.SameAs(interrupt));
            Assert.That(observer.Log, Is.EqualTo(["OnError"]));
            Assert.That(observer.Error, Is.SameAs(interrupt));
        }

        [Test]
        public void InterruptPendingWhenBodyReturns() {
            var observer = new Recorder();
            var source = new AsyncInterruptSource();
            var interrupt = new InvalidOperationException();
            Task<object> task = Run(Expression.Block(Call(() => source.Interrupt(interrupt)), Value("result")), observer, source);
            WaitFor(task);

            Assert.That(task.Exception!.InnerException, Is.SameAs(interrupt));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Returning", "OnError"]));
            Assert.That(observer.Events[1].Payload, Is.EqualTo("result"));
            Assert.That(observer.Error, Is.SameAs(interrupt));
        }

        [Test]
        public void InterruptAtPendingAwaitSupersedesNothing() {
            var observer = new Recorder();
            var source = new AsyncInterruptSource();
            Task<object> task = Run(CatchInvalidOperation(Await(Pending()), Value("caught")), observer, source);
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.True);
            WaitFor(task);

            Assert.That(task.Result, Is.EqualTo("caught"));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Awaiting", "Resumed", "Returning", "OnCompleted"]));
            Assert.That(observer.Events[2].Payload, Is.Null);
        }

        [Test]
        public void InterruptSupersedesFaultedAwait() {
            var observer = new Recorder();
            var source = new AsyncInterruptSource();
            var awaited = new TaskCompletionSource<object>();
            var fault = new ArgumentException();
            var interrupt = new InvalidOperationException();
            var context = new QueueContext();

            // The context holds the body's continuation, so the awaited task faults and the interrupt arrives before the body resumes.
            Task<object> task = context.Run(() => Run(CatchInvalidOperation(Await(awaited.Task), Value("caught")), observer, source));
            awaited.SetException(fault);
            SpinWait.SpinUntil(() => context.Count > 0, s_timeout);
            Assert.That(source.Interrupt(interrupt), Is.True);
            context.RunOne();
            WaitFor(task);

            Assert.That(task.Result, Is.EqualTo("caught"));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Awaiting", "Resumed", "Returning", "OnCompleted"]));
            Assert.That(observer.Events[2].Payload, Is.SameAs(fault));
        }

        [Test]
        public void ObserverIsEvaluatedForEachBody() {
            var observers = new List<Recorder>();
            Func<Task<object>> run = Compile(
                Value("result"),
                Expression.Invoke(Expression.Constant((Func<Recorder>)(() => { var o = new Recorder(); observers.Add(o); return o; }))));
            WaitFor(run());
            WaitFor(run());

            Assert.That(observers, Has.Count.EqualTo(2));
            Assert.That(observers.Select(o => o.Log), Has.All.EqualTo(new[] { "Started", "Returning", "OnCompleted" }));
        }

        [Test]
        public void NullObserverIsIgnored() {
            Func<Task<object>> run = Compile(Expression.Block(Await(Task.Delay(1)), Value("result")), Expression.Constant(null, typeof(Recorder)));
            Task<object> task = run();
            WaitFor(task);
            Assert.That(task.Result, Is.EqualTo("result"));
        }

        [Test]
        public void ThrowingObserverFailsTask() {
            var observer = new Recorder();
            var error = new ArgumentException();
            observer.OnEvent = e => { if (e.Kind == AsyncBodyEventKind.Started) throw error; };
            var log = new List<string>();
            var source = new AsyncInterruptSource();
            Task<object> task = Run(Expression.Block(Call(() => log.Add("ran")), Value(null)), observer, source);
            WaitFor(task);

            Assert.That(task.Exception!.InnerException, Is.SameAs(error));
            Assert.That(log, Is.Empty);
            Assert.That(observer.Log, Is.EqualTo(["Started"]));
            Assert.That(source.Interrupt(new InvalidOperationException()), Is.False);
        }

#if NET
        private static IAsyncEnumerator<object> Enumerate(Func<LabelTarget, Expression> body, Recorder observer, AsyncInterruptSource source = null) {
            LabelTarget yieldLabel = Expression.Label(typeof(object), "yield");
            AsyncEnumerableExpression node = AstUtils
                .AsyncEnumerable("test", body(yieldLabel), yieldLabel, Expression.Constant(source, typeof(AsyncInterruptSource)))
                .WithObserver(Expression.Constant(observer));
            return Expression.Lambda<Func<IAsyncEnumerable<object>>>(node).Compile()().GetAsyncEnumerator();
        }

        private static bool MoveNext(IAsyncEnumerator<object> enumerator) {
            Task<bool> next = enumerator.MoveNextAsync().AsTask();
            WaitFor(next);
            return next.Result;
        }

        [Test]
        public void AsyncEnumerableWithObserverTwiceThrows() {
            LabelTarget yieldLabel = Expression.Label(typeof(object), "yield");
            AsyncEnumerableExpression node = AstUtils.AsyncEnumerable("test", Expression.Empty(), yieldLabel)
                .WithObserver(Expression.Constant(new Recorder()));
            Assert.Throws<InvalidOperationException>(() => node.WithObserver(Expression.Constant(new Recorder())));
        }

        [Test]
        public void AsyncEnumerableYieldsAndAwaits() {
            var observer = new Recorder();
            TaskCompletionSource<object> awaited = Awaited();
            IAsyncEnumerator<object> enumerator = Enumerate(
                label => Expression.Block(
                    AstUtils.YieldReturn(label, Value("first")),
                    Await(awaited.Task),
                    AstUtils.YieldReturn(label, Value("second"))),
                observer);

            Assert.That(MoveNext(enumerator));
            Task<bool> next = enumerator.MoveNextAsync().AsTask();
            Assert.That(next.IsCompleted, Is.False);
            awaited.SetResult(null);
            WaitFor(next);
            Assert.That(next.Result);
            Assert.That(MoveNext(enumerator), Is.False);

            Assert.That(observer.Log, Is.EqualTo(
                ["Started", "Yielding", "Resumed", "Awaiting", "Resumed", "Yielding", "Resumed", "Returning", "OnCompleted"]));
            List<AsyncBodyEvent> events = observer.Events;
            Assert.That(events[1].Payload, Is.EqualTo("first"));
            Assert.That(events[3].Payload, Is.SameAs(awaited.Task));
            Assert.That(events[5].Payload, Is.EqualTo("second"));
            Assert.That(events[7].Payload, Is.Null);
        }

        [Test]
        public void AsyncEnumerableAbandonedAtYield() {
            var observer = new Recorder();
            IAsyncEnumerator<object> enumerator = Enumerate(
                label => Expression.Block(
                    AstUtils.YieldReturn(label, Value("first")),
                    AstUtils.YieldReturn(label, Value("second"))),
                observer);

            Assert.That(MoveNext(enumerator));
            WaitFor(enumerator.DisposeAsync().AsTask());

            Assert.That(observer.Log, Is.EqualTo(["Started", "Yielding", "OnCompleted"]));
        }

        [Test]
        public void AsyncEnumerableNeverStarted() {
            var observer = new Recorder();
            IAsyncEnumerator<object> enumerator = Enumerate(label => AstUtils.YieldReturn(label, Value("first")), observer);
            WaitFor(enumerator.DisposeAsync().AsTask());

            Assert.That(observer.Log, Is.Empty);
        }

        [Test]
        public void AsyncEnumerableBodyRaises() {
            var observer = new Recorder();
            var error = new ArgumentException();
            IAsyncEnumerator<object> enumerator = Enumerate(
                label => Expression.Block(
                    AstUtils.YieldReturn(label, Value("first")),
                    Expression.Throw(Expression.Constant(error))),
                observer);

            Assert.That(MoveNext(enumerator));
            Task<bool> next = enumerator.MoveNextAsync().AsTask();
            WaitFor(next);

            Assert.That(next.Exception!.InnerException, Is.SameAs(error));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Yielding", "Resumed", "OnError"]));
            Assert.That(observer.Error, Is.SameAs(error));
        }

        [Test]
        public void AsyncEnumerableInterruptPendingAtEnd() {
            var observer = new Recorder();
            var source = new AsyncInterruptSource();
            var interrupt = new InvalidOperationException();
            IAsyncEnumerator<object> enumerator = Enumerate(label => AstUtils.YieldReturn(label, Value("only")), observer, source);

            Assert.That(MoveNext(enumerator));
            Assert.That(source.Interrupt(interrupt), Is.True);
            Task<bool> next = enumerator.MoveNextAsync().AsTask();
            WaitFor(next);

            Assert.That(next.Exception!.InnerException, Is.SameAs(interrupt));
            Assert.That(observer.Log, Is.EqualTo(["Started", "Yielding", "Resumed", "Returning", "OnError"]));
            Assert.That(observer.Error, Is.SameAs(interrupt));
        }
#endif
    }
}
