// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;

namespace Metadata.Tests {
    public class Attr : Attribute {
    }

    public interface Interface1<out A, in B, out C> {
        bool Property1 { get; set; }
        int Method2();
    }

    [Attr]
    public interface Interface2 {
    }

    public class Class1<A, B, C, [Attr]D, E> : Interface1<A, B, C>
        where A : struct
        where B : class, IEnumerable
        where C : IEnumerable<int>
        where D : Class1<A, string, C, D, E>
        where E : Interface2  {

        [Attr]
        const int Const1 = 1234;

        [Attr]
        int Field1 = 1;

        [Attr]
        public int Method1<[Attr]X, Y, [Attr]Z>(X x, [Optional]Y y, params Z[] z) where X : IEnumerable<Y> {
            return Field1;
        }

        int Interface1<A, B, C>.Method2() {
            return 1;
        }

        [Attr]
        public bool Property1 {
            [Attr]
            get { return true; }
            set { }
        }

        [Attr]
        public event Func<int> Evnt {
            add { }
            remove { }
        }
    }
}
