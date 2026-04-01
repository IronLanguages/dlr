// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Internal class which binds a LanguageContext, StreamContentProvider, and Encoding together to produce
    /// a TextContentProvider which reads binary data with the correct language semantics.
    /// </summary>
    internal sealed class LanguageBoundTextContentProvider : TextContentProvider {
        private readonly LanguageContext _context;
        private readonly StreamContentProvider _streamProvider;
        private readonly Encoding _defaultEncoding;
        private readonly string _path;

        public LanguageBoundTextContentProvider(LanguageContext context, StreamContentProvider streamProvider, Encoding defaultEncoding, string path) {
            Assert.NotNull(context, streamProvider, defaultEncoding);
            _context = context;
            _streamProvider = streamProvider;
            _defaultEncoding = defaultEncoding;
            _path = path;
        }

        public override SourceCodeReader GetReader() {
            Stream stream = _streamProvider.GetStream();
            try {
                return _context.GetSourceReader(stream, _defaultEncoding, _path);
            }
            catch {
                stream.Dispose();
                throw;
            }
        }
    }
}
