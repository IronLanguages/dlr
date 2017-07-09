using System;

using NUnit.Framework;

namespace Microsoft.Dynamic.Test.Utils
{
    [TestFixture]
    public class MathUtilsTest {

        [Test]
        public void FloorDivideUnchecked32Test() {
            Assert.Throws<DivideByZeroException>(
                () => { Scripting.Utils.MathUtils.FloorDivideUnchecked(0, 0); });
            Assert.Throws<DivideByZeroException>(
                () => { Scripting.Utils.MathUtils.FloorDivideUnchecked(1, 0); });

            Assert.AreEqual(0, Scripting.Utils.MathUtils.FloorDivideUnchecked(0, 2));
            Assert.AreEqual(0, Scripting.Utils.MathUtils.FloorDivideUnchecked(1, 2));
            Assert.AreEqual(-1, Scripting.Utils.MathUtils.FloorDivideUnchecked(1, -2));
            Assert.AreEqual(0, Scripting.Utils.MathUtils.FloorDivideUnchecked(-1, -2));
            Assert.AreEqual(-1, Scripting.Utils.MathUtils.FloorDivideUnchecked(-1, 2));
        }

        [Test]
        public void FloorDivideUnchecked64Test() {
            Assert.Throws<DivideByZeroException>(
                () => { Scripting.Utils.MathUtils.FloorDivideUnchecked(0u, 0u); });
            Assert.Throws<DivideByZeroException>(
                () => { Scripting.Utils.MathUtils.FloorDivideUnchecked(1u, 0u); });

            Assert.AreEqual(0u, Scripting.Utils.MathUtils.FloorDivideUnchecked(0u, 2u));
            Assert.AreEqual(0u, Scripting.Utils.MathUtils.FloorDivideUnchecked(1u, 2u));
            Assert.AreEqual(-1u, Scripting.Utils.MathUtils.FloorDivideUnchecked(1u, -2u));
            Assert.AreEqual(0u, Scripting.Utils.MathUtils.FloorDivideUnchecked(-1u, -2u));
            Assert.AreEqual(-1u, Scripting.Utils.MathUtils.FloorDivideUnchecked(-1u, 2u));
        }

        [TestCase(double.PositiveInfinity, 1, ExpectedResult = double.PositiveInfinity)]
        [TestCase(1, double.PositiveInfinity, ExpectedResult = double.PositiveInfinity)]
        [TestCase(1, 0, ExpectedResult = 1.0d)]
        [TestCase(0, 1, ExpectedResult = 1.0d)]
        [TestCase(0, 0, ExpectedResult = 0.0d)]
        [TestCase(1, 2, ExpectedResult = 2.2360679774997898d)]
        [TestCase(2, 1, ExpectedResult = 2.2360679774997898d)]
        [TestCase(-1, 2, ExpectedResult = 2.2360679774997898d)]
        public double HypotTest(double x, double y) {
           return Scripting.Utils.MathUtils.Hypot(x, y);
        }

        [TestCase(0, ExpectedResult = double.NaN)]
        [TestCase(double.MaxValue, ExpectedResult = double.PositiveInfinity)]
        [TestCase(double.MinValue, ExpectedResult = double.NaN)]
        [TestCase(1, ExpectedResult = 4.4408920985006262E-16d)]
        [TestCase(-1, ExpectedResult = double.NaN)]
        [TestCase(2.5599833278516301e+305, ExpectedResult = 1.7976931348623099E+308d)]
        [TestCase(2, ExpectedResult = 8.8817841970012523E-16d)]
        [TestCase(4, ExpectedResult = 1.7917594692280545d)]
        [TestCase(-1 / 2d, ExpectedResult = 1.265512123484646d)]
        [TestCase(1 / 2d, ExpectedResult = 0.57236494292470042d)]
        public double LogGammaTest(double v0) {
           return Scripting.Utils.MathUtils.LogGamma(v0);
        }
    }
}
