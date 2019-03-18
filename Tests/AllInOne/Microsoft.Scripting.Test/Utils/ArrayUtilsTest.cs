using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Microsoft.Scripting.Test.Utils
{
    [TestClass]
    public class ArrayUtilsTest {

        [TestMethod]
        public void RotateRightTest() {
            int[] array = null;
            Assert.ThrowsException<ArgumentNullException>(
                () => { Scripting.Utils.ArrayUtils.RotateRight(array, 1); });
            array = new[] { 1, 2, 3 };
            Assert.IsTrue(new int[] { 3, 1, 2 }.SequenceEqual(Scripting.Utils.ArrayUtils.RotateRight(array, 1)));
        }

        [TestMethod]
        public void ShiftRightTest() {
            int[] array = null;
            Assert.ThrowsException<ArgumentNullException>(
                () => { Scripting.Utils.ArrayUtils.ShiftRight(array, 1); });
            array = new[] { 1, 2, 3 };
            Assert.IsTrue(new int[] { 0, 1, 2, 3 }.SequenceEqual(Scripting.Utils.ArrayUtils.ShiftRight(array, 1)));
        }

        [TestMethod]
        public void ShiftLeftTest() {
            int[] array = null;
            Assert.ThrowsException<ArgumentNullException>(
                () => { Scripting.Utils.ArrayUtils.ShiftLeft(array, 1); });
            array = new[] { 1, 2, 3 };
            Assert.IsTrue(new int[] { 2, 3 }.SequenceEqual(Scripting.Utils.ArrayUtils.ShiftLeft(array, 1)));
        }

        [TestMethod]
        public void ReverseTest() {
            int[] array = null;
            Assert.ThrowsException<NullReferenceException>(
                () => { Scripting.Utils.ArrayUtils.Reverse(array); });
            array = new[] { 1, 2, 3 };
            Assert.IsTrue(new int[] { 3, 2, 1 }.SequenceEqual(Scripting.Utils.ArrayUtils.Reverse(array)));
        }
    }
}
