// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
