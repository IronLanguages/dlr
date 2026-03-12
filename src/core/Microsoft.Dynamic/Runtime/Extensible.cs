// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    public class Extensible<T> {

        public Extensible() { }
        public Extensible(T value) { Value = value; }

        public T Value { get; }

        public override bool Equals(object obj) => Value.Equals(obj);

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public override string ToString() {
            return Value.ToString();
        }

        public static implicit operator T(Extensible<T> extensible) {
            return extensible.Value;
        }
    }
}
