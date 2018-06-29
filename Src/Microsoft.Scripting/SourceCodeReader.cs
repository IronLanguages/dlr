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

using System.IO;
using Microsoft.Scripting.Utils;
using System.Text;
using System;

namespace Microsoft.Scripting {

    /// <summary>
    /// Source code reader.
    /// </summary>    
    public class SourceCodeReader : TextReader {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static new readonly SourceCodeReader Null = new SourceCodeReader(TextReader.Null, null);

        public SourceCodeReader(TextReader textReader, Encoding encoding) {
            ContractUtils.RequiresNotNull(textReader, nameof(textReader));

            Encoding = encoding;
            BaseReader = textReader;
        }

        /// <summary>
        /// Gets the encoding that is used by the reader to convert binary data read from an underlying binary stream.
        /// <c>Null</c> if the reader is reading from a textual source (not performing any byte to character transcoding).
        /// </summary>
        public Encoding Encoding { get; }

        public TextReader BaseReader { get; }

        public override string ReadLine() {
            return BaseReader.ReadLine();
        }

        /// <summary>
        /// Seeks the first character of a specified line in the text stream.
        /// </summary>
        /// <param name="line">Line number. The current position is assumed to be line #1.</param>
        /// <returns>
        /// Returns <c>true</c> if the line is found, <b>false</b> otherwise.
        /// </returns>
        public virtual bool SeekLine(int line) {
            if (line < 1) throw new ArgumentOutOfRangeException(nameof(line));
            if (line == 1) return true;

            int current_line = 1;

            for (; ; ) {
                int c = BaseReader.Read();

                if (c == '\r') {
                    if (BaseReader.Peek() == '\n') {
                        BaseReader.Read();
                    }

                    current_line++;
                    if (current_line == line) return true;

                } else if (c == '\n') {
                    current_line++;
                    if (current_line == line) return true;
                } else if (c == -1) {
                    return false;
                }
            }
        }

        public override string ReadToEnd() {
            return BaseReader.ReadToEnd();
        }

        public override int Read(char[] buffer, int index, int count) {
            return BaseReader.Read(buffer, index, count);
        }

        public override int Peek() {
            return BaseReader.Peek();
        }

        public override int Read() {
            return BaseReader.Read();
        }

        protected override void Dispose(bool disposing) {
            BaseReader.Dispose();
            base.Dispose(disposing);
        }
    }
}
