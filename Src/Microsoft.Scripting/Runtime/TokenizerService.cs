// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Scripting.Runtime {
    public abstract class TokenizerService {

        // static contract:
        protected TokenizerService() {
        }

        public abstract void Initialize(object state, TextReader sourceReader, SourceUnit sourceUnit, SourceLocation initialLocation);

        /// <summary>
        /// The current internal state of the scanner.
        /// </summary>
        public abstract object CurrentState { get; }

        /// <summary>
        /// The current startLocation of the scanner.
        /// </summary>
        public abstract SourceLocation CurrentPosition { get; }

        /// <summary>
        /// Move the tokenizer past the next token and return its category.
        /// </summary>
        /// <returns>The token information associated with the token just scanned.</returns>
        public abstract TokenInfo ReadToken();

        public abstract bool IsRestartable { get; }
        public abstract ErrorSink ErrorSink { get; set; }

        /// <summary>
        /// Move the tokenizer past the next token.
        /// </summary>
        /// <returns><c>False</c> if the end of stream has been reached, <c>true</c> otherwise.</returns>
        public virtual bool SkipToken() {
            return ReadToken().Category != TokenCategory.EndOfStream;
        }

        /// <summary>
        /// Get all tokens over a block of the stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The scanner should return full tokens. If startLocation + length lands in the middle of a token, the full token
        /// should be returned.
        /// </para>
        /// </remarks>
        /// <param name="characterCount">Tokens are read until at least given amount of characters is read or the stream ends.</param>
        /// <returns>A enumeration of tokens.</returns>
        public IEnumerable<TokenInfo> ReadTokens(int characterCount) {
            List<TokenInfo> tokens = new List<TokenInfo>();

            int start = CurrentPosition.Index;

            while (CurrentPosition.Index - start < characterCount) {
                TokenInfo token = ReadToken();
                if (token.Category == TokenCategory.EndOfStream) {
                    break;
                }
                tokens.Add(token);
            }

            return tokens;
        }

        /// <summary>
        /// Scan from startLocation to at least startLocation + length.
        /// </summary>
        /// <param name="countOfChars">The mininum number of characters to process while getting tokens.</param>
        /// <remarks>
        /// This method is used to determine state at arbitrary startLocation.
        /// </remarks>
        /// <returns><c>False</c> if the end of stream has been reached, <c>true</c> otherwise.</returns>
        public bool SkipTokens(int countOfChars) {
            bool eos = false;
            int start_index = CurrentPosition.Index;

            while (CurrentPosition.Index - start_index < countOfChars && (eos = SkipToken())) {
                ;
            }

            return eos;
        }
    }
}
