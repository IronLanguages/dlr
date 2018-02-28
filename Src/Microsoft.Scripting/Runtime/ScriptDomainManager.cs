// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
    public sealed class ScriptDomainManager {
        private List<Assembly> _loadedAssemblies = new List<Assembly>();

        // last id assigned to a language context:
        private int _lastContextId;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public PlatformAdaptationLayer Platform {
            get {
                PlatformAdaptationLayer result = Host.PlatformAdaptationLayer;
                if (result == null) {
                    throw new InvalidImplementationException();
                }
                return result;
            }
        }

        public SharedIO SharedIO { get; }

        public DynamicRuntimeHostingProvider Host { get; }

        public DlrConfiguration Configuration { get; }

        public ScriptDomainManager(DynamicRuntimeHostingProvider hostingProvider, DlrConfiguration configuration) {
            ContractUtils.RequiresNotNull(hostingProvider, nameof(hostingProvider));
            ContractUtils.RequiresNotNull(configuration, nameof(configuration));

            configuration.Freeze();

            Host = hostingProvider;
            Configuration = configuration;

            SharedIO = new SharedIO();

            // create the initial default scope
            Globals = new Scope();
        }

        #region Language Registration

        internal ContextId GenerateContextId() {
            return new ContextId(Interlocked.Increment(ref _lastContextId));
        }

        public LanguageContext GetLanguage(Type providerType) {
            ContractUtils.RequiresNotNull(providerType, nameof(providerType));
            return GetLanguageByTypeName(providerType.AssemblyQualifiedName);
        }

        public LanguageContext GetLanguageByTypeName(string providerAssemblyQualifiedTypeName) {
            ContractUtils.RequiresNotNull(providerAssemblyQualifiedTypeName, nameof(providerAssemblyQualifiedTypeName));
            var aqtn = AssemblyQualifiedTypeName.ParseArgument(providerAssemblyQualifiedTypeName, nameof(providerAssemblyQualifiedTypeName));

            if (!Configuration.TryLoadLanguage(this, aqtn, out LanguageContext language)) {
                throw Error.UnknownLanguageProviderType();
            }
            return language;
        }

        public bool TryGetLanguage(string languageName, out LanguageContext language) {
            ContractUtils.RequiresNotNull(languageName, nameof(languageName));
            return Configuration.TryLoadLanguage(this, languageName, false, out language);
        }

        public LanguageContext GetLanguageByName(string languageName) {
            if (!TryGetLanguage(languageName, out LanguageContext language)) {
                throw new ArgumentException($"Unknown language name: '{languageName}'");
            }
            return language;
        }

        public bool TryGetLanguageByFileExtension(string fileExtension, out LanguageContext language) {
            ContractUtils.RequiresNotEmpty(fileExtension, nameof(fileExtension));
            return Configuration.TryLoadLanguage(this, DlrConfiguration.NormalizeExtension(fileExtension), true, out language);
        }

        public LanguageContext GetLanguageByExtension(string fileExtension) {
            if (!TryGetLanguageByFileExtension(fileExtension, out LanguageContext language)) {
                throw new ArgumentException($"Unknown file extension: '{fileExtension}'");
            }
            return language;
        }

        #endregion

        /// <summary>
        /// Gets or sets a collection of environment variables.
        /// </summary>
        public Scope Globals { get; set; }

        /// <summary>
        /// Event for when a host calls LoadAssembly.  After hooking this
        /// event languages will need to call GetLoadedAssemblyList to
        /// get any assemblies which were loaded before the language was
        /// loaded.
        /// </summary>
        public event EventHandler<AssemblyLoadedEventArgs> AssemblyLoaded;

        public bool LoadAssembly(Assembly assembly) {
            ContractUtils.RequiresNotNull(assembly, nameof(assembly));

            lock (_loadedAssemblies) {
                if (_loadedAssemblies.Contains(assembly)) {
                    // only deliver the event if we've never added the assembly before
                    return false;
                }
                _loadedAssemblies.Add(assembly);
            }

            EventHandler<AssemblyLoadedEventArgs> assmLoaded = AssemblyLoaded;
            assmLoaded?.Invoke(this, new AssemblyLoadedEventArgs(assembly));

            return true;
        }
        
        public IList<Assembly> GetLoadedAssemblyList() {
            lock (_loadedAssemblies) {
                return _loadedAssemblies.ToArray();
            }
        }
    }
}
