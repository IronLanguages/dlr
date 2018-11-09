// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Reflection;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Represents a Dynamic Language Runtime in Hosting API. 
    /// Hosting API counterpart for <see cref="ScriptDomainManager"/>.
    /// </summary>
    public sealed class ScriptRuntime : MarshalByRefObject {
        private readonly Dictionary<LanguageContext, ScriptEngine> _engines;
        private readonly InvariantContext _invariantContext;
        private readonly object _lock = new object();
        private ScriptScope _globals;
        private Scope _scopeGlobals;
        private ScriptEngine _invariantEngine;

        /// <summary>
        /// Creates ScriptRuntime in the current app-domain and initialized according to the the specified settings.
        /// Creates an instance of host class specified in the setup and associates it with the created runtime.
        /// Both Runtime and ScriptHost are collocated in the current app-domain.
        /// </summary>
        public ScriptRuntime(ScriptRuntimeSetup setup) {
            ContractUtils.RequiresNotNull(setup, nameof(setup));

            // Do this first, so we detect configuration errors immediately
            DlrConfiguration config = setup.ToConfiguration();
            Setup = setup;

            try {
                Host = (ScriptHost)Activator.CreateInstance(setup.HostType, setup.HostArguments.ToArray<object>());
            } catch (TargetInvocationException e) {
                throw new InvalidImplementationException(Strings.InvalidCtorImplementation(setup.HostType, e.InnerException.Message), e.InnerException);
            } catch (Exception e) {
                throw new InvalidImplementationException(Strings.InvalidCtorImplementation(setup.HostType, e.Message), e);
            }

            ScriptHostProxy hostProxy = new ScriptHostProxy(Host);

            Manager = new ScriptDomainManager(hostProxy, config);
            _invariantContext = new InvariantContext(Manager);

            IO = new ScriptIO(Manager.SharedIO);
            _engines = new Dictionary<LanguageContext, ScriptEngine>();

            _globals = new ScriptScope(GetEngineNoLockNoNotification(_invariantContext, out bool _), Manager.Globals);

            // runtime needs to be all set at this point, host code is called

            Host.SetRuntime(this);

            if (!setup.Options.TryGetValue("NoDefaultReferences", out object noDefaultRefs) || Convert.ToBoolean(noDefaultRefs) == false) {
                LoadAssembly(typeof(string).Assembly);
                LoadAssembly(typeof(Debug).Assembly);
            }
        }

        internal ScriptDomainManager Manager { get; }

        public ScriptHost Host { get; }

        public ScriptIO IO { get; }

        /// <summary>
        /// Creates a new runtime with languages set up according to the current application configuration 
        /// (using System.Configuration).
        /// </summary>
        public static ScriptRuntime CreateFromConfiguration() {
            return new ScriptRuntime(ScriptRuntimeSetup.ReadConfiguration());
        }

        #region Remoting

#if FEATURE_REMOTING

        /// <summary>
        /// Creates ScriptRuntime in the current app-domain and initialized according to the the specified settings.
        /// Creates an instance of host class specified in the setup and associates it with the created runtime.
        /// Both Runtime and ScriptHost are collocated in the specified app-domain.
        /// </summary>
        public static ScriptRuntime CreateRemote(AppDomain domain, ScriptRuntimeSetup setup) {
            ContractUtils.RequiresNotNull(domain, nameof(domain));
            return (ScriptRuntime)domain.CreateInstanceAndUnwrap(
                typeof(ScriptRuntime).Assembly.FullName, 
                typeof(ScriptRuntime).FullName, 
                false, 
                BindingFlags.Default, 
                null, 
                new object[] { setup }, 
                null,
                null
            );
        }

        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
        #endregion

        public ScriptRuntimeSetup Setup { get; }

        #region Engines

        public ScriptEngine GetEngine(string languageName) {
            ContractUtils.RequiresNotNull(languageName, nameof(languageName));

            if (!TryGetEngine(languageName, out ScriptEngine engine)) {
                throw new ArgumentException($"Unknown language name: '{languageName}'");
            }

            return engine;
        }

        public ScriptEngine GetEngineByTypeName(string assemblyQualifiedTypeName) {
            ContractUtils.RequiresNotNull(assemblyQualifiedTypeName, nameof(assemblyQualifiedTypeName));
            return GetEngine(Manager.GetLanguageByTypeName(assemblyQualifiedTypeName));
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public ScriptEngine GetEngineByFileExtension(string fileExtension) {
            ContractUtils.RequiresNotNull(fileExtension, nameof(fileExtension));

            if (!TryGetEngineByFileExtension(fileExtension, out ScriptEngine engine)) {
                throw new ArgumentException($"Unknown file extension: '{fileExtension}'");
            }

            return engine;
        }

        public bool TryGetEngine(string languageName, out ScriptEngine engine) {
            if (!Manager.TryGetLanguage(languageName, out LanguageContext language)) {
                engine = null;
                return false;
            }

            engine = GetEngine(language);
            return true;
        }

        public bool TryGetEngineByFileExtension(string fileExtension, out ScriptEngine engine) {
            if (!Manager.TryGetLanguageByFileExtension(fileExtension, out LanguageContext language)) {
                engine = null;
                return false;
            }

            engine = GetEngine(language);
            return true;
        }

        /// <summary>
        /// Gets engine for the specified language.
        /// </summary>
        internal ScriptEngine GetEngine(LanguageContext language) {
            Assert.NotNull(language);

            ScriptEngine engine;
            bool freshEngineCreated;
            lock (_engines) {
                engine = GetEngineNoLockNoNotification(language, out freshEngineCreated);
            }

            if (freshEngineCreated && !ReferenceEquals(language, _invariantContext)) {
                Host.EngineCreated(engine);
            }

            return engine;
        }

        /// <summary>
        /// Looks up the engine for the specified language. If the engine hasn't been created in this Runtime, it is instantiated here.
        /// The method doesn't lock nor send notifications to the host.
        /// </summary>
        private ScriptEngine GetEngineNoLockNoNotification(LanguageContext language, out bool freshEngineCreated) {
            Debug.Assert(_engines != null, "Invalid ScriptRuntime initialiation order");

            ScriptEngine engine;
            if (freshEngineCreated = !_engines.TryGetValue(language, out engine)) {
                engine = new ScriptEngine(this, language);
                _engines.Add(language, engine);
            }
            return engine;
        }

        #endregion

        #region Compilation, Module Creation

        public ScriptScope CreateScope() {
            return InvariantEngine.CreateScope();
        }

        public ScriptScope CreateScope(string languageId) {
            return GetEngine(languageId).CreateScope();
        }

        public ScriptScope CreateScope(IDynamicMetaObjectProvider storage) {
            return InvariantEngine.CreateScope(storage);
        }

        public ScriptScope CreateScope(string languageId, IDynamicMetaObjectProvider storage) {
            return GetEngine(languageId).CreateScope(storage);
        }

        public ScriptScope CreateScope(IDictionary<string, object> dictionary) {
            return InvariantEngine.CreateScope(dictionary);
        }

        public ScriptScope CreateScope(string languageId, IDictionary<string, object> storage) {
            return GetEngine(languageId).CreateScope(storage);
        }

        #endregion

        // TODO: file IO exceptions, parse exceptions, execution exceptions, etc.
        /// <exception cref="ArgumentException">
        /// path is empty, contains one or more of the invalid characters defined in GetInvalidPathChars or doesn't have an extension.
        /// </exception>
        public ScriptScope ExecuteFile(string path) {
            ContractUtils.RequiresNotEmpty(path, nameof(path));
            string extension = Path.GetExtension(path);

            if (!TryGetEngineByFileExtension(extension, out ScriptEngine engine)) {
                throw new ArgumentException($"File extension '{extension}' is not associated with any language.");
            }

            return engine.ExecuteFile(path);
        }

        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">file extension does not map to language engine</exception>
        /// <exception cref="InvalidOperationException">language does not have any search paths</exception>
        /// <exception cref="FileNotFoundException">file does exist in language's search path</exception>
        public ScriptScope UseFile(string path) {
            ContractUtils.RequiresNotEmpty(path, nameof(path));
            string extension = Path.GetExtension(path);

            if (!TryGetEngineByFileExtension(extension, out ScriptEngine engine)) {
                throw new ArgumentException($"File extension '{extension}' is not associated with any language.");
            }

            var searchPaths = engine.GetSearchPaths();
            if (searchPaths.Count == 0) {
                throw new InvalidOperationException(
                    $"No search paths defined for language '{engine.Setup.DisplayName}'");
            }

            // See if the file is already loaded, if so return the scope
            foreach (string searchPath in searchPaths) {
                string filePath = Path.Combine(searchPath, path);
                ScriptScope scope = engine.GetScope(filePath);
                if (scope != null) {
                    return scope;
                }
            }

            // Find the file on disk, then load it
            foreach (string searchPath in searchPaths) {
                string filePath = Path.Combine(searchPath, path);
                if (Manager.Platform.FileExists(filePath)) {
                    return ExecuteFile(filePath);
                }
            }

            // Didn't find the file, throw
            string allPaths = searchPaths.Aggregate((x, y) => x + ", " + y);
            throw new FileNotFoundException($"File '{path}' not found in language's search path: {allPaths}");
        }

        /// <summary>
        /// This property returns the "global object" or name bindings of the ScriptRuntime as a ScriptScope.  
        /// 
        /// You can set the globals scope, which you might do if you created a ScriptScope with an 
        /// IAttributesCollection so that your host could late bind names.
        /// </summary>
        public ScriptScope Globals {
            get {
                Scope scope = Manager.Globals;
                if (_scopeGlobals == scope) {
                    return _globals;
                }
                lock (_lock) {
                    if (_scopeGlobals != scope) {
                        // make sure no one has changed the globals behind our back
                        _globals = new ScriptScope(InvariantEngine, scope); // TODO: Should get LC from Scope when it's there
                        _scopeGlobals = scope;
                    }

                    return _globals;
                }
            }
            set {
                ContractUtils.RequiresNotNull(value, nameof(value));
                lock (_lock) {
                    _globals = value;
                    Manager.Globals = value.Scope;
                }
            }
        }

        /// <summary>
        /// This method walks the assembly's namespaces and name bindings to ScriptRuntime.Globals 
        /// to represent the types available in the assembly.  Each top-level namespace name gets 
        /// bound in Globals to a dynamic object representing the namespace.  Within each top-level 
        /// namespace object, nested namespace names are bound to dynamic objects representing each 
        /// tier of nested namespaces.  When this method encounters the same namespace-qualified name, 
        /// it merges names together objects representing the namespaces.
        /// </summary>
        /// <param name="assembly"></param>
        public void LoadAssembly(Assembly assembly) {
            Manager.LoadAssembly(assembly);
        }

        public ObjectOperations Operations => InvariantEngine.Operations;

        public ObjectOperations CreateOperations() {
            return InvariantEngine.CreateOperations();
        }

        public void Shutdown() {
            List<LanguageContext> lcs;
            lock (_engines) {
                lcs = new List<LanguageContext>(_engines.Keys);
            }

            foreach (var language in lcs) {
                language.Shutdown();
            }
        }

        internal ScriptEngine InvariantEngine =>
            _invariantEngine ?? (_invariantEngine = GetEngine(_invariantContext));
    }
}
