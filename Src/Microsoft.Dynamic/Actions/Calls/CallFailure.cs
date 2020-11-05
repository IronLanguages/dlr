// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// Represents the reason why a call to a specific method could not be performed by the OverloadResolver.
    /// 
    /// The reason for the failure is specified by the CallFailureReason property.  Once this property
    /// has been consulted the other properties can be consulted for more detailed information regarding
    /// the failure.
    /// 
    /// If reason is ConversionFailure the ConversionResults property will be non-null.
    /// If reason is UnassignableKeyword the KeywordArguments property will be non-null and include
    ///     the keywords which could not be assigned.
    /// If reason is DuplicateKeyword the KeywordArguments property will be non-null and include
    ///     the keywords which were duplicated (either by the keywords themselves or by positional
    ///     arguments); also the PositionalArguments will be non-null and include the positions of positional
    ///     arguments (if any, 0 otherwise) that were duplicated by the corresponding keyword arguments.
    ///
    /// MethodTarget is always set and indicates the method which failed to bind.
    /// </summary>
    public sealed class CallFailure {
        private readonly ConversionResult[] _results;
        private readonly string[] _keywordArgs;
        private readonly int[] _positionalArgs;

        internal CallFailure(MethodCandidate candidate, ConversionResult[] results) {
            Candidate = candidate;
            Reason = CallFailureReason.ConversionFailure;
            _results = results;
        }

        internal CallFailure(MethodCandidate candidate, string[] keywordArgs) {
            Candidate = candidate;
            Reason = CallFailureReason.UnassignableKeyword;
            _keywordArgs = keywordArgs;
            _positionalArgs = EmptyArray<int>.Instance;
        }

        internal CallFailure(MethodCandidate candidate, string[] keywordArgs, int[] positionalArgs) {
            Candidate = candidate;
            Reason = CallFailureReason.DuplicateKeyword;
            _keywordArgs = keywordArgs;
            _positionalArgs = positionalArgs ?? EmptyArray<int>.Instance;
        }

        internal CallFailure(MethodCandidate candidate, CallFailureReason reason) {
            Debug.Assert(reason != CallFailureReason.ConversionFailure); // use first overload
            Debug.Assert(reason != CallFailureReason.UnassignableKeyword); // use second overload
            Debug.Assert(reason != CallFailureReason.DuplicateKeyword); // use third overload

            Candidate = candidate;
            Reason = reason;
        }

        /// <summary>
        /// Gets the MethodTarget which the call failed for.
        /// </summary>
        public MethodCandidate Candidate { get; }

        /// <summary>
        /// Gets the reason for the call failure which determines the other 
        /// properties of the CallFailure which should be consulted.
        /// </summary>
        public CallFailureReason Reason { get; }

        /// <summary>
        /// Gets a list of ConversionResult's for each parameter indicating
        /// whether the conversion was successful or failed and the types
        /// being converted.
        /// This property has a meaningful value only when Reason == ConversionFailure
        /// </summary>
        public IList<ConversionResult> ConversionResults => _results;

        /// <summary>
        /// Gets the list of keyword arguments that were either duplicated or
        /// unassignable.
        /// This property has a meaningful value only when Reason == UnassignableKeyword or DuplicateKeyword
        /// </summary>
        public IList<string> KeywordArguments => _keywordArgs;

        /// <summary>
        /// Gets 1-based positions of positional arguments that were duplicated
        /// by the corresponding (by index) keyword arguments from KeywordArguments.
        /// Value 0 of the position means that no positional argument is duplicated by the corresponding keyword argument.
        /// The list may be shorter than KeywordArguments, in such case all missing elements are assumed 0.
        /// This property has a meaningful value only when Reason == DuplicateKeyword
        /// </summary>
        public IList<int> PositionalArguments => _positionalArgs;
    }
}
