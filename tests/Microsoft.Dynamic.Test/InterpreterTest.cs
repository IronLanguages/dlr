using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

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

        public struct MutableStruct {
            public int Field;
        }

        [Test]
        public void ValueTypeFieldAssignmentTest() {
            // assigning a field of a converted (unboxed) value type updates a copy, not the boxed value
            var obj = Expression.Parameter(typeof(object), "obj");
            var lambda = Expression.Lambda<Func<object, int>>(
                Expression.Block(
                    Expression.Assign(Expression.Field(Expression.Convert(obj, typeof(MutableStruct)), nameof(MutableStruct.Field)), Expression.Constant(42)),
                    Expression.Field(Expression.Convert(obj, typeof(MutableStruct)), nameof(MutableStruct.Field))
                ),
                obj
            );

            object interpretedBox = new MutableStruct { Field = 1 };
            object compiledBox = new MutableStruct { Field = 1 };
            Assert.That(Scripting.Generation.CompilerHelpers.LightCompile(lambda)(interpretedBox), Is.EqualTo(lambda.Compile()(compiledBox)));
            Assert.That(((MutableStruct)interpretedBox).Field, Is.EqualTo(((MutableStruct)compiledBox).Field));
        }

        [Test]
        public void UnboxedValueTypeFieldAssignmentTest() {
            // assigning a field through Unbox updates the boxed value
            var obj = Expression.Parameter(typeof(object), "obj");
            var lambda = Expression.Lambda<Action<object>>(
                Expression.Assign(Expression.Field(Expression.Unbox(obj, typeof(MutableStruct)), nameof(MutableStruct.Field)), Expression.Constant(42)),
                obj
            );

            object interpretedBox = new MutableStruct { Field = 1 };
            object compiledBox = new MutableStruct { Field = 1 };
            Scripting.Generation.CompilerHelpers.LightCompile(lambda)(interpretedBox);
            lambda.Compile()(compiledBox);
            Assert.That(((MutableStruct)interpretedBox).Field, Is.EqualTo(((MutableStruct)compiledBox).Field));
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

        #region Exception handling

        // enough to detect a growing stack, few enough not to overflow it
        private const int LoopIterations = 20;

        public static int StackDepth() => new StackTrace().FrameCount;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowFromHelper(string message) => throw new InvalidOperationException(message);

        private static Expression Throw<T>(string message) where T : Exception {
            return Expression.Throw(Expression.New(typeof(T).GetConstructor([typeof(string)]), Expression.Constant(message)));
        }

        private static Expression Append(ParameterExpression log, Expression value) {
            return Expression.Block(typeof(void),
                Expression.Assign(log, Expression.Call(typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)]), log, value))
            );
        }

        private static Expression Append(ParameterExpression log, string value) => Append(log, Expression.Constant(value));

        private static Expression Message(ParameterExpression exception) => Expression.Property(exception, nameof(Exception.Message));

        // for (int i = 0; i < LoopIterations; i++) { body }
        private static Expression Loop(Func<LabelTarget, Expression> body) {
            var i = Expression.Variable(typeof(int), "i");
            var breakLabel = Expression.Label("break");
            var continueLabel = Expression.Label("continue");
            return Expression.Block([i],
                Expression.Assign(i, Expression.Constant(0)),
                Expression.Loop(
                    Expression.Block(
                        Expression.IfThen(Expression.GreaterThanOrEqual(i, Expression.Constant(LoopIterations)), Expression.Break(breakLabel)),
                        Expression.Assign(i, Expression.Add(i, Expression.Constant(1))),
                        body(continueLabel)
                    ),
                    breakLabel,
                    continueLabel
                )
            );
        }

        private static string Outcome(Func<string> f) {
            try {
                return f();
            } catch (Exception e) {
                return $"{e.GetType().Name}: {e.Message}";
            }
        }

        // The lambda returns the log the body appends to, unless an exception escapes.
        private static void AssertOutcome(string expected, Func<ParameterExpression, Expression> body) {
            var log = Expression.Variable(typeof(string), "log");
            var lambda = Expression.Lambda<Func<string>>(
                Expression.Block(typeof(string), [log], Expression.Assign(log, Expression.Constant("")), body(log), log)
            );

            Assert.That(Outcome(Scripting.Generation.CompilerHelpers.LightCompile(lambda, int.MaxValue)), Is.EqualTo(expected), "interpreted");
            Assert.That(Outcome(lambda.Compile()), Is.EqualTo(expected), "compiled");
        }

        private static Expression RecordStackDepth(List<int> depths) {
            return Expression.Call(
                Expression.Constant(depths),
                typeof(List<int>).GetMethod(nameof(List<int>.Add)),
                Expression.Call(typeof(InterpreterTest).GetMethod(nameof(StackDepth)))
            );
        }

        public enum CatchExit { Continue, Break, ContinueThroughFinally, ContinueFromNestedCatch, ContinueAfterFault }

        [Test]
        public void JumpOutOfCatchTest([Values] CatchExit exit) {
            // Leaving a catch handler by a jump must not leave the interpreter running inside an unfinished CLR catch block,
            // which grows the CLR stack on every iteration (https://github.com/IronLanguages/ironpython3/issues/1182).
            var depths = new List<int>();
            var record = RecordStackDepth(depths);

            var lambda = Expression.Lambda<Action>(Loop(continueLabel => {
                var exitCatch = Expression.Block(record, Expression.Continue(continueLabel));
                switch (exit) {
                    case CatchExit.Continue:
                        return Expression.TryCatch(
                            Throw<InvalidOperationException>("A"),
                            Expression.Catch(typeof(InvalidOperationException), exitCatch)
                        );
                    case CatchExit.Break:
                        var breakLabel = Expression.Label("innerBreak");
                        return Expression.Loop(
                            Expression.TryCatch(
                                Throw<InvalidOperationException>("A"),
                                Expression.Catch(typeof(InvalidOperationException), Expression.Block(record, Expression.Break(breakLabel)))
                            ),
                            breakLabel
                        );
                    case CatchExit.ContinueThroughFinally:
                        return Expression.TryFinally(
                            Expression.TryCatch(
                                Throw<InvalidOperationException>("A"),
                                Expression.Catch(typeof(InvalidOperationException), exitCatch)
                            ),
                            record
                        );
                    case CatchExit.ContinueFromNestedCatch:
                        return Expression.TryCatch(
                            Throw<InvalidOperationException>("A"),
                            Expression.Catch(typeof(InvalidOperationException), Expression.TryCatch(
                                Throw<ArgumentException>("B"),
                                Expression.Catch(typeof(ArgumentException), exitCatch)
                            ))
                        );
                    case CatchExit.ContinueAfterFault:
                        // catch (Exception) { ...; rethrow; } is interpreted as a fault block
                        return Expression.TryCatch(
                            Expression.TryCatch(
                                Throw<InvalidOperationException>("A"),
                                Expression.Catch(typeof(Exception), Expression.Block(record, Expression.Rethrow()))
                            ),
                            Expression.Catch(typeof(InvalidOperationException), exitCatch)
                        );
                    default:
                        throw new ArgumentOutOfRangeException(nameof(exit));
                }
            }));

            Scripting.Generation.CompilerHelpers.LightCompile(lambda, int.MaxValue)();

            int recordsPerIteration = exit is CatchExit.ContinueThroughFinally or CatchExit.ContinueAfterFault ? 2 : 1;
            Assert.That(depths, Has.Count.EqualTo(recordsPerIteration * LoopIterations));
            Assert.That(depths.Distinct(), Has.Exactly(1).Items, $"CLR stack depth changed: {string.Join(", ", depths)}");
        }

        public enum FinallyExit { Continue, Throw }

        [Test]
        public void ExceptionCancelledByFinallyTest([Values] FinallyExit exit) {
            // A finally block running while an exception without a handler in the frame propagates can cancel the exception,
            // by a jump out of the block or by throwing an exception handled in the frame. The method then goes on and must not
            // be left running inside an unfinished CLR catch block.
            var depths = new List<int>();
            var record = RecordStackDepth(depths);

            var lambda = Expression.Lambda<Action>(Loop(continueLabel => exit switch {
                // the interpreter allows jumps out of finally blocks (see FinallyFlowControlExpression)
                FinallyExit.Continue => Expression.TryFinally(
                    Throw<InvalidOperationException>("A"),
                    Expression.Block(record, Expression.Continue(continueLabel))
                ),
                FinallyExit.Throw => Expression.TryCatch(
                    Expression.TryFinally(Throw<InvalidOperationException>("A"), Expression.Block(record, Throw<ArgumentException>("B"))),
                    Expression.Catch(typeof(ArgumentException), Expression.Empty())
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(exit)),
            }));

            Scripting.Generation.CompilerHelpers.LightCompile(lambda, int.MaxValue)();

            Assert.That(depths, Has.Count.EqualTo(LoopIterations));
            Assert.That(depths.Distinct(), Has.Exactly(1).Items, $"CLR stack depth changed: {string.Join(", ", depths)}");
        }

        [Test]
        public void PropagateThroughFinallyTest() {
            // the exception leaves an interpreted lambda that has finally blocks but no handler
            var outer = Expression.Variable(typeof(InvalidOperationException), "outer");
            AssertOutcome("f1f2A", log => Expression.TryCatch(
                Expression.Invoke(Expression.Lambda<Action>(
                    Expression.TryFinally(
                        Expression.TryFinally(Throw<InvalidOperationException>("A"), Append(log, "f1")),
                        Append(log, "f2")
                    )
                )),
                Expression.Catch(outer, Append(log, Message(outer)))
            ));
        }

        [Test]
        public void ThrowFromFinallyTest() {
            // an exception thrown by a finally block replaces the propagating exception
            AssertOutcome("ArgumentException: B", log => Expression.TryFinally(
                Throw<InvalidOperationException>("A"),
                Throw<ArgumentException>("B")
            ));
        }

        [Test]
        public void JumpOutOfFinallyTest() {
            // A jump out of a finally block cancels the propagating exception.
            // FinallyFlowControl rewrites the jump for the compiler, which doesn't allow jumps out of finally blocks.
            Expression CancelledThrow(ParameterExpression log) {
                var after = Expression.Label("after");
                return Scripting.Ast.Utils.FinallyFlowControl(Expression.Block(
                    Expression.TryFinally(Throw<InvalidOperationException>("A"), Expression.Block(Append(log, "f"), Expression.Goto(after))),
                    Expression.Label(after),
                    Append(log, "a")
                ));
            }

            AssertOutcome("fa", CancelledThrow);

            // the cancelled exception doesn't resurface when a later exception propagates
            AssertOutcome("NotSupportedException: C", log => Expression.Block(
                CancelledThrow(log),
                Throw<NotSupportedException>("C")
            ));
        }

        [Test]
        public void RethrowFromCatchTest() {
            var e = Expression.Variable(typeof(InvalidOperationException), "e");
            AssertOutcome("InvalidOperationException: A", log => Expression.TryCatch(
                Throw<InvalidOperationException>("A"),
                Expression.Catch(e, Expression.Rethrow())
            ));
        }

        [Test]
        public void RethrowAfterNestedCatchTest() {
            // the exception of the enclosing handler is rethrown, not the last exception caught
            var e = Expression.Variable(typeof(InvalidOperationException), "e");
            AssertOutcome("InvalidOperationException: A", log => Expression.TryCatch(
                Throw<InvalidOperationException>("A"),
                Expression.Catch(e, Expression.Block(
                    Expression.TryCatch(
                        Throw<ArgumentException>("B"),
                        Expression.Catch(typeof(ArgumentException), Append(log, "b"))
                    ),
                    Expression.Rethrow()
                ))
            ));
        }

        [Test]
        public void RethrowThroughFinallyTest() {
            // the exception is rethrown out of an interpreted lambda with a finally block on the way
            var e = Expression.Variable(typeof(InvalidOperationException), "e");
            var outer = Expression.Variable(typeof(InvalidOperationException), "outer");
            AssertOutcome("cfA", log => Expression.TryCatch(
                Expression.Invoke(Expression.Lambda<Action>(
                    Expression.TryFinally(
                        Expression.TryCatch(
                            Throw<InvalidOperationException>("A"),
                            Expression.Catch(e, Expression.Block(Append(log, "c"), Expression.Rethrow()))
                        ),
                        Append(log, "f")
                    )
                )),
                Expression.Catch(outer, Append(log, Message(outer)))
            ));
        }

        [Test]
        public void RethrowToOuterHandlerThroughFinallyTest() {
            // the exception is rethrown to an enclosing handler in the same lambda with a finally block on the way
            var e = Expression.Variable(typeof(InvalidOperationException), "e");
            var outer = Expression.Variable(typeof(InvalidOperationException), "outer");
            AssertOutcome("cfA", log => Expression.TryCatch(
                Expression.TryFinally(
                    Expression.TryCatch(
                        Throw<InvalidOperationException>("A"),
                        Expression.Catch(e, Expression.Block(Append(log, "c"), Expression.Rethrow()))
                    ),
                    Append(log, "f")
                ),
                Expression.Catch(outer, Append(log, Message(outer)))
            ));
        }

        [Test]
        public void ExceptionFromFinallyReplacesRethrowTest() {
            // an exception thrown by a finally block cancels the rethrow in progress
            Expression ReplacedRethrow(ParameterExpression log) {
                var e = Expression.Variable(typeof(InvalidOperationException), "e");
                var b = Expression.Variable(typeof(ArgumentException), "b");
                return Expression.TryCatch(
                    Expression.TryFinally(
                        Expression.TryCatch(Throw<InvalidOperationException>("A"), Expression.Catch(e, Expression.Rethrow())),
                        Throw<ArgumentException>("B")
                    ),
                    Expression.Catch(b, Append(log, Message(b)))
                );
            }

            AssertOutcome("B", ReplacedRethrow);

            // the cancelled rethrow doesn't resurface when a later exception propagates
            AssertOutcome("NotSupportedException: C", log => Expression.Block(
                ReplacedRethrow(log),
                Expression.TryFinally(Throw<NotSupportedException>("C"), Append(log, "f"))
            ));
        }

        [Test]
        public void ThrowFromCatchTest() {
            AssertOutcome("ArgumentException: B", log => Expression.TryCatch(
                Throw<InvalidOperationException>("A"),
                Expression.Catch(typeof(InvalidOperationException), Throw<ArgumentException>("B"))
            ));
        }

        [Test]
        public void FaultTest() {
            // catch (Exception) { ...; rethrow; } is interpreted as a fault block
            Expression Fault(ParameterExpression log) => Expression.TryCatch(
                Throw<InvalidOperationException>("A"),
                Expression.Catch(typeof(Exception), Expression.Block(Append(log, "f"), Expression.Rethrow()))
            );

            AssertOutcome("InvalidOperationException: A", Fault);

            var outer = Expression.Variable(typeof(InvalidOperationException), "outer");
            AssertOutcome("fA", log => Expression.TryCatch(Fault(log), Expression.Catch(outer, Append(log, Message(outer)))));
        }

        [Test]
        public void CatchWithValueTest() {
            AssertOutcome("caught", log => Expression.Assign(log, Expression.TryCatch(
                Expression.Block(Throw<InvalidOperationException>("A"), Expression.Constant("try")),
                Expression.Catch(typeof(InvalidOperationException), Expression.Constant("caught"))
            )));

            // the handler leaves the loop with a value
            var breakLabel = Expression.Label(typeof(string), "break");
            AssertOutcome("broken", log => Expression.Assign(log, Expression.Loop(
                Expression.TryCatch(
                    Expression.Block(Throw<InvalidOperationException>("A"), Expression.Constant("try")),
                    Expression.Catch(typeof(InvalidOperationException), Expression.Break(breakLabel, Expression.Constant("broken"), typeof(string)))
                ),
                breakLabel
            )));
        }

        [Test]
        public void RethrowPreservesStackTraceTest() {
            Expression CatchAndRethrow(Expression body) {
                var e = Expression.Variable(typeof(InvalidOperationException), "e");
                return Expression.TryCatch(body, Expression.Catch(e, Expression.Rethrow()));
            }

            // rethrown by interpreted lambdas with a catch handler, with a finally block, and with neither
            var lambda = Expression.Lambda<Action>(CatchAndRethrow(Expression.Invoke(Expression.Lambda<Action>(
                Expression.TryFinally(
                    Expression.Invoke(Expression.Lambda<Action>(
                        Expression.Call(typeof(InterpreterTest).GetMethod(nameof(ThrowFromHelper)), Expression.Constant("A"))
                    )),
                    Expression.Empty()
                )
            ))));
            var interpreted = Scripting.Generation.CompilerHelpers.LightCompile(lambda, int.MaxValue);

            var exception = Assert.Throws<InvalidOperationException>(() => interpreted());
            Assert.That(exception.StackTrace, Does.Contain(nameof(ThrowFromHelper)));
        }

#if NETFRAMEWORK
        // signals that the thread can be aborted and waits for it
        private static Expression SignalAndSleep(ManualResetEventSlim started) {
            return Expression.Block(
                Expression.Call(Expression.Constant(started), typeof(ManualResetEventSlim).GetMethod(nameof(ManualResetEventSlim.Set))),
                Expression.Call(typeof(Thread).GetMethod(nameof(Thread.Sleep), [typeof(int)]), Expression.Constant(Timeout.Infinite))
            );
        }

        private static Expression CatchThreadAbort(Expression body) {
            return Expression.TryCatch(
                Expression.Block(body, Expression.Constant("not aborted")),
                Expression.Catch(typeof(ThreadAbortException), Expression.Block(
                    Expression.Call(typeof(Thread).GetMethod(nameof(Thread.ResetAbort))),
                    Expression.Constant("caught")
                ))
            );
        }

        private static string RunAndAbort(Expression<Func<string>> lambda, ManualResetEventSlim started) {
            var interpreted = Scripting.Generation.CompilerHelpers.LightCompile(lambda, int.MaxValue);

            string result = null;
            var thread = new Thread(() => result = interpreted());
            thread.Start();
            started.Wait();
            thread.Abort();
            thread.Join();
            return result;
        }

        [Test]
        public void ThreadAbortTest() {
            // The CLR rethrows ThreadAbortException at the end of a catch block unless the abort is reset,
            // so the interpreted handler has to run inside the CLR catch block.
            using var started = new ManualResetEventSlim();
            var lambda = Expression.Lambda<Func<string>>(CatchThreadAbort(SignalAndSleep(started)));

            Assert.That(RunAndAbort(lambda, started), Is.EqualTo("caught"));
        }

        [Test]
        public void ThreadAbortFinallyTest() {
            // Finally blocks run while ThreadAbortException propagates have to run inside the CLR catch block too,
            // or the CLR rethrows the exception before they run.
            using var started = new ManualResetEventSlim();
            var log = new List<string>();
            var lambda = Expression.Lambda<Func<string>>(CatchThreadAbort(
                // a lambda without a handler for the exception
                Expression.Invoke(Expression.Lambda<Action>(Expression.TryFinally(
                    SignalAndSleep(started),
                    Expression.Call(Expression.Constant(log), typeof(List<string>).GetMethod(nameof(List<string>.Add)), Expression.Constant("finally"))
                )))
            ));

            Assert.That(RunAndAbort(lambda, started), Is.EqualTo("caught"));
            Assert.That(log, Is.EqualTo(new[] { "finally" }));
        }
#endif

        #endregion
    }
}
