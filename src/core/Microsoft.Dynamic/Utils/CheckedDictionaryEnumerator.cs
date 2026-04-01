// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting.Utils {
    /// <summary>
    /// Not all .NET enumerators throw exceptions if accessed in an invalid state. This type
    /// can be used to throw exceptions from enumerators implemented in IronPython.
    /// </summary>
    public abstract class CheckedDictionaryEnumerator : IDictionaryEnumerator, IEnumerator<KeyValuePair<object, object>> {
        private EnumeratorState _enumeratorState = EnumeratorState.NotStarted;

        private void CheckEnumeratorState() {
            if (_enumeratorState == EnumeratorState.NotStarted)
                throw Error.EnumerationNotStarted();

            if (_enumeratorState == EnumeratorState.Ended)
                throw Error.EnumerationFinished();
        }

        #region IDictionaryEnumerator Members
        public DictionaryEntry Entry {
            get {
                CheckEnumeratorState();
                return new DictionaryEntry(Key, Value);
            }
        }

        public object Key {
            get {
                CheckEnumeratorState();
                return GetKey();
            }
        }

        public object Value {
            get {
                CheckEnumeratorState();
                return GetValue();
            }
        }
        #endregion

        #region IEnumerator Members
        public bool MoveNext() {
            if (_enumeratorState == EnumeratorState.Ended)
                throw Error.EnumerationFinished();

            bool result = DoMoveNext();
            if (result)
                _enumeratorState = EnumeratorState.Started;
            else
                _enumeratorState = EnumeratorState.Ended;
            return result;
        }

        public object Current { get { return Entry; } }

        public void Reset() {
            DoReset();
            _enumeratorState = EnumeratorState.NotStarted;
        }
        #endregion

        #region IEnumerator<KeyValuePair<object,object>> Members

        KeyValuePair<object, object> IEnumerator<KeyValuePair<object, object>>.Current {
            get { return new KeyValuePair<object, object>(Key, Value); }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Methods that a sub-type needs to implement

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")] // TODO: fix
        protected abstract object GetKey();
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")] // TODO: fix
        protected abstract object GetValue();

        protected abstract bool DoMoveNext();
        protected abstract void DoReset();

        #endregion

        private enum EnumeratorState {
            NotStarted,
            Started,
            Ended
        }
    }
}
