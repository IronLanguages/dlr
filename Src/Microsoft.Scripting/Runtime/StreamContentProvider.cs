// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.Scripting {
    /// <summary>
    /// Provides a factory to create streams over one source of binary content.  
    /// 
    /// StreamContentProvider's are used when opening a file of an unknown encoding.  The
    /// StreamContentProvider will be wrapped in a TextContentProvider provided by the language
    /// which can support a language specific way of interpreting the binary data into text. 
    /// 
    /// For example some languages allow a marker at the beginning of the file which specifies
    /// the encoding of the rest of the file.
    /// </summary>
    [Serializable]
    public abstract class StreamContentProvider {
        /// <summary>
        /// Creates a new Stream which is backed by the content the StreamContentProvider was created for.
        /// 
        /// For example if the StreamContentProvider was backing a file then GetStream re-opens the file and returns
        /// the new stream.
        /// 
        /// This method may be called multiple times.  For example once to compile the code and again to get
        /// the source code to display error messages.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public abstract Stream GetStream();
    }
}
