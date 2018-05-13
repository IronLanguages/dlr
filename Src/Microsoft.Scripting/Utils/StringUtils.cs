/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

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
