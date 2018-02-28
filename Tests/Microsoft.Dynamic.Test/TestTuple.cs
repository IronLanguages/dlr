// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting;
using NUnit.Framework;

namespace Microsoft.Dynamic.Test {

    // Strongbox should not ever be sealed
    class MyStrongBox<T> : StrongBox<T> {
        public MyStrongBox(T value) : base(value) {
        }
    }

    [TestFixture]
    public class TestTuple {
        public void VerifyTuple(int size) {
            //Construct a tuple of the right type
            MethodInfo mi = typeof(MutableTuple).GetMethod("MakeTupleType", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(mi, "Could not find Tuple.MakeTupleType");

            Type[] args = new Type[size];
            object[] values = new object[size];
            for (int i = 0; i < size; i++) {
                args[i] = typeof(int);
                values[i] = 0;
            }

            Type tupleType = (Type)mi.Invoke(null, new object[] { args });
            MutableTuple t = MutableTuple.MakeTuple(tupleType, values);

            /////////////////////
            //Properties

            //Write
            for (int i = 0; i < size; i++) {
                object o = t;
                foreach (PropertyInfo pi in MutableTuple.GetAccessPath(tupleType, i)) {
                    if (typeof(MutableTuple).IsAssignableFrom(pi.PropertyType))
                        o = pi.GetValue(o, null);
                    else
                        pi.SetValue(o, i * 5, null);
                }
            }

            //Read
            for (int i = 0; i < size; i++) {
                object o = t;
                foreach (PropertyInfo pi in MutableTuple.GetAccessPath(tupleType, i))
                    o = pi.GetValue(o, null);
                Assert.AreEqual(typeof(int), o.GetType());
                Assert.AreEqual((int)o, i * 5);
            }

            //Negative cases for properties
            Assert.Throws<ArgumentException>(delegate () {
                foreach (PropertyInfo pi in MutableTuple.GetAccessPath(tupleType, -1))
                    Console.WriteLine(pi.Name); //This won't run, but we need it so that this call isn't inlined
            });

            /////////////////////
            //GetTupleValues
            values = MutableTuple.GetTupleValues(t);
            Assert.AreEqual(values.Length, size);
            for (int i = 0; i < size; i++) {
                Assert.AreEqual(typeof(int), values[i].GetType());
                Assert.AreEqual((int)(values[i]), i * 5);
            }

            /////////////////////
            //Access methods

            if (size <= MutableTuple.MaxSize) {
                //SetValue
                for (int i = 0; i < size; i++)
                    t.SetValue(i, i * 3);

                //GetValue
                for (int i = 0; i < size; i++)
                    Assert.AreEqual(t.GetValue(i), i * 3);

                //Ensure there are no extras
                if (tupleType.GetGenericArguments().Length <= size) {
                    //We're requesting an index beyond the end of this tuple.
                    Assert.Throws<ArgumentOutOfRangeException>(delegate () { t.SetValue(size, 3); });
                    Assert.Throws<ArgumentOutOfRangeException>(delegate () { t.GetValue(size); });
                } else {
                    /*We're requesting an index in the scope of this tuple but beyond the scope of our
                     requested capacity (in which case the field's type will be Microsoft.Scripting.None
                     and we won't be able to convert "3" to that).  Imagine asking for a tuple of 3 ints,
                     we'd actually get a Tuple<int,int,int,Microsoft.Scripting.None> since there is no
                     Tuple that takes only 3 generic arguments.*/
                    Assert.Throws<InvalidCastException>(delegate () { t.SetValue(size, 3); });

                    //Verify the type of the field
                    Assert.AreEqual(typeof(Microsoft.Scripting.Runtime.DynamicNull), tupleType.GetGenericArguments()[size]);

                    //Verify the value of the field is null
                    Assert.AreEqual(null, t.GetValue(size));
                }
            }
        }

        [Test]
        public void TestBasic() {
            foreach (int i in new int[] { 1, 2, 4, 8, 16, 32, 64, 127, 128, 129, 256, 512, 1024, 24, 96 }) {
                VerifyTuple(i);
            }
        }

        [Test]
        public void TestStrongBox() {
            MyStrongBox<int> sb = new MyStrongBox<int>(5);
            Assert.AreEqual(sb.Value, 5);
        }
    }
}
