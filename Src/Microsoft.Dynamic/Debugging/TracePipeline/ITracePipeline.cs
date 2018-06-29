﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Debugging {
    public interface ITracePipeline {
        void Close();

        bool TrySetNextStatement(string sourceFile, SourceSpan sourceSpan);

        ITraceCallback TraceCallback { get; set; }
    }
}
