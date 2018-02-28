// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {

    public class ParserSink {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ParserSink Null = new ParserSink();
        
        public virtual void MatchPair(SourceSpan opening, SourceSpan closing, int priority) {
        }

        public virtual void MatchTriple(SourceSpan opening, SourceSpan middle, SourceSpan closing, int priority) {
        }

        public virtual void EndParameters(SourceSpan span) {
        }

        public virtual void NextParameter(SourceSpan span) {
        }

        public virtual void QualifyName(SourceSpan selector, SourceSpan span, string name) {
        }

        public virtual void StartName(SourceSpan span, string name) {
        }

        public virtual void StartParameters(SourceSpan context) {
        }
    }
}
