// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")] // TODO: fix
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

        #region IEquatable<TokenInfo> Members

        public bool Equals(TokenInfo other) {
            return Category == other.Category && Trigger == other.Trigger && SourceSpan == other.SourceSpan;
        }

        #endregion

        public override string ToString() {
            return $"TokenInfo: {SourceSpan}, {Category}, {Trigger}";
        }
    }
}
