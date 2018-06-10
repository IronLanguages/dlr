/* ****************************************************************************
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
