// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Scripting.Utils {
    public static class StringUtils {

        [Obsolete("Use Encoding.Default")]
        public static Encoding DefaultEncoding { get; } = Encoding.Default;

        public static string GetSuffix(string str, char separator, bool includeSeparator) {
            ContractUtils.RequiresNotNull(str, nameof(str));
            int last = str.LastIndexOf(separator);
            return (last != -1) ? str.Substring(includeSeparator ? last : last + 1) : null;
        }

        public static string GetLongestPrefix(string str, char separator, bool includeSeparator) {
            ContractUtils.RequiresNotNull(str, nameof(str));
            int last = str.LastIndexOf(separator);
            return (last != -1) ? str.Substring(0, (includeSeparator || last == 0) ? last : last - 1) : null;
        }

        public static int CountOf(string str, char c) {
            if (string.IsNullOrEmpty(str)) return 0;

            int result = 0;
            for (int i = 0; i < str.Length; i++) {
                if (c == str[i]) {
                    result++;
                }
            }
            return result;
        }

        public static string[] Split(string str, string separator, int maxComponents, StringSplitOptions options) {
            ContractUtils.RequiresNotNull(str, nameof(str));
            return str.Split(new string[] { separator }, maxComponents, options);
        }

        [Obsolete("Use string.Split")]
        public static string[] Split(string str, char[] separators, int maxComponents, StringSplitOptions options)
            => str.Split(separators, maxComponents, options);

        /// <summary>
        /// Splits text and optionally indents first lines - breaks along words, not characters.
        /// </summary>
        public static string SplitWords(string text, bool indentFirst, int lineWidth) {
            ContractUtils.RequiresNotNull(text, nameof(text));

            const string indent = "    ";

            if (text.Length <= lineWidth || lineWidth <= 0) {
                if (indentFirst) return indent + text;
                return text;
            }

            StringBuilder res = new StringBuilder();
            int start = 0, len = lineWidth;
            while (start != text.Length) {
                if (len >= lineWidth) {
                    // find last space to break on
                    while (len != 0 && !char.IsWhiteSpace(text[start + len - 1]))
                        len--;
                }

                if (res.Length != 0) res.Append(' ');
                if (indentFirst || res.Length != 0) res.Append(indent);

                if (len == 0) {
                    int copying = Math.Min(lineWidth, text.Length - start);
                    res.Append(text, start, copying);
                    start += copying;
                } else {
                    res.Append(text, start, len);
                    start += len;
                }
                res.AppendLine();
                len = Math.Min(lineWidth, text.Length - start);
            }
            return res.ToString();
        }

        public static string AddSlashes(string str) {
            ContractUtils.RequiresNotNull(str, nameof(str));

            // TODO: optimize
            StringBuilder result = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++) {
                switch (str[i]) {
                    case '\a': result.Append("\\a"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    case '\v': result.Append("\\v"); break;
                    default: result.Append(str[i]); break;
                }
            }

            return result.ToString();
        }

        [Obsolete("Use double.TryParse")]
        public static bool TryParseDouble(string s, NumberStyles style, IFormatProvider provider, out double result)
            => double.TryParse(s, style, provider, out result);

        [Obsolete("Use int.TryParse")]
        public static bool TryParseInt32(string s, out int result)
            => int.TryParse(s, out result);

        [Obsolete("Use DateTime.TryParseExact")]
        public static bool TryParseDateTimeExact(string s, string format, IFormatProvider provider, DateTimeStyles style, out DateTime result)
            => DateTime.TryParseExact(s, format, provider, style, out result);

        [Obsolete("Use DateTime.TryParseExact")]
        public static bool TryParseDateTimeExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles style, out DateTime result)
            => DateTime.TryParseExact(s, formats, provider, style, out result);

        [Obsolete("Use DateTime.TryParse")]
        public static bool TryParseDate(string s, IFormatProvider provider, DateTimeStyles style, out DateTime result)
            => DateTime.TryParse(s, provider, style, out result);

        [Obsolete("Use CultureInfo.GetCultureInfo")]
        public static CultureInfo GetCultureInfo(string name)
            => CultureInfo.GetCultureInfo(name);

        // Like string.Split, but enumerates
        public static IEnumerable<string> Split(string str, string sep) {
            int start = 0, end;
            while ((end = str.IndexOf(sep, start, StringComparison.Ordinal)) != -1) {
                yield return str.Substring(start, end - start);

                start = end + sep.Length;
            }
            yield return str.Substring(start);
        }
    }
}
