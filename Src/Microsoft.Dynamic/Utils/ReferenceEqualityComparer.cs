// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Dynamic;

namespace Microsoft.Scripting.Utils {
    public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

        private ReferenceEqualityComparer() { }

        public bool Equals(T x, T y) {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
