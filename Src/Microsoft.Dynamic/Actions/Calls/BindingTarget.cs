// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// Encapsulates the result of an attempt to bind to one or methods using the OverloadResolver.
    /// 
    /// Users should first check the Result property to see if the binding was successful or
    /// to determine the specific type of failure that occured.  If the binding was successful
    /// MakeExpression can then be called to create an expression which calls the method.
    /// If the binding was a failure callers can then create a custom error message based upon
    /// the reason the call failed.
    /// </summary>
    public sealed class BindingTarget {
        private readonly CallFailure[] _callFailures;                                     // if failed on conversion the various conversion failures for all overloads
        private readonly MethodCandidate[] _ambiguousMatches;                             // list of methods which are ambiguous to bind to.
        private readonly int[] _expectedArgs;                                             // gets the acceptable number of parameters which can be passed to the method.

        /// <summary>
        /// Creates a new BindingTarget when the method binding has succeeded.
        /// </summary>
        internal BindingTarget(string name, int actualArgumentCount, MethodCandidate candidate, NarrowingLevel level, RestrictedArguments restrictedArgs) {
            Name = name;
            MethodCandidate = candidate;
            RestrictedArguments = restrictedArgs;
            NarrowingLevel = level;
            ActualArgumentCount = actualArgumentCount;
        }

        /// <summary>
        /// Creates a new BindingTarget when the method binding has failed due to an incorrect argument count
        /// </summary>
        internal BindingTarget(string name, int actualArgumentCount, int[] expectedArgCount) {
            Name = name;
            Result = BindingResult.IncorrectArgumentCount;
            _expectedArgs = expectedArgCount;
            ActualArgumentCount = actualArgumentCount;
        }

        /// <summary>
        /// Creates a new BindingTarget when the method binding has failued due to 
        /// one or more parameters which could not be converted.
        /// </summary>
        internal BindingTarget(string name, int actualArgumentCount, CallFailure[] failures) {
            Name = name;
            Result = BindingResult.CallFailure;
            _callFailures = failures;
            ActualArgumentCount = actualArgumentCount;
        }

        /// <summary>
        /// Creates a new BindingTarget when the match was ambiguous
        /// </summary>
        internal BindingTarget(string name, int actualArgumentCount, MethodCandidate[] ambiguousMatches) {
            Name = name;
            Result = BindingResult.AmbiguousMatch;
            _ambiguousMatches = ambiguousMatches;
            ActualArgumentCount = actualArgumentCount;
        }

        /// <summary>
        /// Other failure.
        /// </summary>
        internal BindingTarget(string name, BindingResult result) {
            Name = name;
            Result = result;
        }

        /// <summary>
        /// Gets the result of the attempt to bind.
        /// </summary>
        public BindingResult Result { get; }

        /// <summary>
        /// Gets an Expression which calls the binding target if the method binding succeeded.
        /// 
        /// Throws InvalidOperationException if the binding failed.
        /// </summary>
        public Expression MakeExpression() {
            if (MethodCandidate == null) {
                throw new InvalidOperationException("An expression cannot be produced because the method binding was unsuccessful.");
            }

            if (RestrictedArguments == null) {
                throw new InvalidOperationException("An expression cannot be produced because the method binding was done with Expressions, not MetaObject's");
            }

            return MethodCandidate.MakeExpression(RestrictedArguments);
        }

        /// <summary>
        /// Returns the method if the binding succeeded, or null if no method was applicable.
        /// </summary>
        [Obsolete("Use Overload instead")]
        public MethodBase Method => MethodCandidate?.Overload.ReflectionInfo;

        /// <summary>
        /// Returns the selected overload if the binding succeeded, or null if no one was applicable.
        /// </summary>
        public OverloadInfo Overload => MethodCandidate?.Overload;

        /// <summary>
        /// Gets the name of the method as supplied to the OverloadResolver.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the MethodTarget if the binding succeeded, or null if no method was applicable.
        /// </summary>
        public MethodCandidate MethodCandidate { get; }

        /// <summary>
        /// Returns the methods which don't have any matches or null if Result == BindingResult.AmbiguousMatch
        /// </summary>
        public IEnumerable<MethodCandidate> AmbiguousMatches => _ambiguousMatches;

        /// <summary>
        /// Returns the methods and their associated conversion failures if Result == BindingResult.CallFailure.
        /// </summary>
        public ICollection<CallFailure> CallFailures => _callFailures;

        /// <summary>
        /// Returns the acceptable number of arguments which can be passed to the method if Result == BindingResult.IncorrectArgumentCount.
        /// </summary>
        public IList<int> ExpectedArgumentCount => _expectedArgs;

        /// <summary>
        /// Returns the total number of arguments provided to the call. 0 if the call succeeded or failed for a reason other
        /// than argument count mismatch.
        /// </summary>
        public int ActualArgumentCount { get; }

        /// <summary>
        /// Gets the MetaObjects which we originally did binding against in their restricted form.
        /// 
        /// The members of the array correspond to each of the arguments.  All members of the array
        /// have a value.
        /// </summary>
        public RestrictedArguments RestrictedArguments { get; }

        /// <summary>
        /// Returns the return type of the binding, or null if no method was applicable.
        /// </summary>
        public Type ReturnType => MethodCandidate?.ReturnType;

        /// <summary>
        /// Gets the NarrowingLevel of the method if the call succeeded.
        /// If the call failed returns NarrowingLevel.None.
        /// </summary>
        public NarrowingLevel NarrowingLevel { get; }

        /// <summary>
        /// Returns true if the binding was succesful, false if it failed.
        /// This is an alias for BindingTarget.Result == BindingResult.Success.
        /// </summary>
        public bool Success => Result == BindingResult.Success;
    }
}
