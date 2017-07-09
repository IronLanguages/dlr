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

using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        public virtual int CountOutParams {
            get { return 0; }
        }

        public Type ReturnType { get; }
    }
}
