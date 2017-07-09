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
