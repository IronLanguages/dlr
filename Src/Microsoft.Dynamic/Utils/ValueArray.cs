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

using System;

namespace Microsoft.Scripting.Utils {

    /// <summary>
    /// Represents an array that has value equality.
    /// </summary>
    public class ValueArray<T> : IEquatable<ValueArray<T>> {
        private readonly T[] _array;

        public ValueArray(T[] array) {
            ContractUtils.RequiresNotNull(array, nameof(array));
            ContractUtils.RequiresNotNullItems(array, nameof(array));
            _array = array;
        }

        #region IEquatable<ValueArray<T>> Members

        public bool Equals(ValueArray<T> other) {
            if (other == null) return false;
            return _array.ValueEquals(other._array);
        }

        #endregion

        public override bool Equals(object obj) {
            return Equals(obj as ValueArray<T>);
        }

        public override int GetHashCode() {
            int val = 6551;

            for (int i = 0; i < _array.Length; i++) {
                val ^= _array[i].GetHashCode();
            }
            return val;
        }
    }
}
