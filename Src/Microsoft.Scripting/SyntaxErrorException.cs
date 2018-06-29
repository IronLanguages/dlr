// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Dynamic;
using Microsoft.Scripting.Utils;
using System.Security;

namespace Microsoft.Scripting {

    [Serializable]
    public class SyntaxErrorException : Exception {
        private SourceSpan _span;

        private string _sourceCode;
        private string _sourceLine;
        private string _sourcePath;

        private Severity _severity;
        private int _errorCode;

        public SyntaxErrorException() : base() { }

        public SyntaxErrorException(string message) : base(message) { }

        public SyntaxErrorException(string message, Exception innerException)
            : base(message, innerException) {
        }

        public SyntaxErrorException(string message, SourceUnit sourceUnit, SourceSpan span, int errorCode, Severity severity)
            : base(message) {
            ContractUtils.RequiresNotNull(message, nameof(message));

            _span = span;
            _severity = severity;
            _errorCode = errorCode;
            if (sourceUnit != null) {
                _sourcePath = sourceUnit.Path;
                try {
                    _sourceCode = sourceUnit.GetCode();
                    _sourceLine = sourceUnit.GetCodeLine(Line);
                } catch (System.IO.IOException) {
                    // could not get source code.
                }
            }
        }

        public SyntaxErrorException(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity)
            : base(message) {
            ContractUtils.RequiresNotNull(message, nameof(message));

            _span = span;
            _severity = severity;
            _errorCode = errorCode;

            _sourcePath = path;
            _sourceCode = code;
            _sourceLine = line;
        }

#if FEATURE_SERIALIZATION
        protected SyntaxErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context) {

            _span = (SourceSpan)info.GetValue("Span", typeof(SourceSpan));
            _sourceCode = info.GetString("SourceCode");
            _sourcePath = info.GetString("SourcePath");
            _severity = (Severity)info.GetValue("Severity", typeof(Severity));
            _errorCode = info.GetInt32("ErrorCode");
        }

        [SecurityCritical]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            ContractUtils.RequiresNotNull(info, nameof(info));

            base.GetObjectData(info, context);
            info.AddValue("Span", _span);
            info.AddValue("SourceCode", _sourceCode);
            info.AddValue("SourcePath", _sourcePath);
            info.AddValue("Severity", _severity);
            info.AddValue("ErrorCode", _errorCode);
        }
#endif

        /// <summary>
        /// Unmapped span.
        /// </summary>
        public SourceSpan RawSpan {
            get { return _span; }
        }

        public string SourceCode {
            get { return _sourceCode; }
        }

        public string SourcePath {
            get { return _sourcePath; }
        }

        public Severity Severity {
            get { return _severity; }
        }

        public int Line {
            get { return _span.Start.Line; }
        }

        public int Column {
            get { return _span.Start.Column; }
        }

        public int ErrorCode {
            get { return _errorCode; }
        }

        // TODO: fix
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public string GetSymbolDocumentName() {
            return _sourcePath;
        }

        // TODO: fix
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public string GetCodeLine() {
            return _sourceLine;
        }
    }
}