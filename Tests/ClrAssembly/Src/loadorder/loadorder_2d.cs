// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// non-generic type, which has same namespace, different name from First.Nongeneric1
namespace First {
    public class Nongeneric2 {
        public static string Flag = typeof(Nongeneric2).FullName;
    }
}
