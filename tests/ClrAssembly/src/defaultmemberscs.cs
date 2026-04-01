// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Merlin.Testing.DefaultMemberSample {
    //
    // Currently there is no way to expose default field/property/method/event
    //

    [DefaultMember("Field")]
    public class ClassWithDefaultField {
        public int Field = 10;
    }

    [DefaultMember("Property")]
    public class ClassWithDefaultProperty {
        int _value;
        public int Property {
            get { return _value; }
            set { _value = value; }
        }
    }

    [DefaultMember("Method")]
    public class ClassWithDefaultMethod {
        public void Method() { Flag.Set(11); }
    }

    [DefaultMember("ClassWithDefaultMemberCtor")]
    public class ClassWithDefaultMemberCtor {
        public ClassWithDefaultMemberCtor(int i) { Flag.Set(21); }
    }

    //
    // static member as default member
    //

    [DefaultMember("Field")]
    public class ClassWithDefaultStaticField {
        public static int Field = 10;
    }

    //
    // value type having default member
    //
    [DefaultMember("Method")]
    public struct StructWithDefaultMethod {
        public void Method() { Flag.Set(21); }
    }

    //
    // indexing operation is the only member kind currently supported
    //

    // special name
    [DefaultMember("Item")]
    public class ClassWithItem {
        int _value;
        public int Item {
            get { return _value; }
            set { _value = value; }
        }
    }

    [DefaultMember("set_Item")]
    public class ClassWithset_Item {
        public void set_Item(int arg) { Flag.Set(arg); }
    }

    [DefaultMember("get_Item")]
    public class ClassWithget_Item {
        public int get_Item(int arg1, int arg2) { return arg1 + arg2; }
    }

    //
    // Cannot specify the DefaultMember attribute on a type containing an indexer
    //
    // [DefaultMember("Item")]
    // public class ClassWith {
    //    public int this[int i] {
    //        get { return i; }
    //        set { Flag.Set(i + value); }
    //    }
    // }

}
