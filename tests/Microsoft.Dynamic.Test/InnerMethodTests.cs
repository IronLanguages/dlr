// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Generation;

using NUnit.Framework;

using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Dynamic.Test {
    /// <summary>
    ///   Generator and async nodes nested in another body reduce their own bodies to lambdas of their own.
    ///   The rewriters of the enclosing body must leave them to their own state machine, as they do nested lambdas.
    /// </summary>
    [TestFixture]
    public class InnerMethodTests {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

        private static Expression Value(object value) => Expression.Constant(value, typeof(object));

        // async { return await Task.FromResult(42); }, nested directly or in a lambda
        private static Expression InnerAsync(bool inLambda) {
            Expression inner = AstUtils.Async("inner", AstUtils.Await(Expression.Constant(Task.FromResult<object>(42), typeof(Task))));
            return inLambda ? Expression.Invoke(Expression.Lambda<Func<Task<object>>>(inner)) : inner;
        }

        // The lambda compiled, then interpreted.
        private static IEnumerable<T> Compile<T>(Expression<T> lambda) {
            yield return lambda.Compile();
            yield return lambda.LightCompile();
        }

        private static T Wait<T>(Task<T> task) {
            Assert.That(task.Wait(s_timeout), "the body didn't finish");
            return task.Result;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AsyncAwaitsNestedAsync(bool inLambda) {
            var lambda = Expression.Lambda<Func<Task<object>>>(AstUtils.Async("outer", AstUtils.Await(InnerAsync(inLambda))));

            foreach (var outer in Compile(lambda)) {
                Assert.That(Wait(outer()), Is.EqualTo(42));
            }
        }

#if NET
        [TestCase(false)]
        [TestCase(true)]
        public void AsyncEnumerableAwaitsNestedAsync(bool inLambda) {
            var label = Expression.Label(typeof(object), "yield");
            var node = AstUtils.AsyncEnumerable("outer", AstUtils.YieldReturn(label, AstUtils.Await(InnerAsync(inLambda))), label);

            foreach (var outer in Compile(Expression.Lambda<Func<IAsyncEnumerable<object>>>(node))) {
                var enumerator = outer().GetAsyncEnumerator();
                Assert.That(Wait(enumerator.MoveNextAsync().AsTask()));
                Assert.That(enumerator.Current, Is.EqualTo(42));
                Assert.That(Wait(enumerator.MoveNextAsync().AsTask()), Is.False);
            }
        }
#endif

        [Test]
        public void GeneratorYieldsNestedGenerator() {
            var innerLabel = Expression.Label(typeof(object), "inner");
            var inner = Expression.Convert(
                AstUtils.Generator(innerLabel, Expression.Block(typeof(void), AstUtils.YieldReturn(innerLabel, Value(1)), AstUtils.YieldReturn(innerLabel, Value(2)))),
                typeof(object));
            var label = Expression.Label(typeof(object), "outer");
            var node = AstUtils.Generator(label, Expression.Block(typeof(void), AstUtils.YieldReturn(label, inner), AstUtils.YieldReturn(label, inner)));

            foreach (var outer in Compile(Expression.Lambda<Func<IEnumerable<object>>>(node))) {
                // two enumerables of the inner generator, which don't share their state
                var enumerators = outer().Select(x => ((IEnumerable<object>)x).GetEnumerator()).ToArray();
                Assert.That(enumerators, Has.Length.EqualTo(2));
                foreach (int expected in new[] { 1, 2 }) {
                    foreach (var enumerator in enumerators) {
                        Assert.That(enumerator.MoveNext());
                        Assert.That(enumerator.Current, Is.EqualTo(expected));
                    }
                }
            }
        }

        [Test]
        public void DebugTransformNestedGenerator() {
            var document = Expression.SymbolDocument("test");
            var label = Expression.Label(typeof(object), "yield");
            var inner = AstUtils.Generator(label, Expression.Block(typeof(void), Expression.DebugInfo(document, 2, 1, 2, 10), AstUtils.YieldReturn(label, Value(1))));
            var lambda = Expression.Lambda<Func<IEnumerable<object>>>(Expression.Block(Expression.DebugInfo(document, 1, 1, 1, 10), inner));

            var transformed = (Expression<Func<IEnumerable<object>>>)DebugContext.CreateInstance().TransformLambda(lambda);

            foreach (var outer in Compile(transformed)) {
                Assert.That(outer(), Is.EqualTo(new object[] { 1 }));
            }
        }
    }
}
