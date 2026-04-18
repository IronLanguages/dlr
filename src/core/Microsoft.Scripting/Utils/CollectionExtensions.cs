// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Scripting.Utils {
    internal static class CollectionExtensions {
        /// <summary>
        /// Materlializes the provided enumerable if needed and wraps it in a ReadOnlyCollection{T}
        /// </summary>
        /// <remarks>
        /// Copies all of the data into a new array, so the data can't be
        /// accidentally changed after creation. The exception is if the enumerable is
        /// already a ReadOnlyCollection{T}, in which case we just return it.
        /// </remarks>
        internal static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable) {
            if (enumerable is null) {
                return EmptyReadOnlyCollection<T>.Instance;
            }

            if (enumerable is ReadOnlyCollection<T> roCollection) {
                return roCollection;
            }

            if (enumerable is ICollection<T> collection) {
                int count = collection.Count;
                if (count == 0) {
                    return EmptyReadOnlyCollection<T>.Instance;
                }

                T[] array = new T[count];
                collection.CopyTo(array, 0);
                return new ReadOnlyCollection<T>(array);
            }

            // ToArray trims the excess space and speeds up access
            return new ReadOnlyCollection<T>(System.Linq.Enumerable.ToArray(enumerable));
        }
    }

    internal static class EmptyReadOnlyCollection<T> {
        internal static readonly ReadOnlyCollection<T> Instance = new(Array.Empty<T>());
    }
}
