// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    public static partial class Utils {
        public static NewExpression SimpleNewHelper(ConstructorInfo constructor, params Expression[] arguments) {
            ContractUtils.RequiresNotNull(constructor, nameof(constructor));
            ContractUtils.RequiresNotNullItems(arguments, nameof(arguments));

            ParameterInfo[] parameters = constructor.GetParameters();
            ContractUtils.Requires(arguments.Length == parameters.Length, nameof(arguments), "Incorrect number of arguments");

            return Expression.New(constructor, ArgumentConvertHelper(arguments, parameters));
        }
    }
}
