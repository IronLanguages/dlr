// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting {
    public enum ScriptCodeParseResult {
        /// <summary>
        /// Source code is a syntactically correct.
        /// </summary>
        Complete,

        /// <summary>
        /// Source code represents an empty statement/expression.
        /// </summary>
        Empty,
            
        /// <summary>
        /// Source code is already invalid and no suffix can make it syntactically correct.
        /// </summary>
        Invalid,

        /// <summary>
        /// Last token is incomplete. Source code can still be completed correctly.
        /// </summary>
        IncompleteToken,

        /// <summary>
        /// Last statement is incomplete. Source code can still be completed correctly.
        /// </summary>
        IncompleteStatement,
    }

    // TODO: rename or remove
    public static class SourceCodePropertiesUtils {
        public static bool IsCompleteOrInvalid(/*this*/ ScriptCodeParseResult props, bool allowIncompleteStatement) {
            return
                props == ScriptCodeParseResult.Invalid ||
                props != ScriptCodeParseResult.IncompleteToken &&
                (allowIncompleteStatement || props != ScriptCodeParseResult.IncompleteStatement);
        }
    }
}
