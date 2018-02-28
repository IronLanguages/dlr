// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING && FEATURE_FULL_CONSOLE

using System;
using System.Diagnostics;
using System.Threading;
using System.Security.Permissions;

namespace Microsoft.Scripting.Hosting.Shell.Remote {
    /// <summary>
    /// This allows the RemoteConsoleHost to abort a long-running operation. The RemoteConsoleHost itself
    /// does not know which ThreadPool thread might be processing the remote call, and so it needs
    /// cooperation from the remote runtime server.
    /// </summary>
    public class RemoteCommandDispatcher : MarshalByRefObject, ICommandDispatcher {
        /// <summary>
        /// Since OnOutputDataReceived is sent async, it can arrive late. The remote console
        /// cannot know if all output from the current command has been received. So
        /// RemoteCommandDispatcher writes out a marker to indicate the end of the output
        /// </summary>
        internal const string OutputCompleteMarker = "{7FF032BB-DB03-4255-89DE-641CA195E5FA}";

        private Thread _executingThread;

        public RemoteCommandDispatcher(ScriptScope scope) {
            ScriptScope = scope;
        }

        public ScriptScope ScriptScope { get; }

        public object Execute(CompiledCode compiledCode, ScriptScope scope) {
            Debug.Assert(_executingThread == null);
            _executingThread = Thread.CurrentThread;

            try {
                object result = compiledCode.Execute(scope);

                Console.WriteLine(OutputCompleteMarker);

                return result;
            } catch (ThreadAbortException tae) {
                if (tae.ExceptionState is KeyboardInterruptException pki) {
                    // Most exceptions get propagated back to the client. However, ThreadAbortException is handled
                    // differently by the remoting infrastructure, and gets wrapped in a RemotingException
                    // ("An error occurred while processing the request on the server"). So we filter it out
                    // and raise the KeyboardInterruptException
                    Thread.ResetAbort();
                    throw pki;
                } else {
                    throw;
                }
            } finally {
                _executingThread = null;
            }
        }

        /// <summary>
        /// Aborts the current active call to Execute by doing Thread.Abort
        /// </summary>
        /// <returns>true if a Thread.Abort was actually called. false if there is no active call to Execute</returns>
        public bool AbortCommand() {
            Thread executingThread = _executingThread;
            if (executingThread == null) {
                return false;
            }

            executingThread.Abort(new KeyboardInterruptException(""));
            return true;
        }

        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
    }
}

#endif