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

using System;
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Represents extension method.
    /// </summary>
    public class ExtensionMethodTracker : MethodTracker {
        internal ExtensionMethodTracker(MethodInfo method, bool isStatic, Type declaringType)
            : base(method, isStatic) {
            ContractUtils.RequiresNotNull(declaringType, nameof(declaringType));
            DeclaringType = declaringType;
        }

        /// <summary>
        /// Gets the declaring type of the extension method. Since this is an extension method,
        /// the declaring type is in fact the type this extension method extends,
        /// not Method.DeclaringType
        /// </summary>
        public override Type DeclaringType { get; }
    }
}
