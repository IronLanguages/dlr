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

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Singleton LanguageContext which represents a language-neutral LanguageContext
    /// </summary>
    internal sealed class InvariantContext : LanguageContext {
        // friend: ScriptDomainManager
        internal InvariantContext(ScriptDomainManager manager)
            : base(manager) {
        }

        public override bool CanCreateSourceCode => false;

        public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink) {
            // invariant language doesn't have a grammar:
            throw new NotSupportedException();
        }

        public override T ScopeGetVariable<T>(Scope scope, string name) {
            if (scope.Storage is ScopeStorage storage && storage.TryGetValue(name, false, out object res)) {
                return Operations.ConvertTo<T>(res);
            }

            if (scope.Storage is StringDictionaryExpando dictStorage &&
                dictStorage.Dictionary.TryGetValue(name, out res)) {
                return Operations.ConvertTo<T>(res);
            }

            return base.ScopeGetVariable<T>(scope, name);
        }

        public override dynamic ScopeGetVariable(Scope scope, string name) {
            if (scope.Storage is ScopeStorage storage && storage.TryGetValue(name, false, out object res)) {
                return res;
            }

            if (scope.Storage is StringDictionaryExpando dictStorage &&
                dictStorage.Dictionary.TryGetValue(name, out res)) {
                return res;
            }

            return base.ScopeGetVariable(scope, name);
        }

        public override void ScopeSetVariable(Scope scope, string name, object value) {
            if (scope.Storage is ScopeStorage storage) {
                storage.SetValue(name, false, value);
                return;
            }

            if (scope.Storage is StringDictionaryExpando dictStorage) {
                dictStorage.Dictionary[name] = value;
                return;
            }

            base.ScopeSetVariable(scope, name, value);
        }

        public override bool ScopeTryGetVariable(Scope scope, string name, out dynamic value) {
            if (scope.Storage is ScopeStorage storage && storage.TryGetValue(name, false, out value)) {
                return true;
            }

            if (scope.Storage is StringDictionaryExpando dictStorage && dictStorage.Dictionary.TryGetValue(name, out value)) {
                return true;
            }

            return base.ScopeTryGetVariable(scope, name, out value);
        }
    }
}
