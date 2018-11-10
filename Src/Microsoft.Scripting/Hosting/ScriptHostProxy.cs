// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Provides hosting to DLR. Forwards DLR requests to the ScriptHost. 
    /// </summary>
    internal sealed class ScriptHostProxy : DynamicRuntimeHostingProvider {
        private readonly ScriptHost _host;

        public ScriptHostProxy(ScriptHost host) {
            Assert.NotNull(host);
            _host = host;
        }

        public override PlatformAdaptationLayer PlatformAdaptationLayer => _host.PlatformAdaptationLayer;
    }
}
