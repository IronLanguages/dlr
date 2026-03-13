// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// non-generic type, which has different namespace, same name from First.Nongeneric1
namespace Second {
    public class Nongeneric1 {
        public static string Flag = typeof(Nongeneric1).FullName;
    }
}
