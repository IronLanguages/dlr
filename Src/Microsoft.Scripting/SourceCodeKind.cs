// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace Microsoft.Scripting {

    /// <summary>
    /// Defines a kind of the source code. The parser sets its initial state accordingly.
    /// </summary>
    public enum SourceCodeKind {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Unspecified = 0,

        /// <summary>
        /// The code is an expression.
        /// </summary>
        Expression = 1,

        /// <summary>
        /// The code is a sequence of statements.
        /// </summary>
        Statements = 2,

        /// <summary>
        /// The code is a single statement.
        /// </summary>
        SingleStatement = 3,

        /// <summary>
        /// The code is a content of a file.
        /// </summary>
        File = 4,

        /// <summary>
        /// The code is an interactive command.
        /// </summary>
        InteractiveCode = 5,

        /// <summary>
        /// The language parser auto-detects the kind. A syntax error is reported if it is not able to do so.
        /// </summary>
        AutoDetect = 6
    }
}

namespace Microsoft.Scripting.Utils {
    public static partial class EnumBounds {
        public static bool IsValid(this SourceCodeKind value) {
            return value > SourceCodeKind.Unspecified && value <= SourceCodeKind.AutoDetect;
        }
    }
}
