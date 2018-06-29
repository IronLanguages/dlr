// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System;
using System.Diagnostics;

namespace Microsoft.Scripting.Metadata {
    internal static class ClrStubs {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal unsafe static int GetCharCount(this Encoding encoding, byte* bytes, int byteCount, object nls) {
            return encoding.GetCharCount(bytes, byteCount);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal unsafe static void GetChars(this Encoding encoding, byte* bytes, int byteCount, char* chars, int charCount, object nls) {
            encoding.GetChars(bytes, byteCount, chars, charCount);
        }
    }
}