// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Utils {
    internal static class ExceptionUtils {
        public static ArgumentNullException MakeArgumentItemNullException(int index, string arrayName) {
            return new ArgumentNullException($"{arrayName}[{index}]");
        }
    }
}
