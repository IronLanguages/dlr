using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Test]
        public void IndexAssignmentValueTest() {
            // the value of an index assignment is the assigned value
            var array = Expression.Parameter(typeof(int[]), "array");
            var arrayLambda = Expression.Lambda<Func<int[], int>>(
                Expression.Assign(Expression.ArrayAccess(array, Expression.Constant(1)), Expression.Constant(42)),
                array
            );

            var interpretedArray = new int[3];
            var compiledArray = new int[3];
            Assert.That(Scripting.Generation.CompilerHelpers.LightCompile(arrayLambda)(interpretedArray), Is.EqualTo(arrayLambda.Compile()(compiledArray)));
            Assert.That(interpretedArray, Is.EqualTo(compiledArray));

            var list = Expression.Parameter(typeof(List<int>), "list");
            var listLambda = Expression.Lambda<Func<List<int>, int>>(
                Expression.Assign(Expression.Property(list, "Item", Expression.Constant(1)), Expression.Constant(42)),
                list
            );

            var interpretedList = new List<int> { 0, 0, 0 };
            var compiledList = new List<int> { 0, 0, 0 };
            Assert.That(Scripting.Generation.CompilerHelpers.LightCompile(listLambda)(interpretedList), Is.EqualTo(listLambda.Compile()(compiledList)));
            Assert.That(interpretedList, Is.EqualTo(compiledList));
        }

        private static readonly Type[] PrimitiveTypes = {
            typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
        };

        private static object[] SampleValues(Type type) {
            if (type == typeof(bool)) return [false, true];
            if (type == typeof(char)) return ['\0', 'a', 'b', char.MaxValue];
            if (type == typeof(float)) return [0f, 1f, 2.5f, -7f, float.MaxValue, float.NaN];
            if (type == typeof(double)) return [0d, 1d, 2.5d, -7d, double.MaxValue, double.NaN];

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
            } catch (ArithmeticException ex) {
                // Mono doesn't wrap arithmetic exceptions thrown by dynamic methods
                return ex.GetType();
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
                    yield return [l, r];
                }
            }
        }

        [Test]
        public void BinaryOperatorTest([Values(
            ExpressionType.Add, ExpressionType.AddChecked, ExpressionType.Subtract, ExpressionType.SubtractChecked,
            ExpressionType.Multiply, ExpressionType.MultiplyChecked, ExpressionType.Divide, ExpressionType.Modulo,
            ExpressionType.And, ExpressionType.Or, ExpressionType.ExclusiveOr,
            ExpressionType.Equal, ExpressionType.NotEqual,
            ExpressionType.LessThan, ExpressionType.LessThanOrEqual, ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual
        )] ExpressionType nodeType, [Values] bool nullable) {
            int tested = 0;
            foreach (var type in PrimitiveTypes) {
                var paramType = nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
                var x = Expression.Parameter(paramType, "x");
                var y = Expression.Parameter(paramType, "y");
                object[] values = nullable ? [null, .. SampleValues(type)] : SampleValues(type);

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

        public enum TestEnum : ushort { A, B }

        [Test]
        public void NullableEnumEqualityTest([Values(ExpressionType.Equal, ExpressionType.NotEqual)] ExpressionType nodeType, [Values] bool liftToNull) {
            var x = Expression.Parameter(typeof(TestEnum?), "x");
            var y = Expression.Parameter(typeof(TestEnum?), "y");
            object[] values = [null, TestEnum.A, TestEnum.B];

            AssertInterpretedMatchesCompiled(Expression.Lambda(Expression.MakeBinary(nodeType, x, y, liftToNull, null), x, y), Pairs(values, values));
        }

        [Test]
        public void NullComparisonTest() {
            var x = Expression.Parameter(typeof(int?), "x");
            object[][] argSets = [[null], [1]];

            AssertInterpretedMatchesCompiled(Expression.Lambda(Expression.Equal(x, Expression.Constant(null)), x), argSets);
            AssertInterpretedMatchesCompiled(Expression.Lambda(Expression.NotEqual(Expression.Constant(null), x), x), argSets);
        }

        [Test]
        public void ShiftTest([Values(ExpressionType.LeftShift, ExpressionType.RightShift)] ExpressionType nodeType, [Values] bool nullable) {
            int tested = 0;
            object[] shifts = [0, 1, 7, 31, 32, 63, 65, -1];
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
                AssertInterpretedMatchesCompiled(Expression.Lambda(body, x, y), nullable ? Pairs([null, .. values], [null, .. shifts]) : Pairs(values, shifts));
                tested++;
            }
            Assert.That(tested, Is.GreaterThan(0));
        }

        [Test]
        public void UnaryOperatorTest([Values(
            ExpressionType.Negate, ExpressionType.NegateChecked, ExpressionType.Not, ExpressionType.OnesComplement,
            ExpressionType.Increment, ExpressionType.Decrement, ExpressionType.UnaryPlus,
            ExpressionType.IsTrue, ExpressionType.IsFalse
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

                object[] values = nullable ? [null, .. SampleValues(type)] : SampleValues(type);
                AssertInterpretedMatchesCompiled(Expression.Lambda(body, x), values.Select(v => new[] { v }));
                tested++;
            }
            Assert.That(tested, Is.GreaterThan(0));
        }
    }
}
