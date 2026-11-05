// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Scripting.Debugging {
    public sealed class DebugSourceFile {
        private DebugMode _debugMode;

        internal DebugSourceFile(string fileName, DebugMode debugMode) {
            Name = fileName;
            _debugMode = debugMode;
        }

        internal string Name { get; }

        internal DebugMode DebugMode {
            get { return _debugMode; }
            set { _debugMode = value; }
        }

        [Obsolete("do not call this property", true)]
        public int Mode {
            get { return (int)_debugMode; }
        }
    }
}
