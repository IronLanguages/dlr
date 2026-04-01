// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace HostingTest
{
    public partial class ScriptRuntimeTest : HAPITestBase {

        public ScriptRuntimeTest() {

        }

        private TestContext testContextInstance;
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        internal static ScriptRuntime CreateRuntime(AppDomain appDomain) {
            return ScriptRuntime.CreateRemote(appDomain, CreateSetup());
        }

        internal static ScriptRuntime CreatePythonOnlyRuntime(string[] ids, string[] exts) {
            ScriptRuntimeSetup srs = new ScriptRuntimeSetup();
            srs.LanguageSetups.Add(new LanguageSetup(
                typeof(IronPython.Runtime.PythonContext).AssemblyQualifiedName,
                "python", ids, exts
            ));

            return new ScriptRuntime(srs);
        }
    }
	
    internal static class ScriptRuntimeExtensions {
        internal static bool IsValid(this ScriptRuntime sr) {
            ScriptEngine se = sr.GetEngine("py");
            ScriptScope ss = se.CreateScope();

            ScriptSource code = se.CreateScriptSourceFromString("five=2+3", Microsoft.Scripting.SourceCodeKind.Statements);
            code.Execute(ss);

            return (int)ss.GetVariable("five") == 5;
        }
    }
}
