// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;

using Microsoft.Scripting.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Microsoft.Dynamic.Test {
    [TestClass]
    public class TestReflectionServices {
        [TestMethod]
        public void TestExtensionMethods1() {
            bool expectExact = typeof(Enumerable).Assembly.FullName == "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            Dictionary<string, int> expected = new Dictionary<string, int>();

            List<string> names = (new string[] {
                "Where", "Where", "OfType", "Cast", "Select", "Select", "SelectMany",
                "SelectMany", "SelectMany", "SelectMany", "Join", "Join", "GroupJoin",
                "GroupJoin", "OrderBy", "OrderBy", "OrderByDescending", "OrderByDescending", "ThenBy", "ThenBy",
                "ThenByDescending", "ThenByDescending", "Take", "TakeWhile",
                "TakeWhile", "Skip", "SkipWhile", "SkipWhile", "GroupBy", "GroupBy", "GroupBy", "GroupBy", "GroupBy",
                "GroupBy", "GroupBy", "GroupBy", "Distinct", "Distinct",
                "Concat", "Zip", "Union", "Union", "Intersect", "Intersect", "Except", "Except", "First", "First",
                "FirstOrDefault", "FirstOrDefault", "Last", "Last",
                "LastOrDefault", "LastOrDefault", "Single", "Single", "SingleOrDefault", "SingleOrDefault", "ElementAt",
                "ElementAtOrDefault", "DefaultIfEmpty", "DefaultIfEmpty",
                "Contains", "Contains", "Reverse", "SequenceEqual", "SequenceEqual", "Any", "Any", "All", "Count",
                "Count", "LongCount", "LongCount", "Min", "Min", "Max", "Max",
                "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum",
                "Sum", "Sum", "Sum", "Sum", "Sum", "Average", "Average",
                "Average", "Average", "Average", "Average", "Average", "Average", "Average", "Average", "Average",
                "Average", "Average", "Average", "Average", "Average", "Average",
                "Average", "Average", "Average", "Aggregate", "Aggregate", "Aggregate", "Where", "Where", "Select",
                "Select", "SelectMany", "SelectMany", "SelectMany", "SelectMany",
                "Take", "TakeWhile", "TakeWhile", "Skip", "SkipWhile", "SkipWhile", "Join", "Join", "GroupJoin",
                "GroupJoin", "OrderBy", "OrderBy", "OrderByDescending",
                "OrderByDescending", "ThenBy", "ThenBy", "ThenByDescending", "ThenByDescending", "GroupBy", "GroupBy",
                "GroupBy", "GroupBy", "GroupBy", "GroupBy", "GroupBy",
                "GroupBy", "Concat", "Zip", "Distinct", "Distinct", "Union", "Union", "Intersect", "Intersect",
                "Except", "Except", "Reverse", "SequenceEqual", "SequenceEqual",
                "AsEnumerable", "ToArray", "ToList", "ToDictionary", "ToDictionary", "ToDictionary", "ToDictionary",
                "ToLookup", "ToLookup", "ToLookup", "ToLookup", "DefaultIfEmpty",
                "DefaultIfEmpty", "OfType", "Cast", "First", "First", "FirstOrDefault", "FirstOrDefault", "Last",
                "Last", "LastOrDefault", "LastOrDefault", "Single", "Single",
                "SingleOrDefault", "SingleOrDefault", "ElementAt", "ElementAtOrDefault", "Any", "Any", "All", "Count",
                "Count", "LongCount", "LongCount", "Contains", "Contains",
                "Aggregate", "Aggregate", "Aggregate", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum",
                "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum",
                "Sum", "Sum", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min",
                "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min",
                "Min", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max",
                "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max",
                "Average", "Average", "Average", "Average", "Average", "Average", "Average", "Average", "Average",
                "Average", "Average", "Average", "Average", "Average", "Average",
                "Average", "Average", "Average", "Average", "Average",
                "Where", "Where", "Select", "Select", "Zip", "Zip", "Join", "Join",
                "Join", "Join", "GroupJoin", "GroupJoin", "GroupJoin", "GroupJoin", "SelectMany", "SelectMany",
                "SelectMany", "SelectMany", "OrderBy", "OrderBy", "OrderByDescending",
                "OrderByDescending", "ThenBy", "ThenBy", "ThenByDescending", "ThenByDescending", "GroupBy", "GroupBy",
                "GroupBy", "GroupBy", "GroupBy", "GroupBy", "GroupBy", "GroupBy",
                "Aggregate", "Aggregate", "Aggregate", "Aggregate", "Aggregate", "Count", "Count", "LongCount",
                "LongCount", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum",
                "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Sum", "Min", "Min", "Min",
                "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min",
                "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Min", "Max", "Max", "Max", "Max", "Max",
                "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max",
                "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Max", "Average", "Average", "Average", "Average",
                "Average", "Average", "Average", "Average", "Average", "Average",
                "Average", "Average", "Average", "Average", "Average", "Average", "Average", "Average", "Average",
                "Average", "Any", "Any", "All", "Contains", "Contains", "Take",
                "TakeWhile", "TakeWhile", "Skip", "SkipWhile", "SkipWhile", "Concat", "Concat", "SequenceEqual",
                "SequenceEqual", "SequenceEqual", "SequenceEqual", "Distinct",
                "Distinct", "Union", "Union", "Union", "Union", "Intersect", "Intersect", "Intersect", "Intersect",
                "Except", "Except", "Except", "Except", "AsEnumerable", "ToArray",
                "ToList", "ToDictionary", "ToDictionary", "ToDictionary", "ToDictionary", "ToLookup", "ToLookup",
                "ToLookup", "ToLookup", "Reverse", "OfType", "Cast", "First", "First",
                "FirstOrDefault", "FirstOrDefault", "Last", "Last", "LastOrDefault", "LastOrDefault", "Single",
                "Single", "SingleOrDefault", "SingleOrDefault", "DefaultIfEmpty",
                "DefaultIfEmpty", "ElementAt", "ElementAtOrDefault"
            }).ToList();

#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETSTANDARD2_0 && !WINDOWS_UWP
            names.AddRange(
                new string[] {
                    "AsQueryable", "AsQueryable", "AsParallel", "AsParallel", "AsParallel", "AsOrdered",
                    "AsOrdered", "AsUnordered", "AsSequential",
                    "WithDegreeOfParallelism", "WithCancellation", "WithExecutionMode", "WithMergeOptions",
                    "ForAll", "Unwrap", "Unwrap"
                });
#endif

            foreach (string name in names) {
                expected.TryGetValue(name, out int count);
                expected[name] = count + 1;
            }

            var methods = ReflectionUtils.GetVisibleExtensionMethods(typeof(Enumerable).Assembly);
            new List<MethodInfo>(ReflectionUtils.GetVisibleExtensionMethodsSlow(typeof(Enumerable).Assembly));

            Dictionary<string, int> actual = new Dictionary<string, int>();
            foreach (MethodInfo method in methods) {
                if (!actual.TryGetValue(method.Name, out int count)) {
                    count = 0;
                }
                actual[method.Name] = count + 1;
            }

            foreach (string name in expected.Keys) {
                Assert.IsTrue(actual.ContainsKey(name));
                Assert.IsTrue(expectExact ? expected[name] == actual[name] : expected[name] >= actual[name]);
            }
        }

        [TestMethod]
        public void TestReflectionCache() {
            MethodGroup a = ReflectionCache.GetMethodGroup("Enumerable", typeof(List<>).GetTypeInfo().DeclaredMethods.ToArray());
            MethodGroup b = ReflectionCache.GetMethodGroup("test", new MemberGroup(typeof(List<>).GetTypeInfo().DeclaredMethods.ToArray()));
        }
    }
}
