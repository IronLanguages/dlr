// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    public sealed class ActualArguments {
        public ActualArguments(IList<DynamicMetaObject> args, IList<DynamicMetaObject> namedArgs, IList<string> argNames,
            int hiddenCount, int collapsedCount, int firstSplattedArg, int splatIndex) {

            ContractUtils.RequiresNotNullItems(args, nameof(args));
            ContractUtils.RequiresNotNullItems(namedArgs, nameof(namedArgs));
            ContractUtils.RequiresNotNullItems(argNames, nameof(argNames));
            ContractUtils.Requires(namedArgs.Count == argNames.Count);

            ContractUtils.Requires(splatIndex == -1 || firstSplattedArg == -1 || firstSplattedArg >= 0 && firstSplattedArg <= splatIndex);
            ContractUtils.Requires(splatIndex == -1 || splatIndex >= 0);
            ContractUtils.Requires(collapsedCount >= 0);
            ContractUtils.Requires(hiddenCount >= 0);

            Arguments = args;
            NamedArguments = namedArgs;
            ArgNames = argNames;
            CollapsedCount = collapsedCount;
            SplatIndex = collapsedCount > 0 ? splatIndex : -1;
            FirstSplattedArg = firstSplattedArg;
            HiddenCount = hiddenCount;
        }

        public int CollapsedCount { get; }

        /// <summary>
        /// Gets the index into _args array indicating the first post-splat argument or -1 of there are no splatted arguments.
        /// For call site f(a,b,*c,d) and preSplatLimit == 1 and postSplatLimit == 2
        /// args would be (a,b,c[0],c[n-2],c[n-1],d) with splat index 3, where n = c.Count.
        /// </summary>
        public int SplatIndex { get; }

        public int FirstSplattedArg { get; }

        public IList<string> ArgNames { get; }

        public IList<DynamicMetaObject> NamedArguments { get; }

        public IList<DynamicMetaObject> Arguments { get; }

        internal int ToSplattedItemIndex(int collapsedArgIndex) {
            return SplatIndex - FirstSplattedArg + collapsedArgIndex;
        }

        /// <summary>
        /// The number of arguments not counting the collapsed ones.
        /// </summary>
        public int Count => Arguments.Count + NamedArguments.Count;

        /// <summary>
        /// Gets the number of hidden arguments (used for error reporting).
        /// </summary>
        public int HiddenCount { get; }

        /// <summary>
        /// Gets the total number of visible arguments passed to the call site including collapsed ones.
        /// </summary>
        public int VisibleCount => Count + CollapsedCount - HiddenCount;

        public DynamicMetaObject this[int index] =>
            index < Arguments.Count ? Arguments[index] : NamedArguments[index - Arguments.Count];

        /// <summary>
        /// Binds named arguments to the parameters. Returns a permutation of indices that captures the relationship between 
        /// named arguments and their corresponding parameters. Checks for duplicate and unbound named arguments.
        /// Ensures that for all i: namedArgs[i] binds to parameters[args.Length + bindingPermutation[i]] 
        /// </summary>
        internal bool TryBindNamedArguments(MethodCandidate method, out ArgumentBinding binding, out CallFailure failure) {
            if (NamedArguments.Count == 0) {
                binding = new ArgumentBinding(Arguments.Count);
                failure = null;
                return true;
            }

            var permutation = new int[NamedArguments.Count];
            var boundParameters = new BitArray(NamedArguments.Count);

            for (int i = 0; i < permutation.Length; i++) {
                permutation[i] = -1;
            }

            List<string> unboundNames = null;
            List<string> duppedNames = null;

            int positionalArgCount = Arguments.Count;

            for (int i = 0; i < ArgNames.Count; i++) {
                int paramIndex = method.IndexOfParameter(ArgNames[i]);
                if (paramIndex >= 0) {
                    int nameIndex = paramIndex - positionalArgCount;

                    // argument maps to already bound parameter:
                    if (paramIndex < positionalArgCount || boundParameters[nameIndex]) {
                        if (duppedNames == null) {
                            duppedNames = new List<string>();
                        }
                        duppedNames.Add(ArgNames[i]);
                    } else {
                        permutation[i] = nameIndex;
                        boundParameters[nameIndex] = true;
                    }
                } else {
                    if (unboundNames == null) {
                        unboundNames = new List<string>();
                    }
                    unboundNames.Add(ArgNames[i]);
                }
            }

            binding = new ArgumentBinding(positionalArgCount, permutation);

            if (unboundNames != null) {
                failure = new CallFailure(method, unboundNames.ToArray(), true);
                return false;
            }

            if (duppedNames != null) {
                failure = new CallFailure(method, duppedNames.ToArray(), false);
                return false;
            }

            failure = null;
            return true;
        }
    }
}
