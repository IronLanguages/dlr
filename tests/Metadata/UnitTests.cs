// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

using Microsoft.Scripting.Metadata;

namespace Metadata {
    [TestFixture]
    public class UnitTests {
        [Test]
        public unsafe void TestEmpty() {
            byte[] empty = new byte[] { 0 };
            fixed (byte* fempty = &empty[0]) {
                MetadataName e = new MetadataName(fempty, null);
                Assert.That(e, Is.EqualTo(MetadataName.Empty));
                Assert.That(e, Is.EqualTo(MetadataNamePart.Empty));
                
                Assert.That(MetadataName.Empty.IsEmpty, Is.True);
                Assert.That(MetadataName.Empty.GetHashCode(), Is.EqualTo(e.GetHashCode()));
                Assert.That(MetadataName.Empty, Is.EqualTo(e));
                Assert.That(MetadataName.Empty, Is.EqualTo(MetadataNamePart.Empty));
                Assert.That(MetadataName.Empty.GetLength(), Is.EqualTo(0));
                Assert.That(MetadataName.Empty.ToString(), Is.EqualTo(""));
                Assert.That(MetadataName.Empty.GetExtent(), Is.EqualTo(MetadataNamePart.Empty));

                Assert.That(MetadataNamePart.Empty.Length, Is.EqualTo(0));
                Assert.That(MetadataNamePart.Empty, Is.EqualTo(e));
                Assert.That(MetadataNamePart.Empty, Is.EqualTo(MetadataName.Empty));
                Assert.That(MetadataNamePart.Empty.GetPart(0), Is.EqualTo((object)MetadataNamePart.Empty));
                Assert.That(MetadataNamePart.Empty.GetPart(0), Is.EqualTo(MetadataNamePart.Empty));
                Assert.That(MetadataNamePart.Empty.GetPart(0, 0), Is.EqualTo(MetadataNamePart.Empty));
                Assert.That(MetadataNamePart.Empty.ToString(), Is.EqualTo(""));
                Assert.That(MetadataNamePart.Empty.IndexOf(1), Is.EqualTo(-1));
                Assert.That(MetadataNamePart.Empty.IndexOf(1, 0, 0), Is.EqualTo(-1));
                Assert.That(MetadataNamePart.Empty.LastIndexOf(1, 0, 0), Is.EqualTo(-1));
                Assert.That(MetadataNamePart.Empty.IndexOf(0), Is.EqualTo(-1));
                Assert.That(MetadataNamePart.Empty.IndexOf(0, 0, 0), Is.EqualTo(-1));
                Assert.That(MetadataNamePart.Empty.LastIndexOf(0, 0, 0), Is.EqualTo(-1));
            }
        }

        [TestCase("xx", '\0', -1)]
        [TestCase("", '\0', -1)]
        [TestCase("", 'x', -1)]
        [TestCase(".", '.', 0)]
        [TestCase(".", 'x', -1)]
        [TestCase("hello.world", '.', 5)]
        [TestCase("helloworld", '.', -1)]
        [TestCase("helloworld.", '.', 10)]
        public unsafe void TestIndexOf(string str, char c, int expected) {
            byte[] bytes = Encoding.UTF8.GetBytes(str + '\0');
            fixed (byte* fbytes = &bytes[0]) {
                MetadataName name = new MetadataName(fbytes, null);
                Assert.That(name.IndexOf(checked((byte)c)), Is.EqualTo(expected));
            }
        }

        [TestCase("Func`4", '`', "Func", "4")]
        [TestCase("Func`", '`', "Func", "")]
        [TestCase("`", '`', "", "")]
        [TestCase("Func", '`', null, null)]
        public unsafe void TestPrefixSuffix(string str, char separator, string expectedPrefix, string expectedSuffix) {
            byte[] bytes = Encoding.UTF8.GetBytes(str + '\0');
            fixed (byte* fbytes = &bytes[0]) {
                MetadataName name = new MetadataName(fbytes, null);
                MetadataNamePart prefix;
                MetadataNamePart suffix;
                MetadataNamePart extent = name.GetExtent();

                int index = extent.IndexOf((byte)separator);
                Assert.That((index < 0), Is.EqualTo((expectedPrefix == null)));

                if (index >= 0) {
                    prefix = extent.GetPart(0, index);
                    Assert.That(prefix.ToString(), Is.EqualTo(expectedPrefix));
                    suffix = extent.GetPart(index + 1);
                    Assert.That(suffix.ToString(), Is.EqualTo(expectedSuffix));
                }
            }
        }

        [Test]
        public unsafe void TestEquals() {
            byte[] b1 = Encoding.UTF8.GetBytes("hello\0");
            byte[] b2 = Encoding.UTF8.GetBytes("__hello__\0");
            byte[] b3 = Encoding.UTF8.GetBytes("__hell\0");
            byte[] b4 = Encoding.UTF8.GetBytes("\0");
            
            fixed (byte* fb1 = &b1[0]) {
                MetadataName name = new MetadataName(fb1, null);
                Assert.That(name.Equals(b2, 2, 5), Is.True);
                Assert.That(name.Equals(b2, 2, 6), Is.False);
                Assert.That(name.Equals(b2, 2, 4), Is.False);
                Assert.That(name.Equals(b2, 1, 4), Is.False);
            }

            fixed (byte* fb4 = &b4[0]) {
                MetadataName name = new MetadataName(fb4, null);
                Assert.That(name.Equals(b2, 2, 0), Is.True);
                Assert.That(name.Equals(b2, 0, 1), Is.False);
            }
        }

        [TestCase("System.Collections.Generic")]
        public unsafe void TestAllPrefixes(string ns) {
            byte[] bytes = Encoding.UTF8.GetBytes(ns + '\0');
            fixed (byte* fbytes = &bytes[0]) {
                MetadataNamePart name = new MetadataName(fbytes, null).GetExtent();
                int dot = name.Length;

                while (true) {
                    int nextDot = name.LastIndexOf((byte)'.', dot - 1, dot);
                    Assert.That(nextDot, Is.EqualTo(ns.LastIndexOf('.', dot - 1, dot)));
                    dot = nextDot;
                    if (dot < 0) {
                        break;
                    }
                    Assert.That(name.GetPart(0, dot).ToString(), Is.EqualTo(ns.Substring(0, dot)));
                    Assert.That(name.GetPart(dot + 1).ToString(), Is.EqualTo(ns.Substring(dot + 1)));
                }
            }
        }

        [Test]
        public unsafe void TestDict() {
            Dictionary<MetadataNamePart, int> dict = new Dictionary<MetadataNamePart, int>();
            byte[] bytes = Encoding.UTF8.GetBytes("A.B.XXXXXXXXX" + '\0');
            fixed (byte* fbytes = &bytes[0]) {
                MetadataName name = new MetadataName(fbytes, null);
                MetadataNamePart[] parts = new[] {
                    MetadataNamePart.Empty,
                    name.GetExtent(),
                    name.GetExtent().GetPart(0, 1),
                    name.GetExtent().GetPart(2, 1),
                    name.GetExtent().GetPart(4, 6),
                };

                for (int i = 0; i < parts.Length; i++) {
                    dict.Add(parts[i], i);
                }

                Assert.That(dict.Count, Is.EqualTo(parts.Length));

                for (int i = 0; i < parts.Length; i++) {
                    Assert.That(dict.TryGetValue(parts[i], out int value), Is.True);
                    Assert.That(value, Is.EqualTo(i));
                }
            }
        }
    }
}
