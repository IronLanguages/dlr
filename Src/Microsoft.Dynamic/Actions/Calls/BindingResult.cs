// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// Indicates the specific type of failure, if any, from binding to a method.
    /// </summary>
    public enum BindingResult {
        /// <summary>
        /// The binding succeeded.  Only one method was applicable or had the best conversion.  
        /// </summary>
        Success,

        /// <summary>
        /// More than one method was applicable for the provided parameters and no method was considered the best.
        /// </summary>
        AmbiguousMatch,

        /// <summary>
        /// There are no overloads that match the number of parameters required for the call
        /// </summary>
        IncorrectArgumentCount,

        /// <summary>
        /// None of the target method(s) can successfully be called.  The failure can be due to:
        ///     1. Arguments could not be successfully converted for the call
        ///     2. Keyword arguments could not be assigned to positional arguments
        ///     3. Keyword arguments could be assigned but would result in an argument being assigned 
        ///        multiple times (keyword and positional arguments conflit or dupliate keyword arguments).
        /// </summary>
        CallFailure,

        /// <summary>
        /// Actual arguments cannot be constructed.
        /// </summary>
        InvalidArguments,

        /// <summary>
        /// No method is callable. For example, all methods have an unbound generic parameter.
        /// </summary>
        NoCallableMethod,
    }
}
