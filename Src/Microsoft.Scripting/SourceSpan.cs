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
using System.Globalization;

namespace Microsoft.Scripting {
    /// <summary>
    /// Stores the location of a span of text in a source file.
    /// </summary>
    [Serializable]
    public readonly struct SourceSpan : IEquatable<SourceSpan> {
        /// <summary>
        /// Constructs a new span with a specific start and end location.
        /// </summary>
        /// <param name="start">The beginning of the span.</param>
        /// <param name="end">The end of the span.</param>
        public SourceSpan(SourceLocation start, SourceLocation end) {
            ValidateLocations(start, end);
            Start = start;
            End = end;
        }

        private static void ValidateLocations(SourceLocation start, SourceLocation end) {
            if (start.IsValid && end.IsValid) {
                if (start > end) {
                    throw new ArgumentException("Start and End must be well ordered");
                }
            } else {
                if (start.IsValid || end.IsValid) {
                    throw new ArgumentException("Start and End must both be valid or both invalid");
                }
            }
        }

        /// <summary>
        /// The start location of the span.
        /// </summary>
        public SourceLocation Start { get; }

        /// <summary>
        /// The end location of the span. Location of the first character behind the span.
        /// </summary>
        public SourceLocation End { get; }

        /// <summary>
        /// Length of the span (number of characters inside the span).
        /// </summary>
        public int Length => End.Index - Start.Index;

        /// <summary>
        /// A valid span that represents no location.
        /// </summary>
        public static readonly SourceSpan None = new SourceSpan(SourceLocation.None, SourceLocation.None);

        /// <summary>
        /// An invalid span.
        /// </summary>
        public static readonly SourceSpan Invalid = new SourceSpan(SourceLocation.Invalid, SourceLocation.Invalid);

        /// <summary>
        /// Whether the locations in the span are valid.
        /// </summary>
        public bool IsValid => Start.IsValid && End.IsValid;

        /// <summary>
        /// Compares two specified Span values to see if they are equal.
        /// </summary>
        /// <param name="left">One span to compare.</param>
        /// <param name="right">The other span to compare.</param>
        /// <returns>True if the spans are the same, False otherwise.</returns>
        public static bool operator ==(SourceSpan left, SourceSpan right) {
            return left.Start == right.Start && left.End == right.End;
        }

        /// <summary>
        /// Compares two specified Span values to see if they are not equal.
        /// </summary>
        /// <param name="left">One span to compare.</param>
        /// <param name="right">The other span to compare.</param>
        /// <returns>True if the spans are not the same, False otherwise.</returns>
        public static bool operator !=(SourceSpan left, SourceSpan right) {
            return left.Start != right.Start || left.End != right.End;
        }

        public bool Equals(SourceSpan other) => Start == other.Start && End == other.End;

        public override bool Equals(object obj) => obj is SourceSpan other && Equals(other);

        public override string ToString() {
            return Start.ToString() + " - " + End.ToString();
        }

        public override int GetHashCode() {
            // 7 bits for each column (0-128), 9 bits for each row (0-512), xor helps if
            // we have a bigger file.
            return (Start.Column) ^ (End.Column << 7) ^ (Start.Line << 14) ^ (End.Line << 23);
        }

        internal string ToDebugString() {
            return String.Format(CultureInfo.CurrentCulture, "{0}-{1}", Start.ToDebugString(), End.ToDebugString());
        }

    }
}
