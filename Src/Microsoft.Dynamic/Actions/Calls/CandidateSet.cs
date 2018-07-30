// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// Represents a collection of MethodCandidate's which all accept the
    /// same number of logical parameters.  For example a params method
    /// and a method with 3 parameters would both be a CandidateSet for 3 parameters.
    /// </summary>
    internal sealed class CandidateSet {
        private readonly int _arity;
        private readonly List<MethodCandidate> _candidates;

        internal CandidateSet(int count) {
            _arity = count;
            _candidates = new List<MethodCandidate>();
        }

        internal List<MethodCandidate> Candidates => _candidates;

        internal int Arity => _arity;

        internal bool IsParamsDictionaryOnly() {
            foreach (MethodCandidate candidate in _candidates) {
                if (!candidate.HasParamsDictionary) {
                    return false;
                }
            }
            return true;
        }

        internal void Add(MethodCandidate target) {
            Debug.Assert(target.ParameterCount == _arity);
            _candidates.Add(target);
        }

        public override string ToString() {
            return $"{_arity}: ({_candidates[0].Overload.Name} on {_candidates[0].Overload.DeclaringType.FullName})";
        }
    }
}
