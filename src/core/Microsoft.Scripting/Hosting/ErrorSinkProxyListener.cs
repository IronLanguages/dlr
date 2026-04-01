// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Bridges ErrorListener and ErrorSink. It provides the reverse functionality as ErrorSinkProxyListener
    /// </summary>
    public sealed class ErrorSinkProxyListener : ErrorListener {
        private readonly ErrorSink _errorSink;

        public ErrorSinkProxyListener(ErrorSink errorSink) {
            _errorSink = errorSink;
        }

        public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity) {
            // Note that we cannot use "source.SourceUnit" since "source" may be a proxy object, and we will not be able to marshall 
            // "source.SourceUnit" to the current AppDomain

            string code = null;
            string line = null;
            try {
                code = source.GetCode();
                line = source.GetCodeLine(span.Start.Line);
            } catch (System.IO.IOException) {
                // could not get source code.
            }

            _errorSink.Add(message, source.Path, code, line, span, errorCode, severity);
        }
    }
}
