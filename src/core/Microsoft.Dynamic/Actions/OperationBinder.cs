// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    [Obsolete("Use ExtensionBinaryOperationBinder or ExtensionUnaryOperationBinder")]
    public abstract class OperationBinder : DynamicMetaObjectBinder {

        protected OperationBinder(string operation) {
            Operation = operation;
        }

        public string Operation { get; }

        public DynamicMetaObject FallbackOperation(DynamicMetaObject target, DynamicMetaObject[] args) {
            return FallbackOperation(target, args, null);
        }

        public abstract DynamicMetaObject FallbackOperation(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion);

        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args) {
            ContractUtils.RequiresNotNull(target, nameof(target));
            ContractUtils.RequiresNotNullItems(args, nameof(args));

            // Try to call BindOperation
            if (target is OperationMetaObject emo) {
                return emo.BindOperation(this, args);
            }

            // Otherwise just fall back (it's as if they didn't override BindOperation)
            return FallbackOperation(target, args);
        }

        public override bool Equals(object obj) =>
            obj is OperationBinder oa && oa.Operation == Operation;

        public override int GetHashCode() {
            return 0x10000000 ^ Operation.GetHashCode();
        }
    }
}
