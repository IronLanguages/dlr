// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Merlin.Testing.TypeSample;

namespace Merlin.Testing.Call {
    public class C {
        public void ReturnVoid() { }
        public object ReturnNull() { return null; }

        public byte ReturnByte() { return 0; }
        public sbyte ReturnSByte() { return 1; }
        public ushort ReturnUInt16() { return 2; }
        public short ReturnInt16() { return 3; }
        public uint ReturnUInt32() { return 4; }
        public int ReturnInt32() { return 5; }
        public ulong ReturnUInt64() { return 6; }
        public long ReturnInt64() { return 7; }
        public double ReturnDouble() { return 8; }
        public float ReturnSingle() { return 9; }
        public decimal ReturnDecimal() { return 10; }

        public char ReturnChar() { return 'A'; }
        public bool ReturnBoolean() { return true; }
        public string ReturnString() { return "CLR"; }

        public EnumInt16 ReturnEnum() { return EnumInt16.C; }

        public SimpleStruct ReturnStruct() { return new SimpleStruct(100); }

        public SimpleClass ReturnClass() { return new SimpleClass(200); }

        public Nullable<int> ReturnNullableInt1() { return 300; }
        public Nullable<int> ReturnNullableInt2() { return null; }

        public Nullable<SimpleStruct> ReturnNullableStruct1() { return new SimpleStruct(400); }
        public Nullable<SimpleStruct> ReturnNullableStruct2() { return null; }

        public SimpleInterface ReturnInterface() { return new ClassImplementSimpleInterface(500); }

        public Int32RInt32EventHandler ReturnDelegate() { return new Int32RInt32EventHandler(delegate(int arg) { return 2 * arg; }); }

        public int[] ReturnInt32Array() { return new int[] { 1, 2 }; }
        public SimpleStruct[] ReturnStructArray() { return new SimpleStruct[] { new SimpleStruct(1), new SimpleStruct(2) }; }
    }

    public delegate int Int32RInt32EventHandler(int arg);

    public class G<T> {
        T _arg;
        public G(T arg) { _arg = arg; }

        public T ReturnT() { return _arg; }

        public SimpleGenericStruct<T> ReturnStructT() {
            return new SimpleGenericStruct<T>(_arg);
        }

        public SimpleGenericClass<T> ReturnClassT() {
            return new SimpleGenericClass<T>(_arg);
        }

        public T[] ReturnArrayT() {
            return new T[] { _arg, _arg, _arg };
        }
    }
}
