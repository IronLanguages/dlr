// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// DLR requires any Hosting API provider to implement this class and provide its instance upon Runtime initialization.
    /// DLR calls on it to perform basic host/system dependent operations.
    /// </summary>
    [Serializable]
    public abstract class DynamicRuntimeHostingProvider {
        /// <summary>
        /// Abstracts system operations that are used by DLR and could potentially be platform specific.
        /// </summary>
        public abstract PlatformAdaptationLayer PlatformAdaptationLayer { get; }
    }
}
