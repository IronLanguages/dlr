﻿
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
#if FEATURE_COM
using System.Linq.Expressions;

using System;

namespace Microsoft.Scripting.ComInterop {

    // Miscellaneous helpers that don't belong anywhere else
    internal static class Helpers {

        internal static Expression Convert(Expression expression, Type type) {
            if (expression.Type == type) {
                return expression;
            }
            return Expression.Convert(expression, type);
        }
    }
}
#endif
