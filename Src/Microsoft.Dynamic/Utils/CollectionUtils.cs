// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting.Utils {
    /// <summary>
    /// Allows wrapping of proxy types (like COM RCWs) to expose their IEnumerable functionality
    /// which is supported after casting to IEnumerable, even though Reflection will not indicate 
    /// IEnumerable as a supported interface
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")] // TODO
    public class EnumerableWrapper : IEnumerable {
        private IEnumerable _wrappedObject;
        public EnumerableWrapper(IEnumerable o) {
            _wrappedObject = o;
        }

        public IEnumerator GetEnumerator() {
            return _wrappedObject.GetEnumerator();
        }
    }

    public static class CollectionUtils {
        public static IEnumerable<T> Cast<S, T>(this IEnumerable<S> sequence) where S : T {
            return (IEnumerable<T>)sequence;
        }

        public static IEnumerable<TSuper> ToCovariant<T, TSuper>(IEnumerable<T> enumerable)
            where T : TSuper {
            return (IEnumerable<TSuper>)enumerable;
        }

        public static void AddRange<T>(ICollection<T> collection, IEnumerable<T> items) {
            ContractUtils.RequiresNotNull(collection, nameof(collection));
            ContractUtils.RequiresNotNull(items, nameof(items));

            if (collection is List<T> list) {
                list.AddRange(items);
            } else {
                foreach (T item in items) {
                    collection.Add(item);
                }
            }
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items) {
            foreach (var item in items) {
                list.Add(item);
            }
        }

        public static IEnumerable<T> ToEnumerable<T>(IEnumerable enumerable) {
            foreach (T item in enumerable) {
                yield return item;
            }
        }

        public static IEnumerator<TSuper> ToCovariant<T, TSuper>(IEnumerator<T> enumerator)
            where T : TSuper {

            ContractUtils.RequiresNotNull(enumerator, nameof(enumerator));

            while (enumerator.MoveNext()) {
                yield return enumerator.Current;
            }
        }

        private class CovariantConvertor<T, TSuper> : IEnumerable<TSuper> where T : TSuper {
            private IEnumerable<T> _enumerable;

            public CovariantConvertor(IEnumerable<T> enumerable) {
                ContractUtils.RequiresNotNull(enumerable, nameof(enumerable));
                _enumerable = enumerable;
            }

            public IEnumerator<TSuper> GetEnumerator() {
                return ToCovariant<T, TSuper>(_enumerable.GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        public static IDictionaryEnumerator ToDictionaryEnumerator(IEnumerator<KeyValuePair<object, object>> enumerator) {
            return new DictionaryEnumerator(enumerator);
        }

        private sealed class DictionaryEnumerator : IDictionaryEnumerator {
            private readonly IEnumerator<KeyValuePair<object, object>> _enumerator;

            public DictionaryEnumerator(IEnumerator<KeyValuePair<object, object>> enumerator) {
                _enumerator = enumerator;
            }

            public DictionaryEntry Entry {
                get { return new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value); }
            }

            public object Key {
                get { return _enumerator.Current.Key; }
            }

            public object Value {
                get { return _enumerator.Current.Value; }
            }

            public object Current {
                get { return Entry; }
            }

            public bool MoveNext() {
                return _enumerator.MoveNext();
            }

            public void Reset() {
                _enumerator.Reset();
            }
        }

        public static List<T> MakeList<T>(T item) {
            List<T> result = new List<T>();
            result.Add(item);
            return result;
        }

        public static int CountOf<T>(IList<T> list, T item) where T : IEquatable<T> {
            if (list == null) return 0;

            int result = 0;
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Equals(item)) {
                    result++;
                }
            }
            return result;
        }

        public static int Max(this IEnumerable<int> values) {
            ContractUtils.RequiresNotNull(values, nameof(values));

            int result = Int32.MinValue;
            foreach (var value in values) {
                if (value > result) {
                    result = value;
                }
            }
            return result;
        }

        public static bool TrueForAll<T>(IEnumerable<T> collection, Predicate<T> predicate) {
            ContractUtils.RequiresNotNull(collection, nameof(collection));
            ContractUtils.RequiresNotNull(predicate, nameof(predicate));

            foreach (T item in collection) {
                if (!predicate(item)) return false;
            }

            return true;
        }

        public static IList<TRet> ConvertAll<T, TRet>(IList<T> collection, Func<T, TRet> predicate) {
            ContractUtils.RequiresNotNull(collection, nameof(collection));
            ContractUtils.RequiresNotNull(predicate, nameof(predicate));

            List<TRet> res = new List<TRet>(collection.Count);
            foreach (T item in collection) {
                res.Add(predicate(item));
            }

            return res;
        }

        public static IEnumerable<TRet> Select<TRet>(this IEnumerable enumerable, Func<object, TRet> selector) {
            ContractUtils.RequiresNotNull(enumerable, nameof(enumerable));
            ContractUtils.RequiresNotNull(selector, nameof(selector));

            foreach (object item in enumerable) {
                yield return selector(item);
            }
        }


        public static List<T> GetRange<T>(IList<T> list, int index, int count) {
            ContractUtils.RequiresNotNull(list, nameof(list));
            ContractUtils.RequiresArrayRange(list, index, count, nameof(index), nameof(count));

            List<T> result = new List<T>(count);
            int stop = index + count;
            for (int i = index; i < stop; i++) {
                result.Add(list[i]);
            }
            return result;
        }

        public static void InsertRange<T>(IList<T> collection, int index, IEnumerable<T> items) {
            ContractUtils.RequiresNotNull(collection, nameof(collection));
            ContractUtils.RequiresNotNull(items, nameof(items));
            ContractUtils.RequiresArrayInsertIndex(collection, index, nameof(index));

            if (collection is List<T> list) {
                list.InsertRange(index, items);
            } else {
                int i = index;
                foreach (T obj in items) {
                    collection.Insert(i++, obj);
                }
            }
        }

        public static void RemoveRange<T>(IList<T> collection, int index, int count) {
            ContractUtils.RequiresNotNull(collection, nameof(collection));
            ContractUtils.RequiresArrayRange(collection, index, count, nameof(index), nameof(count));

            if (collection is List<T> list) {
                list.RemoveRange(index, count);
            } else {
                for (int i = index + count - 1; i >= index; i--) {
                    collection.RemoveAt(i);
                }
            }
        }

        public static int FindIndex<T>(this IList<T> collection, Predicate<T> predicate) {
            ContractUtils.RequiresNotNull(collection, nameof(collection));
            ContractUtils.RequiresNotNull(predicate, nameof(predicate));

            for (int i = 0; i < collection.Count; i++) {
                if (predicate(collection[i])) {
                    return i;
                }
            }
            return -1;
        }

        public static IList<T> ToSortedList<T>(this ICollection<T> collection, Comparison<T> comparison) {
            ContractUtils.RequiresNotNull(collection, nameof(collection));
            ContractUtils.RequiresNotNull(comparison, nameof(comparison));

            var array = new T[collection.Count];
            collection.CopyTo(array, 0);
            Array.Sort(array, comparison);
            return array;
        }

        public static T[] ToReverseArray<T>(this IList<T> list) {
            ContractUtils.RequiresNotNull(list, nameof(list));
            T[] result = new T[list.Count];
            for (int i = 0; i < result.Length; i++) {
                result[i] = list[result.Length - 1 - i];
            }
            return result;
        }

        public static IEqualityComparer<HashSet<T>> CreateSetComparer<T>() {
            return HashSet<T>.CreateSetComparer();
        }
    }
}
