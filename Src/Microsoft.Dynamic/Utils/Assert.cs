// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Utils {

    public static class Assert {

        public static Exception Unreachable {
            get {
                Debug.Assert(false, "Unreachable");
                return new InvalidOperationException("Code supposed to be unreachable");
            }
        }

        [Conditional("DEBUG")]
        public static void NotNull(object var) {
            Debug.Assert(var != null);
        }

        [Conditional("DEBUG")]
        public static void NotNull(object var1, object var2) {
            Debug.Assert(var1 != null && var2 != null);
        }

        [Conditional("DEBUG")]
        public static void NotNull(object var1, object var2, object var3) {
            Debug.Assert(var1 != null && var2 != null && var3 != null);
        }

        [Conditional("DEBUG")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
        public static void NotNull(object var1, object var2, object var3, object var4) {
            Debug.Assert(var1 != null && var2 != null && var3 != null && var4 != null);
        }

        [Conditional("DEBUG")]
        public static void NotEmpty(string str) {
            Debug.Assert(!String.IsNullOrEmpty(str));
        }

        [Conditional("DEBUG")]
        public static void NotEmpty<T>(ICollection<T> array) {
            Debug.Assert(array != null && array.Count > 0);
        }

        [Conditional("DEBUG")]
        public static void NotNullItems<T>(IEnumerable<T> items) where T : class {
            Debug.Assert(items != null);
            foreach (object item in items) {
                Debug.Assert(item != null);
            }
        }

        [Conditional("DEBUG")]
        public static void IsTrue(Func<bool> predicate) {
            ContractUtils.RequiresNotNull(predicate, nameof(predicate));
            Debug.Assert(predicate());
        }
    }
}
