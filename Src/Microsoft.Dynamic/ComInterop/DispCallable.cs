/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if FEATURE_COM
using System.Linq.Expressions;

using System;
using System.Dynamic;
using System.Globalization;

namespace Microsoft.Scripting.ComInterop {

    /// <summary>
    /// This represents a bound dispmember on a IDispatch object.
    /// </summary>
    internal sealed class DispCallable : IPseudoComObject {

        private readonly IDispatchComObject _dispatch;
        private readonly string _memberName;
        private readonly int _dispId;

        internal DispCallable(IDispatchComObject dispatch, string memberName, int dispId) {
            _dispatch = dispatch;
            _memberName = memberName;
            _dispId = dispId;
        }

        public override string ToString() {
            return String.Format(CultureInfo.CurrentCulture, "<bound dispmethod {0}>", _memberName);
        }

        public IDispatchComObject DispatchComObject {
            get { return _dispatch; }
        }

        public IDispatch DispatchObject {
            get { return _dispatch.DispatchObject; }
        }

        public string MemberName {
            get { return _memberName; }
        }

        public int DispId {
            get { return _dispId; }
        }

        public DynamicMetaObject GetMetaObject(Expression parameter) {
            return new DispCallableMetaObject(parameter, this);
        }

        public override bool Equals(object obj) {
            return obj is DispCallable other && other._dispatch == _dispatch && other._dispId == _dispId;
        }

        public override int GetHashCode() {
            return _dispatch.GetHashCode() ^ _dispId;
        }
    }
}

#endif
