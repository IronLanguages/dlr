// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Utils {

    /// <seealso href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Linq.Expressions/src/System/Dynamic/Utils/CollectionExtensions.cs"/>
    internal static class CollectionExtensions {
        /// <summary>
        /// Materlializes the provided enumerable if needed and wraps it in a ReadOnlyCollection{T}
        /// </summary>
        /// <remarks>
        /// Copies all of the data into a new array, so the data can't be
        /// accidentally changed after creation. The exception is if the enumerable is
        /// already a ReadOnlyCollection{T} (or its subclass), in which case we just return it.
        /// </remarks>
        /// <seealso href="https://github.com/dotnet/runtime/blob/b70c35ed8a2e7ae0d91de76f4f5d26c2e7d2c6cd/src/libraries/System.Linq.Expressions/src/System/Dynamic/Utils/CollectionExtensions.cs#L58"/>
        internal static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T>? enumerable) {
            return enumerable switch {
                null => ReadOnlyCollection<T>.Empty,
                ReadOnlyCollection<T> roc => roc,
                ReadOnlyCollectionBuilder<T> builder => builder.ToReadOnlyCollection(),
                _ => enumerable.ToArray() switch {  // ToArray does all necessary casting, copying, and possible optimizations
                    { Length: 0 } => ReadOnlyCollection<T>.Empty,
                    var array => new ReadOnlyCollection<T>(array),
                },
            };
        }
    }
}
