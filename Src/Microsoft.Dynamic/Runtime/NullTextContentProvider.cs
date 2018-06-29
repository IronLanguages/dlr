// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// A NullTextContentProvider to be provided when we have a pre-compiled ScriptCode which doesn't
    /// have source code associated with it.
    /// </summary>
    public sealed class NullTextContentProvider : TextContentProvider {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly NullTextContentProvider Null = new NullTextContentProvider();

        private NullTextContentProvider() {
        }

        public override SourceCodeReader GetReader() {
            return SourceCodeReader.Null;
        }
    }
}
