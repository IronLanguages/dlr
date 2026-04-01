using System;

using NUnit.Framework;

namespace Microsoft.Scripting.Test.Utils
{
    [TestFixture]
    public class ArrayUtilsTest {

        [Test]
        public void RotateRightTest() {
            int[] array = null;
            Assert.Throws<ArgumentNullException>(
                () => { Scripting.Utils.ArrayUtils.RotateRight(array, 1); });
            array = new[] { 1, 2, 3 };
            Assert.AreEqual(new int[] { 3, 1, 2 }, Scripting.Utils.ArrayUtils.RotateRight(array, 1));
        }

        [Test]
        public void ShiftRightTest() {
            int[] array = null;
            Assert.Throws<ArgumentNullException>(
                () => { Scripting.Utils.ArrayUtils.ShiftRight(array, 1); });
            array = new[] { 1, 2, 3 };
            Assert.AreEqual(new int[] { 0, 1, 2, 3 }, Scripting.Utils.ArrayUtils.ShiftRight(array, 1));
        }

        [Test]
        public void ShiftLeftTest() {
            int[] array = null;
            Assert.Throws<ArgumentNullException>(
                () => { Scripting.Utils.ArrayUtils.ShiftLeft(array, 1); });
            array = new[] { 1, 2, 3 };
            Assert.AreEqual(new int[] { 2, 3 }, Scripting.Utils.ArrayUtils.ShiftLeft(array, 1));
        }

        [Test]
        public void ReverseTest() {
            int[] array = null;
            Assert.Throws<NullReferenceException>(
                () => { Scripting.Utils.ArrayUtils.Reverse(array); });
            array = new[] { 1, 2, 3 };
            Assert.AreEqual(new int[] { 3, 2, 1 }, Scripting.Utils.ArrayUtils.Reverse(array));
        }
    }
}
