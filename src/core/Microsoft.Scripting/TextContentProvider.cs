// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Scripting {

    /// <summary>
    /// Provides a factory to create TextReader's over one source of textual content.
    /// 
    /// TextContentProvider's are used when reading from a source which is already decoded
    /// or has a known specific decoding.  
    /// 
    /// For example a text editor might provide a TextContentProvider whose backing is
    /// an in-memory text buffer that the user can actively edit.
    /// </summary>
    [Serializable]
    public abstract class TextContentProvider {

        /// <summary>
        /// Creates a new TextReader which is backed by the content the TextContentProvider was created for.
        /// 
        /// This method may be called multiple times.  For example once to compile the code and again to get
        /// the source code to display error messages.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public abstract SourceCodeReader GetReader();
    }
}
