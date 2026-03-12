// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.IO;
using System.Text;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Provides host-redirectable IO streams used by DLR languages for default IO.
    /// </summary>
    public sealed class ScriptIO : MarshalByRefObject {
        public Stream InputStream => SharedIO.InputStream;
        public Stream OutputStream => SharedIO.OutputStream;
        public Stream ErrorStream => SharedIO.ErrorStream;

        public TextReader InputReader => SharedIO.InputReader;
        public TextWriter OutputWriter => SharedIO.OutputWriter;
        public TextWriter ErrorWriter => SharedIO.ErrorWriter;

        public Encoding InputEncoding => SharedIO.InputEncoding;
        public Encoding OutputEncoding => SharedIO.OutputEncoding;
        public Encoding ErrorEncoding => SharedIO.ErrorEncoding;

        internal SharedIO SharedIO { get; }

        internal ScriptIO(SharedIO io) {
            Assert.NotNull(io);
            SharedIO = io;
        }

        /// <summary>
        /// Used if the host stores the output as binary data.
        /// </summary>
        /// <param name="stream">Binary stream to write data to.</param>
        /// <param name="encoding">Encoding used to convert textual data written to the output by the script.</param>
        public void SetOutput(Stream stream, Encoding encoding) {
            ContractUtils.RequiresNotNull(stream, nameof(stream));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));
            SharedIO.SetOutput(stream, new StreamWriter(stream, encoding));
        }

        /// <summary>
        /// Used if the host handles both kinds of data (textual and binary) by itself.
        /// </summary>
        public void SetOutput(Stream stream, TextWriter writer) {
            ContractUtils.RequiresNotNull(stream, nameof(stream));
            ContractUtils.RequiresNotNull(writer, nameof(writer));
            SharedIO.SetOutput(stream, writer);
        }

        public void SetErrorOutput(Stream stream, Encoding encoding) {
            ContractUtils.RequiresNotNull(stream, nameof(stream));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));
            SharedIO.SetErrorOutput(stream, new StreamWriter(stream, encoding));
        }

        public void SetErrorOutput(Stream stream, TextWriter writer) {
            ContractUtils.RequiresNotNull(stream, nameof(stream));
            ContractUtils.RequiresNotNull(writer, nameof(writer));
            SharedIO.SetErrorOutput(stream, writer);
        }

        public void SetInput(Stream stream, Encoding encoding) {
            ContractUtils.RequiresNotNull(stream, nameof(stream));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));
            SharedIO.SetInput(stream, new StreamReader(stream, encoding), encoding);
        }

        public void SetInput(Stream stream, TextReader reader, Encoding encoding) {
            ContractUtils.RequiresNotNull(stream, nameof(stream));
            ContractUtils.RequiresNotNull(reader, nameof(reader));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));
            SharedIO.SetInput(stream, reader, encoding);
        }

        public void RedirectToConsole() {
            SharedIO.RedirectToConsole();
        }

#if FEATURE_REMOTING
        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
    }
}
