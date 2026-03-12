// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Dynamic;

using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Runtime {
    public class RestrictedMetaObject : DynamicMetaObject, IRestrictedMetaObject {
        public RestrictedMetaObject(Expression expression, BindingRestrictions restriction, object value)  : base(expression, restriction, value) {
        }

        public RestrictedMetaObject(Expression expression, BindingRestrictions restriction)
            : base(expression, restriction) {
        }

        #region IRestrictedMetaObject Members

        public DynamicMetaObject Restrict(Type type) {
            if (type == LimitType) {
                return this;
            }

            if (HasValue) {
                return new RestrictedMetaObject(
                    AstUtils.Convert(Expression, type),
                    BindingRestrictionsHelpers.GetRuntimeTypeRestriction(Expression, type),
                    Value
                );
            }

            return new RestrictedMetaObject(
                AstUtils.Convert(Expression, type),
                BindingRestrictionsHelpers.GetRuntimeTypeRestriction(Expression, type)
            );
        }

        #endregion
    }
}
