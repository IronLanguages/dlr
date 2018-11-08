// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Scripting.Utils {
    // Compares two ICollection<T>'s using element equality
    internal sealed class ListEqualityComparer<T> : EqualityComparer<ICollection<T>> {
        internal static readonly ListEqualityComparer<T> Instance = new ListEqualityComparer<T>();

        private ListEqualityComparer() { }

        // EqualityComparer<T> handles null and object identity for us
        public override bool Equals(ICollection<T> x, ICollection<T> y) => x.ListEquals(y);

        public override int GetHashCode(ICollection<T> obj) {
            return obj.ListHashCode();
        }
    }
}
