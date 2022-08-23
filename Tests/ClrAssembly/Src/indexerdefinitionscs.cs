// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Merlin.Testing.TypeSample;

// C# has no static indexer
// ref, out modifier is not valid for this[xxx] signature

namespace Merlin.Testing.Indexer {
    public interface IReturnDouble {
        int this[int arg] { get; set; }
    }

    public struct StructExplicitImplementInterface : IReturnDouble {
        int[] array;
        public void Init() {
            array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        }
        int IReturnDouble.this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }
    }

    public class ClassExplicitImplementInterface : IReturnDouble {
        int[] array;
        public void Init() {
            array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        }
        int IReturnDouble.this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }
    }

    public class DerivedClassExplicitImplementInterface : ClassExplicitImplementInterface { }

    public struct StructImplicitImplementInterface : IReturnDouble {
        int[] array;
        public void Init() {
            array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        }
        public int this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }
    }

    public class ClassImplicitImplementInterface : IReturnDouble {
        int[] array;
        public void Init() {
            array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        }
        public int this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }
    }

    public struct StructWithIndexer {
        int[] array;
        Dictionary<string, object> dict;

        public void Init() {
            array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            dict = new Dictionary<string, object>();
        }

        public int this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }

        public SimpleStruct this[int arg1, string arg2] {
            get { return (SimpleStruct)dict[arg2 + arg1.ToString()]; }
            set { dict[arg2 + arg1.ToString()] = value; }
        }

        public SimpleClass this[string arg1, string arg2, string arg3] {
            get { return (SimpleClass)dict[arg1 + arg2 + arg3]; }
            set { dict[arg1 + arg2 + arg3] = value; }
        }
    }

    public class ClassWithIndexer {
        int[] array;
        Dictionary<string, object> dict;

        public void Init() {
            array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            dict = new Dictionary<string, object>();
        }

        public int this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }

        public SimpleStruct this[int arg1, string arg2] {
            get { return (SimpleStruct)dict[arg2 + arg1.ToString()]; }
            set { dict[arg2 + arg1.ToString()] = value; }
        }

        public SimpleClass this[string arg1, string arg2, string arg3] {
            get { return (SimpleClass)dict[arg1 + arg2 + arg3]; }
            set { dict[arg1 + arg2 + arg3] = value; }
        }
    }

    public class DerivedClassWithoutIndexer : ClassWithIndexer { }

    public class ClassWithParamsIndexer {
        int[] array;
        public void Init() {
            array = new int[100];
            for (int i = 0; i < 100; i++) {
                array[i] = i;
            }
            array[0] = -100;
        }
        public int this[params int[] args] {
            get {
                int sum = 0;
                foreach (int x in args) {
                    sum += x;
                }
                return array[sum];
            }
            set {
                int sum = 0;
                foreach (int x in args) {
                    sum += x;
                }
                array[sum] = value;
            }
        }
    }

    public class ClassWithIndexerOverloads1 {
        int[] array;
        public void Init() {
            array = new int[100];
            for (int i = 0; i < 100; i++) {
                array[i] = i;
            }
            array[0] = -200;
        }

        public int this[params int[] args] {
            get {
                int sum = 0;
                foreach (int x in args) {
                    sum += x;
                }
                return array[sum];
            }
            set {
                int sum = 0;
                foreach (int x in args) {
                    sum += x;
                }
                array[sum] = value;
            }
        }
        public int this[int arg1, int arg2] {
            get { return array[arg1 * arg2]; }
            set { array[arg1 * arg2] = value; }
        }
    }

    public class ClassWithIndexerOverloads2 {
        int[] array = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        public int this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }

        Dictionary<string, string> dict = new Dictionary<string, string>();
        public string this[string arg] {
            get { return dict[arg]; }
            set { dict[arg] = value; }
        }
    }

    // more overload scenarios needed

    public class ReadOnlyIndexer {
        public int this[int arg] {
            get { return 10; }
        }
    }

    public class WriteOnlyIndexer {
        public int this[int arg] {
            set { Flag.Set(arg + value); }
        }
    }

    public class BaseClassWithIndexer {
        protected int[] array = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        public int this[int arg] {
            get { return array[arg]; }
            set { array[arg] = value; }
        }
    }

    public class DerivedClassWithNewIndexer : BaseClassWithIndexer {
        public new int this[int arg] {
            get { return array[arg] * -1; }
            set { array[arg] = value * 2; }
        }
    }

    public class DerivedClassWithNewWriteOnlyIndexer : BaseClassWithIndexer {
        public new int this[int arg] {
            set { array[arg] = value * 2; }
        }
    }
}
