// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Dynamic;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Generation;

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

        internal List<MethodCandidate> Candidates {
            get { return _candidates; }
        }

        internal int Arity {
            get { return _arity; }
        }

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
