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

namespace Microsoft.Scripting.ComInterop {
    /// <summary>
    /// ArgBuilder provides an argument value used by the MethodBinder.  One ArgBuilder exists for each
    /// physical parameter defined on a method.  
    /// 
    /// Contrast this with ParameterWrapper which represents the logical argument passed to the method.
    /// </summary>
    internal abstract class ArgBuilder {
        /// <summary>
        /// Provides the Expression which provides the value to be passed to the argument.
        /// </summary>
        internal abstract Expression Marshal(Expression parameter);

        /// <summary>
        /// Provides the Expression which provides the value to be passed to the argument.
        /// This method is called when result is intended to be used ByRef.
        /// </summary>
        internal virtual Expression MarshalToRef(Expression parameter) {
            return Marshal(parameter);
        }

        /// <summary>
        /// Provides an Expression which will update the provided value after a call to the method.
        /// May return null if no update is required.
        /// </summary>
        internal virtual Expression UnmarshalFromRef(Expression newValue) {
            return newValue;
        }
    }
}

#endif
