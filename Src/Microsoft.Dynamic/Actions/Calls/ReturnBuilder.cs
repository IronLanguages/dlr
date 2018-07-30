// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions.Calls {

    internal class ReturnBuilder {
        /// <summary>
        /// Creates a ReturnBuilder
        /// </summary>
        /// <param name="returnType">the type the ReturnBuilder will leave on the stack</param>
        public ReturnBuilder(Type returnType) {
            Debug.Assert(returnType != null);

            ReturnType = returnType;
        }

        internal virtual Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret) {
            return ret;
        }

        public virtual int CountOutParams => 0;

        public Type ReturnType { get; }
    }
}
