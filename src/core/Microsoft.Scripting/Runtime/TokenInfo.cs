// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.Scripting {

    [Serializable]
    public struct TokenInfo : IEquatable<TokenInfo> {

        public TokenCategory Category { get; set; }

        public TokenTriggers Trigger { get; set; }

        public SourceSpan SourceSpan { get; set; }

        public TokenInfo(SourceSpan span, TokenCategory category, TokenTriggers trigger) {
            Category = category;
            Trigger = trigger;
            SourceSpan = span;
        }

        public static bool operator ==(TokenInfo first, TokenInfo second) => first.Equals(second);

        public static bool operator !=(TokenInfo first, TokenInfo second) => !first.Equals(second);

        #region IEquatable<TokenInfo> Members

        public override readonly int GetHashCode() {
            unchecked {
                int hash = Category.GetHashCode();
                hash = ((hash << 5) + hash) ^ Trigger.GetHashCode();
                return ((hash << 5) + hash) ^ SourceSpan.GetHashCode();
            }
        }

        public override readonly bool Equals(object? obj) =>
            obj is TokenInfo info && Equals(info);

        public readonly bool Equals(TokenInfo other) =>
            Category == other.Category && Trigger == other.Trigger && SourceSpan == other.SourceSpan;

        #endregion

        public override readonly string ToString() {
            return $"TokenInfo: {SourceSpan}, {Category}, {Trigger}";
        }
    }
}
