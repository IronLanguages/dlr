// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {
    /// <summary>
    /// This structure represents an immutable integer interval that describes a range of values, from Start to End. 
    /// 
    /// It is closed on the left and open on the right: [Start .. End). 
    /// </summary>
    public readonly struct IndexSpan : IEquatable<IndexSpan> {
        public IndexSpan(int start, int length) {
            ContractUtils.Requires(length >= 0, nameof(length));
            ContractUtils.Requires(start >= 0, nameof(start));

            Start = start;
            Length = length;
        }

        public int Start { get; }

        public int End => Start + Length;

        public int Length { get; }

        public bool IsEmpty => Length == 0;

        public override int GetHashCode() {
            return Length.GetHashCode() ^ Start.GetHashCode();
        }

        public override bool Equals(object obj) =>
            obj is IndexSpan span && Equals(span);

        public static bool operator ==(IndexSpan self, IndexSpan other) => self.Equals(other);

        public static bool operator !=(IndexSpan self, IndexSpan other) => !self.Equals(other);

        #region IEquatable<IndexSpan> Members

        public bool Equals(IndexSpan other) => Length == other.Length && Start == other.Start;

        #endregion
    }
}
