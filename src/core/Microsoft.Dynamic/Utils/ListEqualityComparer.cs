// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.Scripting.Utils {
    // Compares two ICollection<T>'s using element equality
    internal sealed class ListEqualityComparer<T> : EqualityComparer<ICollection<T>> {
        internal static readonly ListEqualityComparer<T> Instance = new();

        private ListEqualityComparer() { }

        public override bool Equals(ICollection<T>? x, ICollection<T>? y)
            => ReferenceEquals(x, y) || x is not null && y is not null && x.ListEquals(y);

        public override int GetHashCode(ICollection<T> obj) {
            return obj.ListHashCode();
        }
    }
}
