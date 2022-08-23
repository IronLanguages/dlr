// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Merlin.Testing.TypeSample;

namespace Merlin.Testing.Event {
    public delegate Int32 Int32Int32Delegate(Int32 arg);

    public class TargetClass {
        public static Int32 s_Double(Int32 arg) { Flag.Add(1); return arg * 2; }
        public static Int32 s_Negate(Int32 arg) { Flag.Add(10); return -arg; }
        public static Int32 s_Square(Int32 arg) { Flag.Add(100); return arg * arg; }
        public static Int32 s_Throw(Int32 arg) { throw new ApplicationException(); }

        public Int32 i_Double(Int32 arg) { Flag.Add(1); return arg * 2; }
        public Int32 i_Negate(Int32 arg) { Flag.Add(10); return -arg; }
        public Int32 i_Square(Int32 arg) { Flag.Add(100); return arg * arg; }
        public Int32 i_Throw(Int32 arg) { throw new ApplicationException(); }
    }

    public struct TargetStruct {
        public static Int32 s_Double(Int32 arg) { Flag.Add(1); return arg * 2; }
        public static Int32 s_Negate(Int32 arg) { Flag.Add(10); return -arg; }
        public static Int32 s_Square(Int32 arg) { Flag.Add(100); return arg * arg; }
        public static Int32 s_Throw(Int32 arg) { throw new ApplicationException(); }

        public Int32 i_Double(Int32 arg) { Flag.Add(1); return arg * 2; }
        public Int32 i_Negate(Int32 arg) { Flag.Add(10); return -arg; }
        public Int32 i_Square(Int32 arg) { Flag.Add(100); return arg * arg; }
        public Int32 i_Throw(Int32 arg) { throw new ApplicationException(); }
    }

    public interface IInterface {
        event Int32Int32Delegate OnAction;
    }

    public struct StructImplicitlyImplementInterface : IInterface {
        public event Int32Int32Delegate OnAction;

        public Int32 CallInside(Int32 arg) {
            if (OnAction != null) {
                return OnAction(arg);
            } else {
                return -1;
            }
        }
    }

    public class ClassImplicitlyImplementInterface : IInterface {
        public event Int32Int32Delegate OnAction;

        public Int32 CallInside(Int32 arg) {
            if (OnAction != null) {
                return OnAction(arg);
            } else {
                return -1;
            }
        }
    }

    public struct StructWithSimpleEvent {
        public event Int32Int32Delegate OnAction;

        public Int32 CallInside(Int32 arg) {
            if (OnAction != null) {
                return OnAction(arg);
            } else {
                return -1;
            }
        }
    }
    public class ClassWithSimpleEvent {
        public event Int32Int32Delegate OnAction;

        public Int32 CallInside(Int32 arg) {
            if (OnAction != null) {
                return OnAction(arg);
            } else {
                return -1;
            }
        }
    }

    public struct StructExplicitlyImplementInterface : IInterface {
        private Int32Int32Delegate _private;
        event Int32Int32Delegate IInterface.OnAction {
            add { _private += value; }
            remove { _private -= value; }
        }
    }

    public class ClassExplicitlyImplementInterface : IInterface {
        private Int32Int32Delegate _private;
        event Int32Int32Delegate IInterface.OnAction {
            add { _private += value; }
            remove { _private -= value; }
        }
    }

    public class ClassWithStaticEvent {
        public static event Int32Int32Delegate OnAction;

        public Int32 CallInside(Int32 arg) {
            if (OnAction != null) {
                return OnAction(arg);
            } else {
                return -1;
            }
        }
    }
    public struct StructWithStaticEvent {
        public static event Int32Int32Delegate OnAction;

        public Int32 CallInside(Int32 arg) {
            if (OnAction != null) {
                return OnAction(arg);
            } else {
                return -1;
            }
        }
    }

    public class DerivedClassWithStaticEvent : ClassWithStaticEvent { }

    //public class ClassWithAddOnlyEvent {
    //    private Int32Int32Delegate _private;
    //    public event Int32Int32Delegate OnAction {
    //        add { _private += value; }
    //    }
    //}

    //public class ClassWithRemoveOnlyEvent {
    //    private Int32Int32Delegate _private;
    //    public event Int32Int32Delegate OnAction {
    //        remove { _private -= value; }
    //    }
    //}

    //public class ClassWithPrivateAddPublicRemoveEvent {
    //    public event Int32Int32Delegate OnAction {
    //        add { }
    //        remove { }
    //    }
    //}
}
