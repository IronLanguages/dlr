// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Dynamic;

namespace Microsoft.Scripting.Ast {
    public static partial class Utils {
        private static readonly DefaultExpression VoidInstance = Expression.Empty();

        public static DefaultExpression Empty() {
            return VoidInstance;
        }

        public static DefaultExpression Default(Type type) {
            if (type == typeof(void)) {
                return Empty();
            }
            return Expression.Default(type);
        }
    }
}





