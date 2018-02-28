// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_LCG

using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation {

    public abstract class DynamicILGen : ILGen {
        internal DynamicILGen(ILGenerator il)
            : base(il) {
        }

        public T CreateDelegate<T>() {
            return CreateDelegate<T>(out MethodInfo _);
        }

        public abstract T CreateDelegate<T>(out MethodInfo mi);

        public abstract MethodInfo Finish();
    }

    class DynamicILGenMethod : DynamicILGen {
        private readonly DynamicMethod _dm;

        internal DynamicILGenMethod(DynamicMethod dm, ILGenerator il)
            : base(il) {
            _dm = dm;
        }

        public override T CreateDelegate<T>(out MethodInfo mi) {
            ContractUtils.Requires(typeof(T).IsSubclassOf(typeof(Delegate)), "T");
            mi = _dm;
            return (T)(object)_dm.CreateDelegate(typeof(T), null);
        }

        public override MethodInfo Finish() {
            return _dm;
        }
    }
#if FEATURE_REFEMIT
    class DynamicILGenType : DynamicILGen {
        private readonly TypeBuilder _tb;
        private readonly MethodBuilder _mb;

        internal DynamicILGenType(TypeBuilder tb, MethodBuilder mb, ILGenerator il)
            : base(il) {
            _tb = tb;
            _mb = mb;
        }

        public override T CreateDelegate<T>(out MethodInfo mi) {
            ContractUtils.Requires(typeof(T).IsSubclassOf(typeof(Delegate)), "T");
            mi = CreateMethod();
            return (T)(object)mi.CreateDelegate(typeof(T));
        }

        private MethodInfo CreateMethod() {
            Type t = _tb.CreateType();
            return t.GetMethod(_mb.Name);
        }

        public override MethodInfo Finish() {
            return CreateMethod();
        }
    }
#endif
}
#endif