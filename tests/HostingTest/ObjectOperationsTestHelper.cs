// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace HostingTest
{
    public partial class ObjectOperationsTest : HAPITestBase {

        private TestContext testContextInstance;
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

       public ObjectOperationsTest()
            : base() {
           
       }

       internal object GetVariableValue(string code, string varname) {
            ScriptScope scope = _testEng.CreateScope();
            ScriptSource source = scope.Engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements);
            source.Execute(scope);
            return scope.GetVariable(varname);
       }

       internal void ValidateConvertTo<T> (object objectInScope, T expectedValue){
            // Get Operations for associated Engine
            ObjectOperations operations = _testEng.CreateOperations();
            Assert.AreEqual(expectedValue, operations.ConvertTo<T>(objectInScope));
       }

       internal void ValidateTryConvertTo<T>(object objectInScope, T expectedValue) {
           // Get Operations for associated Engine
           ObjectOperations operations = _testEng.CreateOperations();
           Assert.IsTrue(operations.TryConvertTo<T>(objectInScope, out expectedValue));
       }

       internal void ValidateCallSignatures(object objectFromScope, string[] expectedValue){
           string[] result = (string[])_testEng.Operations.GetCallSignatures(objectFromScope);
           TestHelpers.AreEqualArrays(expectedValue, result);
       }
    }
}
