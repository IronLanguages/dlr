// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Hosting;
using IronRuby.Runtime;
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
        protected ScriptRuntime _runtime, _remoteRuntime;

        protected ScriptEngine _testEng;

        protected ScriptEngine _PYEng;
        protected ScriptEngine _RBEng;

        protected ScriptScope _defaultScope;

        protected HAPITestBase() {
            
            var ses = CreateSetup();
            ses.HostType = typeof(TestHost);
            _runtime = new ScriptRuntime(ses);
            _remoteRuntime = ScriptRuntime.CreateRemote(TestHelpers.CreateAppDomain("Alternate"), ses);

            _runTime = _runtime;// _remoteRuntime;

            _PYEng = _runTime.GetEngine("py");
            _RBEng = _runTime.GetEngine("rb");

            SetTestLanguage();
            
            _defaultScope = _runTime.CreateScope();
            _codeSnippets = new PreDefinedCodeSnippets();
        }

        public static ScriptRuntime CreateRuntime() {
            return new ScriptRuntime(CreateSetup());
        }

        public static ScriptRuntime CreateRemoteRuntime(AppDomain domain) {
            return ScriptRuntime.CreateRemote(domain, CreateSetup());
        }

        public static ScriptRuntimeSetup CreateSetup() {
            var configFile = TestHelpers.StandardConfigFile;
            Debug.Assert(File.Exists(configFile), configFile);
            return ScriptRuntimeSetup.ReadConfiguration(configFile);
        }

        private void SetTestLanguage() {
//          _testLanguage = "ironpython";
            _testEng = _PYEng; 
        }

    }
}
