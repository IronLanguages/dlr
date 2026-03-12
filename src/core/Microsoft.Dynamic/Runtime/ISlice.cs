// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// A useful interface for taking slices of numeric arrays, inspired by Python's Slice objects.
    /// </summary>
    public interface ISlice {
        /// <summary>
        /// The starting index of the slice or null if no first index defined
        /// </summary>
        object Start { get; }

        /// <summary>
        /// The ending index of the slice or null if no ending index defined
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop")] // TODO: fix
        object Stop { get; }

        /// <summary>
        /// The length of step to take
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Step")] // TODO: fix
        object Step { get; }
    }
}