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
