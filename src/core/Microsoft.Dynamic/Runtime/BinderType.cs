// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    public enum BinderType {
        /// <summary>
        /// The MethodBinder will perform normal method binding.
        /// </summary>
        Normal,
        /// <summary>
        /// The MethodBinder will return the languages definition of NotImplemented if the arguments are
        /// incompatible with the signature.
        /// </summary>
        BinaryOperator,
        ComparisonOperator,
        /// <summary>
        /// The MethodBinder will set properties/fields for unused keyword arguments on the instance 
        /// that gets returned from the method.
        /// </summary>
        Constructor
    }
}
