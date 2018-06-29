// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.Runtime.Serialization;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Providers {

    /// <summary>
    /// Advanced APIs for HAPI providers. These methods should not be used by hosts. 
    /// They are provided for other hosting API implementers that would like to leverage existing HAPI and 
    /// extend it with language specific functionality, for example. 
    /// </summary>
    public static class HostingHelpers {
        /// <exception cref="ArgumentNullException"><paramref name="runtime"/> is a <c>null</c> reference.</exception>
        /// <exception cref="SerializationException"><paramref name="runtime"/> is remote.</exception>
        public static ScriptDomainManager GetDomainManager(ScriptRuntime runtime) {
            ContractUtils.RequiresNotNull(runtime, nameof(runtime));
            return runtime.Manager;
        }

        /// <exception cref="ArgumentNullException"><paramref name="engine"/>e is a <c>null</c> reference.</exception>
        /// <exception cref="SerializationException"><paramref name="engine"/> is remote.</exception>
        public static LanguageContext GetLanguageContext(ScriptEngine engine) {
            ContractUtils.RequiresNotNull(engine, nameof(engine));
            return engine.LanguageContext;
        }

        /// <exception cref="ArgumentNullException"><paramref name="scriptSource"/> is a <c>null</c> reference.</exception>
        /// <exception cref="SerializationException"><paramref name="scriptSource"/> is remote.</exception>
        public static SourceUnit GetSourceUnit(ScriptSource scriptSource) {
            ContractUtils.RequiresNotNull(scriptSource, nameof(scriptSource));
            return scriptSource.SourceUnit;
        }

        /// <exception cref="ArgumentNullException"><paramref name="compiledCode"/> is a <c>null</c> reference.</exception>
        /// <exception cref="SerializationException"><paramref name="compiledCode"/> is remote.</exception>
        public static ScriptCode GetScriptCode(CompiledCode compiledCode) {
            ContractUtils.RequiresNotNull(compiledCode, nameof(compiledCode));
            return compiledCode.ScriptCode;
        }

        /// <exception cref="ArgumentNullException"><paramref name="io"/> is a <c>null</c> reference.</exception>
        /// <exception cref="SerializationException"><paramref name="io"/> is remote.</exception>
        public static SharedIO GetSharedIO(ScriptIO io) {
            ContractUtils.RequiresNotNull(io, nameof(io));
            return io.SharedIO;
        }

        /// <exception cref="ArgumentNullException"><paramref name="scriptScope"/> is a <c>null</c> reference.</exception>
        /// <exception cref="SerializationException"><paramref name="scriptScope"/> is remote.</exception>
        public static Scope GetScope(ScriptScope scriptScope) {
            ContractUtils.RequiresNotNull(scriptScope, nameof(scriptScope));
            return scriptScope.Scope;
        }

        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is a <c>null</c> reference.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="scope"/> is a <c>null</c> reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="engine"/> is a transparent proxy.</exception>
        public static ScriptScope CreateScriptScope(ScriptEngine engine, Scope scope) {
            ContractUtils.RequiresNotNull(engine, nameof(engine));
            ContractUtils.RequiresNotNull(scope, nameof(scope));
#if FEATURE_REMOTING
            ContractUtils.Requires(!RemotingServices.IsTransparentProxy(engine), nameof(engine), "The engine cannot be a transparent proxy");
#endif
            return new ScriptScope(engine, scope);
        }

        /// <summary>
        /// Performs a callback in the ScriptEngine's app domain and returns the result.
        /// </summary>
        [Obsolete("You should implement a service via LanguageContext and call ScriptEngine.GetService")]
        public static TRet CallEngine<T, TRet>(ScriptEngine engine, Func<LanguageContext, T, TRet> f, T arg) {            
            return engine.Call(f, arg);
        }

        /// <summary>
        /// Creates a new DocumentationOperations object from the given DocumentationProvider.
        /// </summary>
        public static DocumentationOperations CreateDocumentationOperations(DocumentationProvider provider) {
            return new DocumentationOperations(provider);
        }
    }
}
