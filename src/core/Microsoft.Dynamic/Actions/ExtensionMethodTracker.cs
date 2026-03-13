// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
