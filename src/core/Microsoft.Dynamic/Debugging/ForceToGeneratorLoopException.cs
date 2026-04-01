// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Debugging {
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design","CA1032:ImplementStandardExceptionConstructors")]
    public sealed class ForceToGeneratorLoopException : Exception {
        public ForceToGeneratorLoopException() : base() { }
    }
}
