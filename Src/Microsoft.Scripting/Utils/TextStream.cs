// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Scripting.Utils {
    internal abstract class TextStreamBase : Stream {

        private readonly bool _buffered;

        protected TextStreamBase(bool buffered) {
            _buffered = buffered;
        }

        public abstract Encoding  Encoding { get; }
        public abstract TextReader Reader { get; }
        public abstract TextWriter Writer { get; }

        public sealed override bool CanSeek {
            get { return false; }
        }

        public sealed override bool CanWrite {
            get { return Writer != null; }
        }

        public sealed override bool CanRead {
            get { return Reader != null; }
        }

        public sealed override void Flush() {
            if (!CanWrite) throw new InvalidOperationException();
            Writer.Flush();
        }

        public sealed override int Read(byte[]  buffer, int offset, int count) {
            if (!CanRead) throw new InvalidOperationException();
            ContractUtils.RequiresArrayRange(buffer, offset, count, "offset", "count");

            char[] charBuffer = new char[count];
            int realCount = Reader.Read(charBuffer, 0, count);
            return Encoding.GetBytes(charBuffer, 0, realCount, buffer, offset);
        }

        public sealed override void Write(byte[]  buffer, int offset, int count) {
            ContractUtils.RequiresArrayRange(buffer, offset, count, "offset", "count");
            char[] charBuffer = Encoding.GetChars(buffer, offset, count);
            Writer.Write(charBuffer, 0, charBuffer.Length);
            if (!_buffered) Writer.Flush();
        }

        #region Invalid Operations

        public sealed override long Length {
            get {
                throw new InvalidOperationException();
            }
        }

        public sealed override long Position {
            get {
                throw new InvalidOperationException();
            }
            set {
                throw new InvalidOperationException();
            }
        }

        public sealed override long Seek(long offset, SeekOrigin origin) {
            throw new InvalidOperationException();
        }

        public sealed override void SetLength(long value) {
            throw new InvalidOperationException();
        }

        #endregion
    }

    internal sealed class TextStream : TextStreamBase {

        private readonly TextReader _reader;
        private readonly TextWriter _writer;
        private readonly Encoding  _encoding;

        public override Encoding Encoding {
            get { return _encoding; }
        }

        public override TextReader Reader {
            get { return _reader; }
        }

        public override TextWriter Writer {
            get { return _writer; }
        }

        internal TextStream(TextReader  reader, Encoding  encoding)
            : base(true) {
            ContractUtils.RequiresNotNull(reader, nameof(reader));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));

            _reader = reader;
            _encoding = encoding;
        }

        internal TextStream(TextWriter  writer)
            : this(writer, writer.Encoding, true) {
        }

        internal TextStream(TextWriter  writer, Encoding  encoding, bool buffered)
            : base(buffered) {
            ContractUtils.RequiresNotNull(writer, nameof(writer));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));

            _writer = writer;
            _encoding = encoding;
        }
    }


}
