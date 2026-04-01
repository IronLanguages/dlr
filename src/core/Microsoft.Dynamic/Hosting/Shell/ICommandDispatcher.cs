// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Hosting.Shell {
    /// <summary>
    /// Used to dispatch a single interactive command. It can be used to control things like which Thread
    /// the command is executed on, how long the command is allowed to execute, etc
    /// </summary>
    public interface ICommandDispatcher {
        object Execute(CompiledCode compiledCode, ScriptScope scope);
    }
}