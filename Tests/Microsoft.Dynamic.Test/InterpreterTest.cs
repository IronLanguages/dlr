using NUnit.Framework;
using System;
using System.Linq.Expressions;

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
                Expression.Equal(Expression.New(typeof(TestGc).GetConstructor(new Type[0])), Expression.New(typeof(TestGc).GetConstructor(new Type[0]))),
                Expression.Call(typeof(TestGc).GetMethod(nameof(TestGc.CheckCount)))
            ));

            Scripting.Generation.CompilerHelpers.LightCompile(lambda).DynamicInvoke();
        }
    }
}
