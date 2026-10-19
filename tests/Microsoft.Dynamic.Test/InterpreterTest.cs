using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Dynamic.Test {
    [TestFixture]
    public class InterpreterTest {
        public class TestGc {
            static int cnt = 0;

            public static void CheckCount() {
                GC.Collect();
                GC.Collect(); // make Mono happy
                GC.WaitForPendingFinalizers();
                Assert.Zero(cnt);
            }

            public TestGc() {
                cnt += 1;
            }

            ~TestGc() {
                cnt -= 1;
            }
        }

        [Test]
        public void PopTest() {
            // Equal gets converted to an EqualReference instruction which does two pops and a push.
            // If pop doesn't clear the stack one of the two TestGc objects should remain alive.
            var lambda = Expression.Lambda(Expression.Block(
                Expression.Equal(Expression.New(typeof(TestGc).GetConstructor(Array.Empty<Type>())), Expression.New(typeof(TestGc).GetConstructor(Array.Empty<Type>()))),
                Expression.Call(typeof(TestGc).GetMethod(nameof(TestGc.CheckCount)))
            ));

            Scripting.Generation.CompilerHelpers.LightCompile(lambda).DynamicInvoke();
        }

        [TestCase(typeof(short))]
        [TestCase(typeof(ushort))]
        [TestCase(typeof(int))]
        [TestCase(typeof(uint))]
        [TestCase(typeof(long))]
        [TestCase(typeof(ulong))]
        public void EqualityTest(Type type) {
            var x = Expression.Parameter(type, "x");
            var y = Expression.Parameter(type, "y");
            var one = Convert.ChangeType(1, type);
            var two = Convert.ChangeType(2, type);

            foreach (var nodeType in new[] { ExpressionType.Equal, ExpressionType.NotEqual }) {
                var lambda = Expression.Lambda(Expression.MakeBinary(nodeType, x, y), x, y);
                var interpreted = Scripting.Generation.CompilerHelpers.LightCompile(lambda);
                var compiled = lambda.Compile();

                Assert.That(interpreted.DynamicInvoke(one, one), Is.EqualTo(compiled.DynamicInvoke(one, one)));
                Assert.That(interpreted.DynamicInvoke(one, two), Is.EqualTo(compiled.DynamicInvoke(one, two)));
            }
        }

        public enum TestEnum : ushort { A, B }

        [TestCase(typeof(int), false)]
        [TestCase(typeof(int), true)]
        [TestCase(typeof(uint), false)]
        [TestCase(typeof(uint), true)]
        [TestCase(typeof(long), false)]
        [TestCase(typeof(ulong), true)]
        [TestCase(typeof(bool), false)]
        [TestCase(typeof(bool), true)]
        [TestCase(typeof(double), false)]
        [TestCase(typeof(double), true)]
        [TestCase(typeof(char), false)]
        [TestCase(typeof(TestEnum), false)]
        [TestCase(typeof(TestEnum), true)]
        public void NullableEqualityTest(Type type, bool liftToNull) {
            var nullableType = typeof(Nullable<>).MakeGenericType(type);
            var x = Expression.Parameter(nullableType, "x");
            var y = Expression.Parameter(nullableType, "y");
            object one = type.IsEnum ? Enum.ToObject(type, 1) : Convert.ChangeType(1, type);
            object two = type.IsEnum ? Enum.ToObject(type, 0) : Convert.ChangeType(type == typeof(bool) ? 0 : 2, type);
            var values = new[] { (null, null), (null, one), (one, null), (one, one), (one, two) };

            foreach (var lambda in new[] {
                Expression.Lambda(Expression.Equal(x, y, liftToNull, null), x, y),
                Expression.Lambda(Expression.NotEqual(x, y, liftToNull, null), x, y),
            }) {
                var interpreted = Scripting.Generation.CompilerHelpers.LightCompile(lambda);
                var compiled = lambda.Compile();

                foreach (var (left, right) in values) {
                    Assert.That(interpreted.DynamicInvoke(left, right), Is.EqualTo(compiled.DynamicInvoke(left, right)), $"{lambda.Body.NodeType}({left ?? "null"}, {right ?? "null"})");
                }
            }
        }

        [Test]
        public void NullComparisonTest() {
            var x = Expression.Parameter(typeof(int?), "x");

            foreach (var lambda in new[] {
                Expression.Lambda<Func<int?, bool>>(Expression.Equal(x, Expression.Constant(null)), x),
                Expression.Lambda<Func<int?, bool>>(Expression.NotEqual(Expression.Constant(null), x), x),
            }) {
                var interpreted = Scripting.Generation.CompilerHelpers.LightCompile(lambda);
                var compiled = lambda.Compile();

                Assert.That(interpreted(null), Is.EqualTo(compiled(null)));
                Assert.That(interpreted(1), Is.EqualTo(compiled(1)));
            }
        }

        private static readonly Type[] PrimitiveTypes = {
            typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
        };

        private static object[] SampleValues(Type type) {
            if (type == typeof(bool)) return new object[] { false, true };
            if (type == typeof(char)) return new object[] { '\0', 'a', 'b', char.MaxValue };
            if (type == typeof(float)) return new object[] { 0f, 1f, 2.5f, -7f, float.MaxValue, float.NaN };
            if (type == typeof(double)) return new object[] { 0d, 1d, 2.5d, -7d, double.MaxValue, double.NaN };

            var values = new List<object> {
                type.GetField("MinValue").GetValue(null),
                type.GetField("MaxValue").GetValue(null),
            };
            foreach (var value in new[] { 0, 1, 2, 7, -1, -3 }) {
                try {
                    values.Add(Convert.ChangeType(value, type));
                } catch (OverflowException) { }
            }
            return values.ToArray();
        }

        private static object InvokeOrException(Delegate d, object[] args) {
            try {
                return d.DynamicInvoke(args);
            } catch (TargetInvocationException ex) {
                return ex.InnerException.GetType();
            }
        }

        private static void AssertInterpretedMatchesCompiled(LambdaExpression lambda, IEnumerable<object[]> argSets) {
            // never switch to the compiled lambda
            var interpreted = Scripting.Generation.CompilerHelpers.LightCompile(lambda, int.MaxValue);
            var compiled = lambda.Compile();

            foreach (var args in argSets) {
                var expected = InvokeOrException(compiled, args);
                var actual = InvokeOrException(interpreted, args);
                Assert.That(Equals(actual, expected), Is.True, $"{lambda.Body}({string.Join(", ", Array.ConvertAll(args, a => a ?? "null"))}): expected {expected ?? "null"} but was {actual ?? "null"}");
            }
        }

        private static IEnumerable<object[]> Pairs(object[] left, object[] right) {
            foreach (var l in left) {
                foreach (var r in right) {
                    yield return new[] { l, r };
                }
            }
        }

        private static object[] WithNull(object[] values) {
            var res = new object[values.Length + 1];
            values.CopyTo(res, 1);
            return res;
        }

        [Test]
        public void BinaryOperatorTest([Values(
            ExpressionType.Add, ExpressionType.AddChecked, ExpressionType.Subtract, ExpressionType.SubtractChecked,
            ExpressionType.Multiply, ExpressionType.MultiplyChecked, ExpressionType.Divide, ExpressionType.Modulo,
            ExpressionType.And, ExpressionType.Or, ExpressionType.ExclusiveOr,
            ExpressionType.LessThan, ExpressionType.LessThanOrEqual, ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual
        )] ExpressionType nodeType, [Values] bool nullable) {
            int tested = 0;
            foreach (var type in PrimitiveTypes) {
                var paramType = nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
                var x = Expression.Parameter(paramType, "x");
                var y = Expression.Parameter(paramType, "y");
                var values = nullable ? WithNull(SampleValues(type)) : SampleValues(type);

                BinaryExpression body;
                try {
                    body = Expression.MakeBinary(nodeType, x, y);
                } catch (InvalidOperationException) {
                    continue; // operator not defined for this type
                }

                AssertInterpretedMatchesCompiled(Expression.Lambda(body, x, y), Pairs(values, values));
                if (nullable && body.Type == typeof(bool)) {
                    // comparisons lifted to null
                    AssertInterpretedMatchesCompiled(Expression.Lambda(Expression.MakeBinary(nodeType, x, y, liftToNull: true, method: null), x, y), Pairs(values, values));
                }
                tested++;
            }
            Assert.That(tested, Is.GreaterThan(0));
        }

        [Test]
        public void ShiftTest([Values(ExpressionType.LeftShift, ExpressionType.RightShift)] ExpressionType nodeType, [Values] bool nullable) {
            int tested = 0;
            var shifts = new object[] { 0, 1, 7, 31, 32, 63, 65, -1 };
            foreach (var type in PrimitiveTypes) {
                var x = Expression.Parameter(nullable ? typeof(Nullable<>).MakeGenericType(type) : type, "x");
                var y = Expression.Parameter(nullable ? typeof(int?) : typeof(int), "y");

                BinaryExpression body;
                try {
                    body = Expression.MakeBinary(nodeType, x, y);
                } catch (InvalidOperationException) {
                    continue; // operator not defined for this type
                }

                var values = SampleValues(type);
                AssertInterpretedMatchesCompiled(Expression.Lambda(body, x, y), nullable ? Pairs(WithNull(values), WithNull(shifts)) : Pairs(values, shifts));
                tested++;
            }
            Assert.That(tested, Is.GreaterThan(0));
        }

        [Test]
        public void UnaryOperatorTest([Values(
            ExpressionType.Negate, ExpressionType.NegateChecked, ExpressionType.Not, ExpressionType.OnesComplement,
            ExpressionType.Increment, ExpressionType.Decrement, ExpressionType.UnaryPlus
        )] ExpressionType nodeType, [Values] bool nullable) {
            int tested = 0;
            foreach (var type in PrimitiveTypes) {
                var x = Expression.Parameter(nullable ? typeof(Nullable<>).MakeGenericType(type) : type, "x");

                UnaryExpression body;
                try {
                    body = Expression.MakeUnary(nodeType, x, null);
                } catch (InvalidOperationException) {
                    continue; // operator not defined for this type
                }

                var values = nullable ? WithNull(SampleValues(type)) : SampleValues(type);
                AssertInterpretedMatchesCompiled(Expression.Lambda(body, x), Array.ConvertAll(values, v => new[] { v }));
                tested++;
            }
            Assert.That(tested, Is.GreaterThan(0));
        }

        [TestCase(ExpressionType.IsTrue, true)]
        [TestCase(ExpressionType.IsTrue, false)]
        [TestCase(ExpressionType.IsFalse, true)]
        [TestCase(ExpressionType.IsFalse, false)]
        public void UnaryBoolCheckTest(ExpressionType nodeType, bool value) {
            var x = Expression.Parameter(typeof(bool), "x");
            var lambda = Expression.Lambda<Func<bool, bool>>(Expression.MakeUnary(nodeType, x, null), x);

            Assert.That(Scripting.Generation.CompilerHelpers.LightCompile(lambda)(value), Is.EqualTo(lambda.Compile()(value)));
        }

        [TestCase(ExpressionType.IsTrue, true)]
        [TestCase(ExpressionType.IsTrue, false)]
        [TestCase(ExpressionType.IsTrue, null)]
        [TestCase(ExpressionType.IsFalse, true)]
        [TestCase(ExpressionType.IsFalse, false)]
        [TestCase(ExpressionType.IsFalse, null)]
        public void LiftedUnaryBoolCheckTest(ExpressionType nodeType, bool? value) {
            var x = Expression.Parameter(typeof(bool?), "x");
            var lambda = Expression.Lambda<Func<bool?, bool?>>(Expression.MakeUnary(nodeType, x, null), x);

            Assert.That(Scripting.Generation.CompilerHelpers.LightCompile(lambda)(value), Is.EqualTo(lambda.Compile()(value)));
        }
    }
}
