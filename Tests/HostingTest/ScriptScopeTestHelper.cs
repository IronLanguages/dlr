﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;


namespace HostingTest
{
    public partial class ScriptScopeTest : HAPITestBase {

        private TestContext testContextInstance;
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }
  
        private void ValidateGetItems(ScriptRuntime runtime) {
            ScriptScope scope = runtime.CreateScope();

            var dict = new KeyValuePair<string, object>("var1", 1);
            scope.SetVariable(dict.Key, dict.Value);

            TestHelpers.AreEqualCollections<KeyValuePair<string, object>>(new[] { dict }, scope.GetItems());

            var newDict = new KeyValuePair<string, object>("newVar", "newval");
            scope.SetVariable(newDict.Key, newDict.Value);

            TestHelpers.AreEqualCollections<KeyValuePair<string, object>>(new[] { dict, newDict }, scope.GetItems());
        }
    }

    internal static class ScriptScopeExtensions {

        //TODO : add validation code for ScriptScope
        internal static bool IsValid(this ScriptScope scope) {
            return true;
        }

        /// <summary>
        /// Check If Scope Is Empty
        /// 
        /// Simply verify that there are zero elements
        /// </summary>
        internal static bool IsEmpty(this ScriptScope scope) {
            return scope.GetVariableCount() == 0;
        }

        internal static int GetVariableCount(this ScriptScope scope) {
            int count = 0;
            foreach (string i in scope.GetVariableNames()) count++;
            return count;
        }

        /// <summary>
        /// Make sure scope does not have a default scope.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        internal static bool HasNoDefaultLanguage(this ScriptScope scope) {
            return scope.Engine == null;
        }

        /// <summary>
        /// Check to make sure that two seperate (i.e. not references to same memory) 
        /// ScriptScope objects are equivalent.
        /// 
        /// 1) If they are NOT pointing at the same memory.
        /// 2) If they have the same number of elements and each scope element is the
        ///    same then they are both equal
        internal static bool IsSimilarButNotSameAs(this ScriptScope callingScope, ScriptScope scope) {
            // if reference of same object in memory return false
            if (callingScope == scope) return false;

            foreach (string varName in callingScope.GetVariableNames()) {
                if (!scope.ContainsVariable(varName))
                    return false;
                if (scope.GetVariable(varName) != callingScope.GetVariable(varName))
                    return false;
            }

            return true;
        }
    }
}

