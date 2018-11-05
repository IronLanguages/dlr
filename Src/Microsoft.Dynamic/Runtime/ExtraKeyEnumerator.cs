// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    class ExtraKeyEnumerator : CheckedDictionaryEnumerator {
        private readonly CustomStringDictionary _idDict;
        private int _curIndex = -1;

        public ExtraKeyEnumerator(CustomStringDictionary idDict) {
            _idDict = idDict;
        }

        protected override object GetKey() {
            return _idDict.GetExtraKeys()[_curIndex];
        }

        protected override object GetValue() {
            bool hasExtraValue = _idDict.TryGetExtraValue(_idDict.GetExtraKeys()[_curIndex], out object val);
            Debug.Assert(hasExtraValue && !(val is Uninitialized));
            return val;
        }

        protected override bool DoMoveNext() {
            if (_idDict.GetExtraKeys().Length == 0)
                return false;

            while (_curIndex < (_idDict.GetExtraKeys().Length - 1)) {
                _curIndex++;
                if (_idDict.TryGetExtraValue(_idDict.GetExtraKeys()[_curIndex], out object val) &&
                    val != Uninitialized.Instance) {
                    return true;
                }
            }
            return false;
        }

        protected override void DoReset() {
            _curIndex = -1;
        }
    }
}
