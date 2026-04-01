// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Dynamic;
using System.Linq.Expressions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    public abstract class ExtensionBinaryOperationBinder : BinaryOperationBinder {
        private readonly string _operation;

        protected ExtensionBinaryOperationBinder(string operation)
            : base(ExpressionType.Extension) {
            ContractUtils.RequiresNotNull(operation, nameof(operation));
            _operation = operation;
        }

        public string ExtensionOperation => _operation;

        public override int GetHashCode() {
            return base.GetHashCode() ^ _operation.GetHashCode();
        }

        public override bool Equals(object obj) {
            return obj is ExtensionBinaryOperationBinder ebob && base.Equals(obj) && _operation == ebob._operation;
        }
    }
}
