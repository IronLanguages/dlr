// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Scripting.Debugging {
    internal static class CollectionUtils {
        internal static T[] RemoveLast<T>(this T[] array) {
            T[] result = new T[array.Length - 1];
            Array.Copy(array, 0, result, 0, result.Length);
            return result;
        }

        internal static bool ListEquals<T>(this ICollection<T> first, ICollection<T> second) {
            if (first.Count != second.Count) {
                return false;
            }

            var cmp = EqualityComparer<T>.Default;

            using (var f = first.GetEnumerator()) {
                using (var s = second.GetEnumerator()) {
                    while (f.MoveNext()) {
                        s.MoveNext();

                        if (!cmp.Equals(f.Current, s.Current)) {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        internal static int ListHashCode<T>(this IEnumerable<T> list) {
            var cmp = EqualityComparer<T>.Default;
            int h = 6551;
            foreach (T t in list) {
                h ^= (h << 5) ^ cmp.GetHashCode(t);
            }
            return h;
        }
    }

    // Compares two ICollection<T>'s using element equality
    internal sealed class ListEqualityComparer<T> : EqualityComparer<ICollection<T>> {
        internal static readonly ListEqualityComparer<T> Instance = new ListEqualityComparer<T>();

        private ListEqualityComparer() { }

        // EqualityComparer<T> handles null and object identity for us
        public override bool Equals(ICollection<T> x, ICollection<T> y) {
            return x.ListEquals(y);
        }

        public override int GetHashCode(ICollection<T> obj) {
            return obj.ListHashCode();
        }
    }
}
