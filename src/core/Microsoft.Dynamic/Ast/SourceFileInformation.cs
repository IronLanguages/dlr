// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
namespace Microsoft.Scripting {
    /// <summary>
    /// Stores information needed to emit debugging symbol information for a
    /// source file, in particular the file name and unique language identifier
    /// </summary>
    public sealed class SourceFileInformation {

        public SourceFileInformation(string fileName) {
            FileName = fileName;
        }

        public SourceFileInformation(string fileName, Guid language) {
            FileName = fileName;
            LanguageGuid = language;
        }

        public SourceFileInformation(string fileName, Guid language, Guid vendor) {
            FileName = fileName;
            LanguageGuid = language;
            VendorGuid = vendor;
        }

        /// <summary>
        /// Gets the source file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the language's unique identifier, if any.
        /// </summary>
        public Guid LanguageGuid { get; }

        /// <summary>
        /// Gets the language vendor's unique identifier, if any.
        /// </summary>
        public Guid VendorGuid { get; }
    }
}
