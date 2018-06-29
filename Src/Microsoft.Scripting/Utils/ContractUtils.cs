// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Utils {
    internal static class ContractUtils {
        public static void RequiresNotNull(object value, string paramName) {
            Assert.NotEmpty(paramName);

            if (value == null) {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void Requires(bool precondition) {
            if (!precondition) {
                throw new ArgumentException(Strings.MethodPreconditionViolated);
            }
        }

        public static void Requires(bool precondition, string paramName) {
            Assert.NotEmpty(paramName);

            if (!precondition) {
                throw new ArgumentException(Strings.InvalidArgumentValue, paramName);
            }
        }

        public static void Requires(bool precondition, string paramName, string message) {
            Assert.NotEmpty(paramName);

            if (!precondition) {
                throw new ArgumentException(message, paramName);
            }
        }

        public static void RequiresNotEmpty(string str, string paramName) {
            RequiresNotNull(str, paramName);
            if (str.Length == 0) {
                throw new ArgumentException(Strings.NonEmptyStringRequired, paramName);
            }
        }

        public static void RequiresNotEmpty<T>(ICollection<T> collection, string paramName) {
            RequiresNotNull(collection, paramName);
            if (collection.Count == 0) {
                throw new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
            }
        }

        /// <summary>
        /// Requires the range [offset, offset + count] to be a subset of [0, array.Count].
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count are out of range.</exception>
        public static void RequiresArrayRange<T>(IList<T> array, int offset, int count, string offsetName, string countName) {
            Assert.NotNull(array);
            RequiresArrayRange(array.Count, offset, count, offsetName, countName);
        }

        /// <summary>
        /// Requires the range [offset, offset + count] to be a subset of [0, array.Count].
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count are out of range.</exception>
        public static void RequiresArrayRange(int arraySize, int offset, int count, string offsetName, string countName) {
            Assert.NotEmpty(offsetName);
            Assert.NotEmpty(countName);
            Debug.Assert(arraySize >= 0);

            if (count < 0) throw new ArgumentOutOfRangeException(countName);
            if (offset < 0 || arraySize - offset < count) throw new ArgumentOutOfRangeException(offsetName);
        }

        /// <summary>
        /// Requires the array and all its items to be non-null.
        /// </summary>
        public static void RequiresNotNullItems<T>(IList<T> array, string arrayName) {
            Assert.NotNull(arrayName);
            RequiresNotNull(array, arrayName);

            for (int i = 0; i < array.Count; i++) {
                if (array[i] == null) {
                    throw ExceptionUtils.MakeArgumentItemNullException(i, arrayName);
                }
            }
        }

        /// <summary>
        /// Requires the enumerable collection and all its items to be non-null.
        /// </summary>
        public static void RequiresNotNullItems<T>(IEnumerable<T> collection, string collectionName) {
            Assert.NotNull(collectionName);
            RequiresNotNull(collection, collectionName);

            int i = 0;
            foreach (var item in collection) {
                if (item == null) {
                    throw ExceptionUtils.MakeArgumentItemNullException(i, collectionName);
                }
                i++;
            }
        }

        /// <summary>
        /// Requires the range [offset, offset + count] to be a subset of [0, array.Count].
        /// </summary>
        /// <exception cref="ArgumentNullException">Array is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count are out of range.</exception>
        public static void RequiresListRange(IList array, int offset, int count, string offsetName, string countName) {
            Assert.NotEmpty(offsetName);
            Assert.NotEmpty(countName);
            Assert.NotNull(array);

            if (count < 0) throw new ArgumentOutOfRangeException(countName);
            if (offset < 0 || array.Count - offset < count) throw new ArgumentOutOfRangeException(offsetName);
        }

        public static Exception Unreachable {
            get { return new InvalidOperationException("Unreachable"); }
        }
    }
}
