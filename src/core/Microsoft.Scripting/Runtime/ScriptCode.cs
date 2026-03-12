// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {
    /// <summary>
    /// ScriptCode is an instance of compiled code that is bound to a specific LanguageContext
    /// but not a specific ScriptScope. The code can be re-executed multiple times in different
    /// scopes. Hosting API counterpart for this class is <c>CompiledCode</c>.
    /// </summary>
    public abstract class ScriptCode {

        protected ScriptCode(SourceUnit sourceUnit) {
            ContractUtils.RequiresNotNull(sourceUnit, nameof(sourceUnit));

            SourceUnit = sourceUnit;
        }

        public LanguageContext LanguageContext => SourceUnit.LanguageContext;

        public SourceUnit SourceUnit { get; }

        public virtual Scope CreateScope() {
            return SourceUnit.LanguageContext.CreateScope();
        }

        public virtual object Run() {
            return Run(CreateScope());
        }

        public abstract object Run(Scope scope);

        public override string ToString() {
            return $"ScriptCode '{SourceUnit.Path}' from {LanguageContext.GetType().Name}";
        }
    }
}
