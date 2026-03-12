// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {

    public sealed class IfStatementTest {
        internal IfStatementTest(Expression test, Expression body) {
            Test = test;
            Body = body;
        }

        public Expression Test { get; }

        public Expression Body { get; }
    }

    public partial class Utils {
        public static IfStatementTest IfCondition(Expression test, Expression body) {
            ContractUtils.RequiresNotNull(test, nameof(test));
            ContractUtils.RequiresNotNull(body, nameof(body));
            ContractUtils.Requires(test.Type == typeof(bool), nameof(test), "Test must be boolean");

            return new IfStatementTest(test, body);
        }
    }
}
