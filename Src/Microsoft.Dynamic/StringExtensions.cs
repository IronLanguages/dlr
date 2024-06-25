using System;

namespace Microsoft.Scripting
{
    internal static class StringExtensions
    {
#if !NETCOREAPP
        public static bool EndsWith(this string str, char value)
        {
            return str.EndsWith(value.ToString(), StringComparison.Ordinal);
        }

        public static bool StartsWith(this string str, char value)
        {
            return str.StartsWith(value.ToString(), StringComparison.Ordinal);
        }

        public static int IndexOf(this string str, char value, StringComparison comparisonType) {
            if (comparisonType == StringComparison.Ordinal) return str.IndexOf(value);
            return str.IndexOf(value.ToString(), comparisonType);
        }
#endif
    }
}
