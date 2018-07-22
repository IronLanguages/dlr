// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")] // TODO
    public struct ArgumentBinding {
        private static readonly int[] _EmptyBinding = new int[0];

        private readonly int _positionalArgCount;
        private readonly int[] _binding; // immutable

        internal ArgumentBinding(int positionalArgCount) {
            _positionalArgCount = positionalArgCount;
            _binding = _EmptyBinding;
        }

        internal ArgumentBinding(int positionalArgCount, int[] binding) {
            Assert.NotNull(binding);
            _binding = binding;
            _positionalArgCount = positionalArgCount;
        }

        public int PositionalArgCount => _positionalArgCount;

        public int ArgumentToParameter(int argumentIndex) {
            int i = argumentIndex - _positionalArgCount;
            return (i < 0) ? argumentIndex : _positionalArgCount + _binding[i];
        }
    }
}
