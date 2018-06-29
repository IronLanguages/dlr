// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {

    /// <summary>
    /// Represents the context that is flowed for doing Compiler.  Languages can derive
    /// from this class to provide additional contextual information.
    /// </summary>
    public sealed class CompilerContext {

        /// <summary>
        /// Gets the source unit currently being compiled in the CompilerContext.
        /// </summary>
        public SourceUnit SourceUnit { get; }

        /// <summary>
        /// Gets the sink for parser callbacks (e.g. brace matching, etc.).
        /// </summary>
        public ParserSink ParserSink { get; }

        /// <summary>
        /// Gets the current error sink.
        /// </summary>
        public ErrorSink Errors { get; }

        /// <summary>
        /// Gets the compiler specific options.
        /// </summary>
        public CompilerOptions Options { get; }

        public CompilerContext(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink)
            : this(sourceUnit, options, errorSink, ParserSink.Null) {
        }

        public CompilerContext(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink, ParserSink parserSink) {
            ContractUtils.RequiresNotNull(sourceUnit, nameof(sourceUnit));
            ContractUtils.RequiresNotNull(errorSink, nameof(errorSink));
            ContractUtils.RequiresNotNull(parserSink, nameof(parserSink));
            ContractUtils.RequiresNotNull(options, nameof(options));

            SourceUnit = sourceUnit;
            Options = options;
            Errors = errorSink;
            ParserSink = parserSink;
        }
    }
}
