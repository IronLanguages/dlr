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
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Scripting.Utils;
using System.Threading;

namespace Microsoft.Scripting.Runtime {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
    public sealed class ScriptDomainManager {
        private readonly DynamicRuntimeHostingProvider _hostingProvider;
        private readonly SharedIO _sharedIO;
        private List<Assembly> _loadedAssemblies = new List<Assembly>();

        // last id assigned to a language context:
        private int _lastContextId;

        private readonly DlrConfiguration _configuration;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public PlatformAdaptationLayer Platform {
            get {
                PlatformAdaptationLayer result = _hostingProvider.PlatformAdaptationLayer;
                if (result == null) {
                    throw new InvalidImplementationException();
                }
                return result;
            }
        }

        public SharedIO SharedIO {
            get { return _sharedIO; }
        }

        public DynamicRuntimeHostingProvider Host {
            get { return _hostingProvider; }
        }

        public DlrConfiguration Configuration {
            get { return _configuration; }
        }

        public ScriptDomainManager(DynamicRuntimeHostingProvider hostingProvider, DlrConfiguration configuration) {
            ContractUtils.RequiresNotNull(hostingProvider, nameof(hostingProvider));
            ContractUtils.RequiresNotNull(configuration, nameof(configuration));

            configuration.Freeze();

            _hostingProvider = hostingProvider;
            _configuration = configuration;

            _sharedIO = new SharedIO();

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

            LanguageContext language;
            if (!_configuration.TryLoadLanguage(this, aqtn, out language)) {
                throw Error.UnknownLanguageProviderType();
            }
            return language;
        }

        public bool TryGetLanguage(string languageName, out LanguageContext language) {
            ContractUtils.RequiresNotNull(languageName, nameof(languageName));
            return _configuration.TryLoadLanguage(this, languageName, false, out language);
        }

        public LanguageContext GetLanguageByName(string languageName) {
            if (!TryGetLanguage(languageName, out LanguageContext language)) {
                throw new ArgumentException($"Unknown language name: '{languageName}'");
            }
            return language;
        }

        public bool TryGetLanguageByFileExtension(string fileExtension, out LanguageContext language) {
            ContractUtils.RequiresNotEmpty(fileExtension, nameof(fileExtension));
            return _configuration.TryLoadLanguage(this, DlrConfiguration.NormalizeExtension(fileExtension), true, out language);
        }

        public LanguageContext GetLanguageByExtension(string fileExtension) {
            LanguageContext language;
            if (!TryGetLanguageByFileExtension(fileExtension, out language)) {
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
