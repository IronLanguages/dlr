// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// A MetaObject which was produced as the result of a failed binding.
    /// </summary>
    public sealed class ErrorMetaObject : DynamicMetaObject {
        public ErrorMetaObject(Expression body, BindingRestrictions restrictions)
            : base(body, restrictions) {
        }
    }
}
