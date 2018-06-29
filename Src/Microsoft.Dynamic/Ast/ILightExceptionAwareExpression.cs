// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    /// Implemented by expressions which can provide a version which is aware of light exceptions.  
    /// 
    /// Normally these expressions will simply reduce to a version which throws a real exception.
    /// When the expression is used inside of a region of code which supports light exceptions
    /// the light exception re-writer will call ReduceForLightExceptions.  The expression can
    /// then return a new expression which can return a light exception rather than throwing
    /// a real .NET exception.
    /// </summary>
    public interface ILightExceptionAwareExpression {
        Expression ReduceForLightExceptions();
    }
}
