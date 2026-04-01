// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Microsoft.Scripting {
    public class AssemblyLoadedEventArgs : EventArgs {
        public AssemblyLoadedEventArgs(Assembly assembly) {
            Assembly = assembly;
        }

        public Assembly Assembly { get; }
    }
}
