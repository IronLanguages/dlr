/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {
    /// <summary>
    /// ScriptCode is an instance of compiled code that is bound to a specific LanguageContext
    /// but not a specific ScriptScope. The code can be re-executed multiple times in different
    /// scopes. Hosting API counterpart for this class is <c>CompiledCode</c>.
    /// </summary>
    public abstract class ScriptCode {
        private readonly SourceUnit _sourceUnit;

        protected ScriptCode(SourceUnit sourceUnit) {
            ContractUtils.RequiresNotNull(sourceUnit, nameof(sourceUnit));

            _sourceUnit = sourceUnit;
        }

        public LanguageContext LanguageContext => _sourceUnit.LanguageContext;

        public SourceUnit SourceUnit => _sourceUnit;

        public virtual Scope CreateScope() {
            return _sourceUnit.LanguageContext.CreateScope();
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
