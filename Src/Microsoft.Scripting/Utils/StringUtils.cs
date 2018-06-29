// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Scripting.Utils {
    internal static class StringUtils {

        public static Encoding DefaultEncoding {
            get {
#if FEATURE_ENCODING
                return Encoding.Default;
#else
                return Encoding.UTF8;
#endif
            }
        }

        public static string[] Split(string str, char[] separators, int maxComponents, StringSplitOptions options) {
            ContractUtils.RequiresNotNull(str, nameof(str));
            return str.Split(separators, maxComponents, options);
        }
    }
}
