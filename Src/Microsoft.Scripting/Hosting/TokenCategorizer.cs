// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.Collections.Generic;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    public sealed class TokenCategorizer : MarshalByRefObject {
        private readonly TokenizerService _tokenizer;

        internal TokenCategorizer(TokenizerService tokenizer) {
            Assert.NotNull(tokenizer);
            _tokenizer = tokenizer;
        }

        public void Initialize(object state, ScriptSource scriptSource, SourceLocation initialLocation) {
            _tokenizer.Initialize(state, scriptSource.SourceUnit.GetReader(), scriptSource.SourceUnit, initialLocation);
        }

        /// <summary>
        /// The current internal state of the scanner.
        /// </summary>
        public object CurrentState => _tokenizer.CurrentState;

        /// <summary>
        /// The current startLocation of the scanner.
        /// </summary>
        public SourceLocation CurrentPosition => _tokenizer.CurrentPosition;

        /// <summary>
        /// Move the tokenizer past the next token and return its category.
        /// </summary>
        /// <returns>The token information associated with the token just scanned.</returns>
        public TokenInfo ReadToken() {
            return _tokenizer.ReadToken();
        }

        public bool IsRestartable => _tokenizer.IsRestartable;

        // TODO: Should be ErrorListener
        public ErrorSink ErrorSink {
            get => _tokenizer.ErrorSink;
            set => _tokenizer.ErrorSink = value;
        }

        /// <summary>
        /// Move the tokenizer past the next token.
        /// </summary>
        /// <returns><c>False</c> if the end of stream has been reached, <c>true</c> otherwise.</returns>
        public bool SkipToken() {
            return _tokenizer.SkipToken();
        }

        /// <summary>
        /// Get all tokens over a block of the stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The scanner should return full tokens. If startLocation + length lands in the middle of a token, the full token
        /// should be returned.
        /// </para>
        /// </remarks>s
        /// <param name="characterCount">Tokens are read until at least given amount of characters is read or the stream ends.</param>
        /// <returns>A enumeration of tokens.</returns>
        public IEnumerable<TokenInfo> ReadTokens(int characterCount) {
            return _tokenizer.ReadTokens(characterCount);
        }

        /// <summary>
        /// Scan from startLocation to at least startLocation + length.
        /// </summary>
        /// <param name="characterCount">Tokens are read until at least given amount of characters is read or the stream ends.</param>
        /// <remarks>
        /// This method is used to determine state at arbitrary startLocation.
        /// </remarks>
        /// <returns><c>False</c> if the end of stream has been reached, <c>true</c> otherwise.</returns>
        public bool SkipTokens(int characterCount) {
            return _tokenizer.SkipTokens(characterCount);
        }

#if FEATURE_REMOTING
        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
    }
}
