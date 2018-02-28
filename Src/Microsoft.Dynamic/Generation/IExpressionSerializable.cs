// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Enables an object to be serializable to an Expression tree.  The expression tree can then
    /// be emitted into an assembly enabling the de-serialization of the object.
    /// </summary>
    public interface IExpressionSerializable {
        Expression CreateExpression();
    }
}
