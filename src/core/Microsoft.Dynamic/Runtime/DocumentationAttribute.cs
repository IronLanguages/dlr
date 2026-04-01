// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Provides a mechanism for providing documentation stored in an assembly as metadata.  
    /// 
    /// Applying this attribute will enable documentation to be provided to the user at run-time
    /// even if XML Documentation files are unavailable.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class DocumentationAttribute : Attribute {
        public DocumentationAttribute(string documentation) {
            Documentation = documentation;
        }

        public string Documentation { get; }
    }
}
