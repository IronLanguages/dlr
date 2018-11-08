// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public override bool Equals(object obj) =>
            Equals(obj as ValueArray<T>);

        public override int GetHashCode() {
            int val = 6551;

            for (int i = 0; i < _array.Length; i++) {
                val ^= _array[i].GetHashCode();
            }
            return val;
        }
    }
}
