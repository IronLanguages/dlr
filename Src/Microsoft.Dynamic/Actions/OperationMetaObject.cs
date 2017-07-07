﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Linq.Expressions;

using System;
using System.Dynamic;
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
            ContractUtils.RequiresNotNull(binder, "binder");
            return binder.FallbackOperation(this, args);
        }
    }
}
