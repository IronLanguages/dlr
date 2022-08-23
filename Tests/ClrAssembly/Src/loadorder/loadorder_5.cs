// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace NS {
    public class Target<T> {
        public static string Flag = typeof(Target<>).FullName;
    }

    public class Target<T1, T2> {
        public static string Flag = typeof(Target<,>).FullName;
    }
}

