// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions.Calls {
    public enum Candidate {
        Equivalent = 0,
        One = +1,
        Two = -1,
        Ambiguous = 2
    }

    internal static class CandidateExtension {
        public static bool Chosen(this Candidate candidate) {
            return candidate == Candidate.One || candidate == Candidate.Two;
        }

        public static Candidate TheOther(this Candidate candidate) {
            if (candidate == Candidate.One) {
                return Candidate.Two;
            }
            if (candidate == Candidate.Two) {
                return Candidate.One;
            }
            return candidate;
        }
    }
}
