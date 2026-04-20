// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Hosting;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Scripting.Generation;


namespace HostingTest {
    public class HAPITestBase {

        static internal PreDefinedCodeSnippets _codeSnippets;
//      static string _testLanguage;

        //Don't use this member. Use _runtime instead. 
        //This will be deleted once all the files are switched over to the id with correct casing
        protected ScriptRuntime _runTime;
        protected ScriptRuntime _runtime;
#if FEATURE_REMOTING
        protected ScriptRuntime _remoteRuntime;
#endif

        protected ScriptEngine _testEng;

        protected ScriptEngine _PYEng;
        protected ScriptEngine _RBEng;

        protected ScriptScope _defaultScope;

        protected HAPITestBase() {
            
            var ses = CreateSetup();
            ses.HostType = typeof(TestHost);
            _runtime = new ScriptRuntime(ses);
#if FEATURE_REMOTING
            _remoteRuntime = ScriptRuntime.CreateRemote(TestHelpers.CreateAppDomain("Alternate"), ses);
#endif

            _runTime = _runtime;// _remoteRuntime;

            _PYEng = _runTime.GetEngine("py");
            _RBEng = TryGetEngine(_runTime, "rb");

            AddSearchPaths(_PYEng);

            SetTestLanguage();
            
            _defaultScope = _runTime.CreateScope();
            _codeSnippets = new PreDefinedCodeSnippets();
        }

        /// <summary>
        /// Try to get a script engine by name. Returns null if not available.
        /// </summary>
        private static ScriptEngine TryGetEngine(ScriptRuntime runtime, string name) {
            try {
                return runtime.GetEngine(name);
            } catch (ArgumentException) {
                return null;
            }
        }

        /// <summary>
        /// Returns true if IronRuby is configured and available.
        /// </summary>
        internal protected bool IsRubyAvailable => _RBEng != null;

        public static ScriptRuntime CreateRuntime() {
            return new ScriptRuntime(CreateSetup());
        }

#if FEATURE_REMOTING
        public static ScriptRuntime CreateRemoteRuntime(AppDomain domain) {
            return ScriptRuntime.CreateRemote(domain, CreateSetup());
        }
#endif

        public static ScriptRuntimeSetup CreateSetup() {
            var configFile = TestHelpers.StandardConfigFile;
            Debug.Assert(File.Exists(configFile), configFile);
            return ScriptRuntimeSetup.ReadConfiguration(configFile);
        }

        private void SetTestLanguage() {
//          _testLanguage = "ironpython";
            _testEng = _PYEng; 
        }

        private static void AddSearchPaths(ScriptEngine engine) {
            var ironPythonPath = TestHelpers.IronPythonPath;
            if (!string.IsNullOrEmpty(ironPythonPath)) {
                var paths = new System.Collections.Generic.List<string>(engine.GetSearchPaths());
                if (!paths.Contains(ironPythonPath)) {
                    paths.Insert(0, ironPythonPath);
                    engine.SetSearchPaths(paths);
                }
            }
        }

    }
}
