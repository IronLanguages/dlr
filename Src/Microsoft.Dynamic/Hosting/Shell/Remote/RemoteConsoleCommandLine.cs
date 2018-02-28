// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING && FEATURE_FULL_CONSOLE

using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Threading;

namespace Microsoft.Scripting.Hosting.Shell.Remote {
    /// <summary>
    /// Customize the CommandLine for remote scenarios
    /// </summary>
    public class RemoteConsoleCommandLine : CommandLine {
        private RemoteConsoleCommandDispatcher _remoteConsoleCommandDispatcher;

        public RemoteConsoleCommandLine(ScriptScope scope, RemoteCommandDispatcher remoteCommandDispatcher, AutoResetEvent remoteOutputReceived) {
            _remoteConsoleCommandDispatcher = new RemoteConsoleCommandDispatcher(remoteCommandDispatcher, remoteOutputReceived);
            Debug.Assert(scope != null);
            ScriptScope = scope;
        }

        protected override ICommandDispatcher CreateCommandDispatcher() {
            return _remoteConsoleCommandDispatcher;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void UnhandledExceptionWorker(Exception e) {
            try {
                base.UnhandledException(e);
            } catch (Exception exceptionDuringHandling) {
                // All bets are off while accessing the remote runtime. So we catch all exceptions.
                // However, in most cases, we only expect to see RemotingException here.
                if (!(exceptionDuringHandling is RemotingException)) {
                    Console.WriteLine(
                        $"({exceptionDuringHandling.GetType()} thrown while trying to display unhandled exception)", Style.Error);
                }

                // The remote server may have shutdown. So just do something simple
                Console.WriteLine(e.ToString(), Style.Error);
            }
        }

        protected override void UnhandledException(Exception e) {
            UnhandledExceptionWorker(e);
        }

        /// <summary>
        /// CommandDispatcher to ensure synchronize output from the remote runtime
        /// </summary>
        class RemoteConsoleCommandDispatcher : ICommandDispatcher {
            private RemoteCommandDispatcher _remoteCommandDispatcher;
            private AutoResetEvent _remoteOutputReceived;

            internal RemoteConsoleCommandDispatcher(RemoteCommandDispatcher remoteCommandDispatcher, AutoResetEvent remoteOutputReceived) {
                _remoteCommandDispatcher = remoteCommandDispatcher;
                _remoteOutputReceived = remoteOutputReceived;
            }

            public object Execute(CompiledCode compiledCode, ScriptScope scope) {
                // Delegate the operation to the RemoteCommandDispatcher which will execute the code in the remote runtime
                object result = _remoteCommandDispatcher.Execute(compiledCode, scope);

                // Output is received async, and so we need explicit synchronization in the remote console
                _remoteOutputReceived.WaitOne();

                return result;
            }
        }
    }
}

#endif