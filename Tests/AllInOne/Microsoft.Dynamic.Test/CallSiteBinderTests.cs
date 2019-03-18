using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AllInOne.Try {
    [TestClass]
    public class CallSiteBinderTests {
        public class BinaryCallSiteBinder : BinaryOperationBinder {
            public BinaryCallSiteBinder()
                : base(ExpressionType.Add) {
            }

            public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion) {

                return new DynamicMetaObject(
                    Expression.Convert(
                    Expression.Add(
                        Expression.Convert(target.Expression, typeof(int)),
                        Expression.Convert(arg.Expression, typeof(int))
                    ), typeof(object)),

                    BindingRestrictions.GetTypeRestriction(target.Expression, typeof(int)).Merge(
                        BindingRestrictions.GetTypeRestriction(arg.Expression, typeof(int))
                    ));
            }
        }

        [TestMethod]
        public void Method() {
            var expr = DynamicExpression.Dynamic(new BinaryCallSiteBinder(), typeof(object), Expression.Constant(40, typeof(object)), Expression.Constant(2, typeof(object)));
            var f = Expression.Lambda<Func<object>>(expr);
            var f2 = f.Compile();
            Assert.AreEqual(42, f2());
        }
    }
}
