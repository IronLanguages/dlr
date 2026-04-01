// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace PossibleLoadException {
    public class A {
        public static int F = 10;
    }

    public class B : MissingType{
        public static int F = 20;
    }

    public class C {
        public static int F = 30;
    }
}
