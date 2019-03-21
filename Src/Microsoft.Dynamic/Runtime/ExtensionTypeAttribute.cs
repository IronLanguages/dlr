// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Marks a class in the assembly as being an extension type for another type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class ExtensionTypeAttribute : Attribute {
        /// <summary>
        /// Marks a type in the assembly as being an extension type for another type.
        /// </summary>
        /// <param name="extends">The type which is being extended</param>
        /// <param name="extensionType">The type which provides the extension members.</param>
        public ExtensionTypeAttribute(Type extends, Type extensionType) {
            if (extends == null) {
                throw new ArgumentNullException(nameof(extends));
            }
            if (extensionType != null && !extensionType.IsPublic && !extensionType.IsNestedPublic) {
                throw Error.ExtensionMustBePublic(extensionType.FullName);
            }

            Extends = extends;
            ExtensionType = extensionType;
        }

        /// <summary>
        /// The type which contains extension members which are added to the type being extended.
        /// </summary>
        public Type ExtensionType { get; }

        /// <summary>
        /// The type which is being extended by the extension type.
        /// </summary>
        public Type Extends { get; }
    }
}
