// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Scripting.Utils;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.Dynamic.Test {
    [TestFixture]
    public class TestReflectionServices {
        [Test]
        public void TestExtensionMethods1() {
            // on .NET Framework we expect exact counts
            bool expectExact = typeof(Enumerable).Assembly.FullName == "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            var expectedEnumerable = new Dictionary<string, int> {
                { "Enumerable.Aggregate", 3 },
                { "Enumerable.All", 1 },
                { "Enumerable.Any", 2 },
                { "Enumerable.Append", 1 },
                { "Enumerable.AsEnumerable", 1 },
                { "Enumerable.Average", 20 },
                { "Enumerable.Cast", 1 },
                { "Enumerable.Concat", 1 },
                { "Enumerable.Contains", 2 },
                { "Enumerable.Count", 2 },
                { "Enumerable.DefaultIfEmpty", 2 },
                { "Enumerable.Distinct", 2 },
                { "Enumerable.ElementAt", 1 },
                { "Enumerable.ElementAtOrDefault", 1 },
                { "Enumerable.Except", 2 },
                { "Enumerable.First", 2 },
                { "Enumerable.FirstOrDefault", 2 },
                { "Enumerable.GroupBy", 8 },
                { "Enumerable.GroupJoin", 2 },
                { "Enumerable.Intersect", 2 },
                { "Enumerable.Join", 2 },
                { "Enumerable.Last", 2 },
                { "Enumerable.LastOrDefault", 2 },
                { "Enumerable.LongCount", 2 },
                { "Enumerable.Max", 22 },
                { "Enumerable.Min", 22 },
                { "Enumerable.OfType", 1 },
                { "Enumerable.OrderBy", 2 },
                { "Enumerable.OrderByDescending", 2 },
                { "Enumerable.Prepend", 1 },
                { "Enumerable.Reverse", 1 },
                { "Enumerable.Select", 2 },
                { "Enumerable.SelectMany", 4 },
                { "Enumerable.SequenceEqual", 2 },
                { "Enumerable.Single", 2 },
                { "Enumerable.SingleOrDefault", 2 },
                { "Enumerable.Skip", 1 },
                { "Enumerable.SkipWhile", 2 },
                { "Enumerable.Sum", 20 },
                { "Enumerable.Take", 1 },
                { "Enumerable.TakeWhile", 2 },
                { "Enumerable.ThenBy", 2 },
                { "Enumerable.ThenByDescending", 2 },
                { "Enumerable.ToArray", 1 },
                { "Enumerable.ToDictionary", 4 },
                { "Enumerable.ToHashSet", 2 },
                { "Enumerable.ToList", 1 },
                { "Enumerable.ToLookup", 4 },
                { "Enumerable.Union", 2 },
                { "Enumerable.Where", 2 },
                { "Enumerable.Zip", 1 },
            };

            CheckExtensionMethods(typeof(Enumerable).Assembly, expectedEnumerable, expectExact);

            var expectedParallelEnumerable = new Dictionary<string, int> {
                { "ParallelEnumerable.Aggregate", 5 },
                { "ParallelEnumerable.All", 1 },
                { "ParallelEnumerable.Any", 2 },
                { "ParallelEnumerable.AsEnumerable", 1 },
                { "ParallelEnumerable.AsOrdered", 2 },
                { "ParallelEnumerable.AsParallel", 3 },
                { "ParallelEnumerable.AsSequential", 1 },
                { "ParallelEnumerable.AsUnordered", 1 },
                { "ParallelEnumerable.Average", 20 },
                { "ParallelEnumerable.Cast", 1 },
                { "ParallelEnumerable.Concat", 2 },
                { "ParallelEnumerable.Contains", 2 },
                { "ParallelEnumerable.Count", 2 },
                { "ParallelEnumerable.DefaultIfEmpty", 2 },
                { "ParallelEnumerable.Distinct", 2 },
                { "ParallelEnumerable.ElementAt", 1 },
                { "ParallelEnumerable.ElementAtOrDefault", 1 },
                { "ParallelEnumerable.Except", 4 },
                { "ParallelEnumerable.First", 2 },
                { "ParallelEnumerable.FirstOrDefault", 2 },
                { "ParallelEnumerable.ForAll", 1 },
                { "ParallelEnumerable.GroupBy", 8 },
                { "ParallelEnumerable.GroupJoin", 4 },
                { "ParallelEnumerable.Intersect", 4 },
                { "ParallelEnumerable.Join", 4 },
                { "ParallelEnumerable.Last", 2 },
                { "ParallelEnumerable.LastOrDefault", 2 },
                { "ParallelEnumerable.LongCount", 2 },
                { "ParallelEnumerable.Max", 22 },
                { "ParallelEnumerable.Min", 22 },
                { "ParallelEnumerable.OfType", 1 },
                { "ParallelEnumerable.OrderBy", 2 },
                { "ParallelEnumerable.OrderByDescending", 2 },
                { "ParallelEnumerable.Reverse", 1 },
                { "ParallelEnumerable.Select", 2 },
                { "ParallelEnumerable.SelectMany", 4 },
                { "ParallelEnumerable.SequenceEqual", 4 },
                { "ParallelEnumerable.Single", 2 },
                { "ParallelEnumerable.SingleOrDefault", 2 },
                { "ParallelEnumerable.Skip", 1 },
                { "ParallelEnumerable.SkipWhile", 2 },
                { "ParallelEnumerable.Sum", 20 },
                { "ParallelEnumerable.Take", 1 },
                { "ParallelEnumerable.TakeWhile", 2 },
                { "ParallelEnumerable.ThenBy", 2 },
                { "ParallelEnumerable.ThenByDescending", 2 },
                { "ParallelEnumerable.ToArray", 1 },
                { "ParallelEnumerable.ToDictionary", 4 },
                { "ParallelEnumerable.ToList", 1 },
                { "ParallelEnumerable.ToLookup", 4 },
                { "ParallelEnumerable.Union", 4 },
                { "ParallelEnumerable.Where", 2 },
                { "ParallelEnumerable.WithCancellation", 1 },
                { "ParallelEnumerable.WithDegreeOfParallelism", 1 },
                { "ParallelEnumerable.WithExecutionMode", 1 },
                { "ParallelEnumerable.WithMergeOptions", 1 },
                { "ParallelEnumerable.Zip", 2 },
            };

            CheckExtensionMethods(typeof(ParallelEnumerable).Assembly, expectedParallelEnumerable, expectExact);

            var expectedQueryable = new Dictionary<string, int> {
                { "Queryable.Aggregate", 3 },
                { "Queryable.All", 1 },
                { "Queryable.Any", 2 },
                { "Queryable.AsQueryable", 2 },
                { "Queryable.Average", 20 },
                { "Queryable.Cast", 1 },
                { "Queryable.Concat", 1 },
                { "Queryable.Contains", 2 },
                { "Queryable.Count", 2 },
                { "Queryable.DefaultIfEmpty", 2 },
                { "Queryable.Distinct", 2 },
                { "Queryable.ElementAt", 1 },
                { "Queryable.ElementAtOrDefault", 1 },
                { "Queryable.Except", 2 },
                { "Queryable.First", 2 },
                { "Queryable.FirstOrDefault", 2 },
                { "Queryable.GroupBy", 8 },
                { "Queryable.GroupJoin", 2 },
                { "Queryable.Intersect", 2 },
                { "Queryable.Join", 2 },
                { "Queryable.Last", 2 },
                { "Queryable.LastOrDefault", 2 },
                { "Queryable.LongCount", 2 },
                { "Queryable.Max", 2 },
                { "Queryable.Min", 2 },
                { "Queryable.OfType", 1 },
                { "Queryable.OrderBy", 2 },
                { "Queryable.OrderByDescending", 2 },
                { "Queryable.Reverse", 1 },
                { "Queryable.Select", 2 },
                { "Queryable.SelectMany", 4 },
                { "Queryable.SequenceEqual", 2 },
                { "Queryable.Single", 2 },
                { "Queryable.SingleOrDefault", 2 },
                { "Queryable.Skip", 1 },
                { "Queryable.SkipWhile", 2 },
                { "Queryable.Sum", 20 },
                { "Queryable.Take", 1 },
                { "Queryable.TakeWhile", 2 },
                { "Queryable.ThenBy", 2 },
                { "Queryable.ThenByDescending", 2 },
                { "Queryable.Union", 2 },
                { "Queryable.Where", 2 },
                { "Queryable.Zip", 1 },
            };

            CheckExtensionMethods(typeof(Queryable).Assembly, expectedQueryable, expectExact);
        }

        private void CheckExtensionMethods(Assembly assembly, Dictionary<string, int> expected, bool expectExact) {
            var methods = ReflectionUtils.GetVisibleExtensionMethods(assembly);

            // check that both methods produce the same result
            var methods2 = ReflectionUtils.GetVisibleExtensionMethodsSlow(assembly);
            Assert.IsTrue(new HashSet<MethodInfo>(methods).SetEquals(methods2));

            var actual = new Dictionary<string, int>();
            foreach (MethodInfo method in methods) {
                var name = $"{method.DeclaringType.Name}.{method.Name}";
                if (!actual.TryGetValue(name, out int count)) {
                    count = 0;
                }
                actual[name] = count + 1;
            }

            foreach (string name in expected.Keys) {
                Assert.IsTrue(actual.ContainsKey(name));
                Assert.IsTrue(expectExact ? expected[name] == actual[name] : expected[name] <= actual[name]);
            }
        }
    }
}
