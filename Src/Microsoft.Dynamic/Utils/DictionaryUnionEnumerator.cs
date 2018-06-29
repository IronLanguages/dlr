// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting.Utils {
    /// <summary>
    /// Presents a flat enumerable view of multiple dictionaries
    /// </summary>
    public class DictionaryUnionEnumerator : CheckedDictionaryEnumerator {
        private IList<IDictionaryEnumerator> _enums;
        private int _current = 0;

        public DictionaryUnionEnumerator(IList<IDictionaryEnumerator> enums) {
            _enums = enums;
        }

        protected override object GetKey() {
            return _enums[_current].Key;
        }

        protected override object GetValue() {
            return _enums[_current].Value;
        }

        protected override bool DoMoveNext() {
            // Have we already walked over all the enumerators in the list?
            if (_current == _enums.Count)
                return false;

            // Are there any more entries in the current enumerator?
            if (_enums[_current].MoveNext())
                return true;

            // Move to the next enumerator in the list
            _current++;

            // Make sure that the next enumerator is ready to be used
            return DoMoveNext();
        }

        protected override void DoReset() {
            for (int i = 0; i < _enums.Count; i++) {
                _enums[i].Reset();
            }
            _current = 0;
        }

    }

}
