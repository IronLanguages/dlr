// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// ArgBuilder provides an argument value used by the MethodBinder.  One ArgBuilder exists for each
    /// physical parameter defined on a method.  
    /// 
    /// Contrast this with ParameterWrapper which represents the logical argument passed to the method.
    /// </summary>
    public abstract class ArgBuilder {
        internal const int AllArguments = -1;

        protected ArgBuilder(ParameterInfo info) {
            ParameterInfo = info;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        public abstract int Priority { get; }

        // can be null, e.g. for ctor return value builder or custom arg builders
        public ParameterInfo ParameterInfo { get; }

        /// <summary>
        /// The number of actual arguments consumed by this builder.
        /// </summary>
        public abstract int ConsumedArgumentCount { get; }

        /// <summary>
        /// Provides the Expression which provides the value to be passed to the argument.
        /// If <c>null</c> is returned the argument is skipped (not passed to the callee).
        /// </summary>
        protected internal abstract Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed);

        /// <summary>
        /// Returns the type required for the argument or null if the ArgBuilder
        /// does not consume a type.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public virtual Type Type => null;

        /// <summary>
        /// Provides an Expression which will update the provided value after a call to the method.  May
        /// return null if no update is required.
        /// </summary>
        internal virtual Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args) {
            return null;
        }

        /// <summary>
        /// If the argument produces a return value (e.g. a ref or out value) this provides
        /// the additional value to be returned.
        /// </summary>
        internal virtual Expression ToReturnExpression(OverloadResolver resolver) {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// An assignable value that is passed to a byref parameter
        /// After the call it will contain the updated value
        /// </summary>
        internal virtual Expression ByRefArgument => null;

        public virtual ArgBuilder Clone(ParameterInfo newType) {
            return null;
        }
    }
}
