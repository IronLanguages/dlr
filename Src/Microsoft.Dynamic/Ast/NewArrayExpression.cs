// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    public static partial class Utils {
        public static NewArrayExpression NewArrayHelper(Type type, IEnumerable<Expression> initializers) {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));

            if (type.Equals(typeof(void))) {
                throw new ArgumentException("Argument type cannot be System.Void.");
            }

            ReadOnlyCollection<Expression> initializerList = initializers.ToReadOnly();

            Expression[] clone = null;
            for (int i = 0; i < initializerList.Count; i++) {
                Expression initializer = initializerList[i];
                ContractUtils.RequiresNotNull(initializer, nameof(initializers));

                if (!TypeUtils.AreReferenceAssignable(type, initializer.Type)) {
                    if (clone == null) {
                        clone = new Expression[initializerList.Count];
                        for (int j = 0; j < i; j++) {
                            clone[j] = initializerList[j];
                        }
                    }
                    if (type.IsSubclassOf(typeof(Expression)) && TypeUtils.AreAssignable(type, initializer.GetType())) {
                        initializer = Expression.Quote(initializer);
                    } else {
                        initializer = Convert(initializer, type);
                    }

                }
                if (clone != null) {
                    clone[i] = initializer;
                }
            }

            if (clone != null) {
                initializerList = new ReadOnlyCollection<Expression>(clone);
            }

            return Expression.NewArrayInit(type, initializerList);
        }
    }
}
