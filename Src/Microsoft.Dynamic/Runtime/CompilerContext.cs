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
            ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");
            ContractUtils.RequiresNotNull(errorSink, "errorSink");
            ContractUtils.RequiresNotNull(parserSink, "parserSink");
            ContractUtils.RequiresNotNull(options, "options");

            SourceUnit = sourceUnit;
            Options = options;
            Errors = errorSink;
            ParserSink = parserSink;
        }
    }
}
