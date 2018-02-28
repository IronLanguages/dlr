// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions.Calls {
    public enum CallFailureReason {
        /// <summary>
        /// Default value, their was no CallFailure.
        /// </summary>
        None,

        /// <summary>
        /// One of more parameters failed to be converted
        /// </summary>
        ConversionFailure,

        /// <summary>
        /// One or more keyword arguments could not be successfully assigned to a positional argument
        /// </summary>
        UnassignableKeyword,

        /// <summary>
        /// One or more keyword arguments were duplicated or would have taken the spot of a 
        /// provided positional argument.
        /// </summary>
        DuplicateKeyword,

        /// <summary>
        /// Type arguments could not be inferred
        /// </summary>
        TypeInference
    }
}
