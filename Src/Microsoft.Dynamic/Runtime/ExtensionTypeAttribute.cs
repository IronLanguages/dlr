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

using System.Linq.Expressions;

using System;
using System.Reflection;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Marks a class in the assembly as being an extension type for another type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class ExtensionTypeAttribute : Attribute {
        private readonly Type _extensionType;

        /// <summary>
        /// Marks a type in the assembly as being an extension type for another type.
        /// </summary>
        /// <param name="extends">The type which is being extended</param>
        /// <param name="extensionType">The type which provides the extension members.</param>
        public ExtensionTypeAttribute(Type extends, Type extensionType) {
            if (extends == null) {
                throw new ArgumentNullException(nameof(extends));
            }
            if (extensionType != null && !extensionType.GetTypeInfo().IsPublic && !extensionType.GetTypeInfo().IsNestedPublic) {
                throw Error.ExtensionMustBePublic(extensionType.FullName);
            }

            Extends = extends;
            _extensionType = extensionType;
        }

        /// <summary>
        /// The type which contains extension members which are added to the type being extended.
        /// </summary>
        public Type ExtensionType {
            get {
                return _extensionType;
            }
        }

        /// <summary>
        /// The type which is being extended by the extension type.
        /// </summary>
        public Type Extends { get; }
    }

}
