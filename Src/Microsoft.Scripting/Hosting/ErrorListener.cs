// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

namespace Microsoft.Scripting.Hosting {

    /// <summary>
    /// The host can use this class to track for errors reported during script parsing and compilation.
    /// Hosting API counterpart for <see cref="ErrorSink"/>.
    /// </summary>
    public abstract class ErrorListener : MarshalByRefObject {
        protected ErrorListener() {
        }

        internal void ReportError(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity) {
            ErrorReported(source, message, span, errorCode, severity);
        }

        public abstract void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity);

#if FEATURE_REMOTING
        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
    }
}
