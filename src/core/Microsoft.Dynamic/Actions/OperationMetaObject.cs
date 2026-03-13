// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using System.Linq.Expressions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    public class OperationMetaObject : DynamicMetaObject {
        public OperationMetaObject(Expression expression, BindingRestrictions restrictions)
            : base(expression, restrictions) {
        }

        public OperationMetaObject(Expression expression, BindingRestrictions restrictions, object value)
            : base(expression, restrictions, value) {
        }

        [Obsolete("Use ExtensionBinaryOperationBinder or ExtensionUnaryOperationBinder")]
        public virtual DynamicMetaObject BindOperation(OperationBinder binder, params DynamicMetaObject[] args) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackOperation(this, args);
        }
    }
}
