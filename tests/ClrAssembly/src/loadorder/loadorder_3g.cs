// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// generic type, which has same namespace, same name from First.Generic1
namespace First {
    public class Generic1<K, V> {
        public static string Flag = typeof(Generic1<,>).FullName + "_Same";
    }
}
