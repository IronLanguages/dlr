// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;



#if SILVERLIGHT
using Microsoft.Silverlight.TestHostCritical;
#endif

using NUnit.Framework;


namespace HostingTest
{
    
    
    /// <summary>
    ///This is a test class for ScriiptIO and is intended
    ///to contain all ScriptIO Unit Tests
    ///</summary>
    public partial class ScriptIOTest : HAPITestBase {

        private TestContext testContextInstance;

        
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="testStream"></param>
       /// <param name="expectedOutput"></param>
       public void ValidateAttachedStreamOutput(string fname, string expectedOutput){

           string[] lines = File.ReadAllLines(fname);
           int n = lines.Length;
           Assert.AreEqual(lines[n - 1], expectedOutput);

       }
    
    }
}
