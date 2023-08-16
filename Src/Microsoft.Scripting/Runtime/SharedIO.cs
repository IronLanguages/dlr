﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    public sealed class SharedIO {

        public enum SupportLevel {
            None,
            Basic,
            Full,
        }

        // prevents this object from transitions to an inconsistent state, doesn't sync output or input:
        private readonly object _mutex = new object();

        #region Proxies

        private sealed class StreamProxy : Stream {
            private readonly ConsoleStreamType _type;
            private readonly SharedIO _io;

            public StreamProxy(SharedIO io, ConsoleStreamType type) {
                Assert.NotNull(io);
                _io = io;
                _type = type;
            }

            public override bool CanRead => _type == ConsoleStreamType.Input;

            public override bool CanSeek => false;

            public override bool CanWrite => !CanRead;

            public override void Flush() {
                _io.GetStream(_type).Flush();
            }

            public override int Read(byte[] buffer, int offset, int count) {
                return _io.GetStream(_type).Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count) {
                _io.GetStream(_type).Write(buffer, offset, count);
            }

            public override long Length => throw new NotSupportedException();

            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin) {
                throw new NotSupportedException();
            }

            public override void SetLength(long value) {
                throw new NotSupportedException();
            }
        }

        #endregion

        // lazily initialized to Console by default:
        private Stream _inputStream;
        private Stream _outputStream;
        private Stream _errorStream;

        private TextReader _inputReader;
        private TextWriter _outputWriter;
        private TextWriter _errorWriter;

        private Encoding _inputEncoding;

        public Stream InputStream { get { InitializeInput(); return _inputStream; } }
        public Stream OutputStream { get { InitializeOutput(); return _outputStream; } }
        public Stream ErrorStream { get { InitializeErrorOutput(); return _errorStream; } }

        public TextReader InputReader { get { InitializeInput(); return _inputReader; } }
        public TextWriter OutputWriter { get { InitializeOutput(); return _outputWriter; } }
        public TextWriter ErrorWriter { get { InitializeErrorOutput(); return _errorWriter; } }

        public Encoding InputEncoding { get { InitializeInput(); return _inputEncoding; } }
        public Encoding OutputEncoding { get { InitializeOutput(); return _outputWriter.Encoding; } }
        public Encoding ErrorEncoding { get { InitializeErrorOutput(); return _errorWriter.Encoding; } }

        public SupportLevel ConsoleSupportLevel { get; set; } = SupportLevel.Full;

        internal SharedIO() {
        }

        private void InitializeInput() {
            if (_inputStream == null) {
                lock (_mutex) {
                    if (_inputStream == null) {
                        switch (ConsoleSupportLevel) {
                            case SupportLevel.Full:
                                _inputEncoding = Console.InputEncoding;
                                _inputStream = ConsoleInputStream.Instance;
                                _inputReader = Console.In;
                                break;
                            case SupportLevel.Basic:
                                _inputEncoding = Encoding.UTF8;
                                _inputStream = new TextStream(Console.In, _inputEncoding);
                                _inputReader = Console.In;
                                break;
                            case SupportLevel.None:
                            default:
                                _inputEncoding = Encoding.UTF8;
                                _inputStream = Stream.Null;
                                _inputReader = TextReader.Null;
                                break;
                        }
                    }
                }
            }
        }

        private void InitializeOutput() {
            if (_outputStream == null) {
                lock (_mutex) {
                    if (_outputStream == null) {
                        switch (ConsoleSupportLevel) {
                            case SupportLevel.Full:
                                _outputStream = Console.OpenStandardOutput();
                                _outputWriter = Console.Out;
                                break;
                            case SupportLevel.Basic:
                                _outputStream = new TextStream(Console.Out);
                                _outputWriter = Console.Out;
                                break;
                            case SupportLevel.None:
                            default:
                                _outputStream = Stream.Null;
                                _outputWriter = new StreamWriter(_outputStream, Encoding.UTF8, bufferSize: 16);
                                break;
                        }
                    }
                }
            }
        }

        private void InitializeErrorOutput() {
            if (_errorStream == null) {
                lock (_mutex) {
                    if (_errorStream == null) {
                        switch (ConsoleSupportLevel) {
                            case SupportLevel.Full:
                                _errorStream = Console.OpenStandardError();
                                _errorWriter = Console.Error;
                                break;
                            case SupportLevel.Basic:
                                _errorStream = new TextStream(Console.Error);
                                _errorWriter = Console.Error;
                                break;
                            case SupportLevel.None:
                            default:
                                _errorStream = Stream.Null;
                                _errorWriter = new StreamWriter(_errorStream, Encoding.UTF8, bufferSize: 16);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Only host should redirect I/O.
        /// </summary>
        public void SetOutput(Stream stream, TextWriter writer) {
            Assert.NotNull(stream, writer);
            lock (_mutex) {
                _outputStream = stream;
                _outputWriter = writer;
            }
        }

        public void SetErrorOutput(Stream stream, TextWriter writer) {
            Assert.NotNull(stream, writer);
            lock (_mutex) {
                _errorStream = stream;
                _errorWriter = writer;
            }
        }

        public void SetInput(Stream stream, TextReader reader, Encoding encoding) {
            Assert.NotNull(stream, reader, encoding);
            lock (_mutex) {
                _inputStream = stream;
                _inputReader = reader;
                _inputEncoding = encoding;
            }
        }

        public void RedirectToConsole() {
            lock (_mutex) {
                _inputEncoding = null;
                _inputStream = null;
                _outputStream = null;
                _errorStream = null;
                _inputReader = null;
                _outputWriter = null;
                _errorWriter = null;
            }
        }

        public Stream GetStream(ConsoleStreamType type) {
            switch (type) {
                case ConsoleStreamType.Input: return InputStream;
                case ConsoleStreamType.Output: return OutputStream;
                case ConsoleStreamType.ErrorOutput: return ErrorStream;
            }
            throw Error.InvalidStreamType(type);
        }

        public TextWriter GetWriter(ConsoleStreamType type) {
            switch (type) {
                case ConsoleStreamType.Output: return OutputWriter;
                case ConsoleStreamType.ErrorOutput: return ErrorWriter;
            }
            throw Error.InvalidStreamType(type);
        }

        public Encoding GetEncoding(ConsoleStreamType type) {
            switch (type) {
                case ConsoleStreamType.Input: return InputEncoding;
                case ConsoleStreamType.Output: return OutputEncoding;
                case ConsoleStreamType.ErrorOutput: return ErrorEncoding;
            }
            throw Error.InvalidStreamType(type);
        }

        public TextReader GetReader(out Encoding encoding) {
            TextReader reader;
            lock (_mutex) {
                reader = InputReader;
                encoding = InputEncoding;
            }
            return reader;
        }

        public Stream GetStreamProxy(ConsoleStreamType type) {
            return new StreamProxy(this, type);
        }
    }
}
