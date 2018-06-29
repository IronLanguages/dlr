// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {

    public class ErrorSink {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ErrorSink/*!*/ Default = new ErrorSink();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ErrorSink/*!*/ Null = new NullErrorSink();

        protected ErrorSink() {
        }

        public virtual void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity) {
            if (severity == Severity.FatalError || severity == Severity.Error) {
                throw new SyntaxErrorException(message, source, span, errorCode, severity);
            }
        }

        /// <summary>
        /// This overload will be called when a SourceUnit is not available. This can happen if the code is being executed remotely,
        /// since SourceUnit cannot be marshaled across AppDomains.
        /// </summary>
        public virtual void Add(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity) {
            if (severity == Severity.FatalError || severity == Severity.Error) {
                throw new SyntaxErrorException(message, path, code, line, span, errorCode, severity);
            }
        }
    }

    internal sealed class NullErrorSink : ErrorSink {

        public override void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity) {
        }
    }

    public class ErrorCounter : ErrorSink {
        private readonly ErrorSink/*!*/ _sink;

        private int _fatalErrorCount;
        private int _errorCount;
        private int _warningCount;

        public int FatalErrorCount {
            get { return _fatalErrorCount; }
        }

        public int ErrorCount {
            get { return _errorCount; }
        }

        public int WarningCount {
            get { return _warningCount; }
        }

        public bool AnyError {
            get {
                return _errorCount > 0 || _fatalErrorCount > 0;
            }
        }

        public ErrorCounter() 
            : this(ErrorSink.Null) {
        }

        public ErrorCounter(ErrorSink/*!*/ sink) {
            ContractUtils.RequiresNotNull(sink, nameof(sink));
            _sink = sink;
        }
        
        protected virtual void CountError(Severity severity) {
            if (severity == Severity.FatalError) Interlocked.Increment(ref _fatalErrorCount);
            else if (severity == Severity.Error) Interlocked.Increment(ref _errorCount);
            else if (severity == Severity.Warning) Interlocked.Increment(ref _warningCount);
        }

        public void ClearCounters() {
            _warningCount = _errorCount = _fatalErrorCount = 0;
        }

        public override void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity) {
            CountError(severity);
            _sink.Add(source, message, span, errorCode, severity);
        }
    }
}
