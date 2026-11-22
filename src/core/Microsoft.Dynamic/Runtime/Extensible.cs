// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.Scripting.Runtime {
    public class Extensible<T> where T : notnull {

        [Obsolete("Do not use - it breaks the notnull guarantee. Migrate to Extensible(default(T)).")]
        public Extensible() {
            Utils.Assert.NotNull(default(T));
            Value = default!;
        }

        public Extensible(T value) { Value = value; }

        public T Value { get; }

        public override bool Equals(object? obj) => Value.Equals(obj);

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public override string ToString() {
            return Value.ToString() ?? string.Empty;
        }

        public static implicit operator T(Extensible<T> extensible) {
            return extensible.Value;
        }
    }
}
