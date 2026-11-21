// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Scripting.Utils {

    // Debug.Assert is not annotated with [DoesNotReturnIf(false)] in netstandard2.0 and net462
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.

    public static class Assert {

        public static Exception Unreachable {
            get {
                Debug.Assert(false, "Unreachable");
                return new InvalidOperationException("Code supposed to be unreachable");
            }
        }

        [Conditional("DEBUG")]
        public static void NotNull([NotNull] object? var) {
            Debug.Assert(var is not null);
        }

        [Conditional("DEBUG")]
        public static void NotNull([NotNull] object? var1, [NotNull] object? var2) {
            Debug.Assert(var1 is not null && var2 is not null);
        }

        [Conditional("DEBUG")]
        public static void NotNull([NotNull] object? var1, [NotNull] object? var2, [NotNull] object? var3) {
            Debug.Assert(var1 is not null && var2 is not null && var3 is not null);
        }

        [Conditional("DEBUG")]
        public static void NotNull([NotNull] object? var1, [NotNull] object? var2, [NotNull] object? var3, [NotNull] object? var4) {
            Debug.Assert(var1 is not null && var2 is not null && var3 is not null && var4 is not null);
        }

        [Conditional("DEBUG")]
        public static void NotEmpty([NotNull] string? str) {
            Debug.Assert(!String.IsNullOrEmpty(str));
        }

        [Conditional("DEBUG")]
        public static void NotEmpty<T>([NotNull] ICollection<T>? array) {
            Debug.Assert(array is not null && array.Count > 0);
        }

        [Conditional("DEBUG")]
        public static void NotNullItems<T>([NotNull] IEnumerable<T>? items) where T : class {
            NotNull(items);
            foreach (object item in items) {
                Debug.Assert(item is not null);
            }
        }

        [Conditional("DEBUG")]
        public static void IsTrue(Func<bool> predicate) {
            ContractUtils.RequiresNotNull(predicate, nameof(predicate));
            Debug.Assert(predicate());
        }
    }
}
