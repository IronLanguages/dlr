// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Singleton for each language.
    /// </summary>
    internal sealed class LanguageConfiguration {
        private readonly IDictionary<string, object> _options;
        private LanguageContext _context;

        public LanguageContext LanguageContext => _context;

        public AssemblyQualifiedTypeName ProviderName { get; }

        public string DisplayName { get; }

        public LanguageConfiguration(AssemblyQualifiedTypeName providerName, string displayName, IDictionary<string, object> options) {
            ProviderName = providerName;
            DisplayName = displayName;
            _options = options;
        }

        /// <summary>
        /// Must not be called under a lock as it can potentially call a user code.
        /// </summary>
        /// <exception cref="InvalidImplementationException">The language context's implementation failed to instantiate.</exception>
        internal LanguageContext LoadLanguageContext(ScriptDomainManager domainManager, out bool alreadyLoaded) {
            if (_context == null) {

                // Let assembly load errors bubble out
                var assembly = domainManager.Platform.LoadAssembly(ProviderName.AssemblyName.FullName);

                Type type = assembly.GetType(ProviderName.TypeName);
                if (type == null) {
                    throw new InvalidOperationException(
                        String.Format(
                            "Failed to load language '{0}': assembly '{1}' does not contain type '{2}'",
                            DisplayName, 
#if FEATURE_FILESYSTEM
                            assembly.Location,
#else
                            assembly.FullName,
#endif
                             ProviderName.TypeName
                    ));
                }

                if (!type.IsSubclassOf(typeof(LanguageContext))) {
                    throw new InvalidOperationException(
                        $"Failed to load language '{DisplayName}': type '{type}' is not a valid language provider because it does not inherit from LanguageContext"); 
                }

                LanguageContext context;
                try {
                    context = (LanguageContext)Activator.CreateInstance(type, new object[] { domainManager, _options });
                } catch (TargetInvocationException e) {
                    throw new TargetInvocationException(
                        $"Failed to load language '{DisplayName}': {e.InnerException.Message}", 
                        e.InnerException
                    );
                } catch (Exception e) {
                    throw new InvalidImplementationException(Strings.InvalidCtorImplementation(type, e.Message), e);
                }

                alreadyLoaded = Interlocked.CompareExchange(ref _context, context, null) != null;
            } else {
                alreadyLoaded = true;
            }

            return _context;
        }
    }

    public sealed class DlrConfiguration {
        private bool _frozen;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly StringComparer FileExtensionComparer = StringComparer.OrdinalIgnoreCase;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly StringComparer LanguageNameComparer = StringComparer.OrdinalIgnoreCase;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly StringComparer OptionNameComparer = StringComparer.Ordinal;

        private readonly Dictionary<string, LanguageConfiguration> _languageNames;
        private readonly Dictionary<string, LanguageConfiguration> _languageExtensions;
        private readonly Dictionary<AssemblyQualifiedTypeName, LanguageConfiguration> _languageConfigurations;
        private readonly Dictionary<Type, LanguageConfiguration> _loadedProviderTypes;

        public DlrConfiguration(bool debugMode, bool privateBinding, IDictionary<string, object> options) {
            ContractUtils.RequiresNotNull(options, nameof(options));
            DebugMode = debugMode;
            PrivateBinding = privateBinding;
            Options = options;

            _languageNames = new Dictionary<string, LanguageConfiguration>(LanguageNameComparer);
            _languageExtensions = new Dictionary<string, LanguageConfiguration>(FileExtensionComparer);
            _languageConfigurations = new Dictionary<AssemblyQualifiedTypeName, LanguageConfiguration>();
            _loadedProviderTypes = new Dictionary<Type, LanguageConfiguration>();
        }

        /// <summary>
        /// Gets a value indicating whether the application is in debug mode.
        /// This means:
        /// 
        /// 1) Symbols are emitted for debuggable methods (methods associated with SourceUnit).
        /// 2) Debuggable methods are emitted to non-collectable types (this is due to CLR limitations on dynamic method debugging).
        /// 3) JIT optimization is disabled for all methods
        /// 4) Languages may disable optimizations based on this value.
        /// </summary>
        public bool DebugMode { get; }

        /// <summary>
        /// Ignore CLR visibility checks.
        /// </summary>
        public bool PrivateBinding { get; }

        internal IDictionary<string, object> Options { get; }

        internal IDictionary<AssemblyQualifiedTypeName, LanguageConfiguration> Languages => _languageConfigurations;

        public void AddLanguage(string languageTypeName, string displayName, IList<string> names, IList<string> fileExtensions,
            IDictionary<string, object> options) {
            AddLanguage(languageTypeName, displayName, names, fileExtensions, options, null);
        }

        internal void AddLanguage(string languageTypeName, string displayName, IList<string> names, IList<string> fileExtensions, 
            IDictionary<string, object> options, string paramName) {
            ContractUtils.Requires(!_frozen, "Configuration cannot be modified once the runtime is initialized");
            ContractUtils.Requires(
                names.TrueForAll((id) => !String.IsNullOrEmpty(id) && !_languageNames.ContainsKey(id)),
                paramName ?? "names",
                "Language name should not be null, empty or duplicated between languages"
            );
            ContractUtils.Requires(
                fileExtensions.TrueForAll((ext) => !String.IsNullOrEmpty(ext) && !_languageExtensions.ContainsKey(ext)),
                paramName ?? "fileExtensions",
                "File extension should not be null, empty or duplicated between languages"
            );
            ContractUtils.RequiresNotNull(displayName, paramName ?? nameof(displayName));

            if (string.IsNullOrEmpty(displayName)) {
                ContractUtils.Requires(names.Count > 0, paramName ?? nameof(displayName), "Must have a non-empty display name or a a non-empty list of language names");
                displayName = names[0];
            }

            var aqtn = AssemblyQualifiedTypeName.ParseArgument(languageTypeName, paramName ?? "languageTypeName");
            if (_languageConfigurations.ContainsKey(aqtn)) {
                throw new ArgumentException($"Duplicate language with type name '{aqtn}'", nameof(languageTypeName));
            }

            // Add global language options first, they can be rewritten by language specific ones:
            var mergedOptions = new Dictionary<string, object>(Options);

            // Replace global options with language-specific options
            foreach (var option in options) {
                mergedOptions[option.Key] = option.Value;
            }

            var config = new LanguageConfiguration(aqtn, displayName, mergedOptions);

            _languageConfigurations.Add(aqtn, config);

            // allow duplicate ids in identifiers and extensions lists:
            foreach (var name in names) {
                _languageNames[name] = config;
            }

            foreach (var ext in fileExtensions) {
                _languageExtensions[NormalizeExtension(ext)] = config;
            }
        }

        internal static string NormalizeExtension(string extension) {
            return extension[0] == '.' ? extension : "." + extension;
        }

        internal void Freeze() {
            Debug.Assert(!_frozen);
            _frozen = true;
        }

        internal bool TryLoadLanguage(ScriptDomainManager manager, AssemblyQualifiedTypeName providerName, out LanguageContext language) {
            Assert.NotNull(manager);

            if (_languageConfigurations.TryGetValue(providerName, out LanguageConfiguration config)) {
                language = LoadLanguageContext(manager, config);
                return true;
            }

            language = null;
            return false;
        }

        internal bool TryLoadLanguage(ScriptDomainManager manager, string str, bool isExtension, out LanguageContext language) {
            Assert.NotNull(manager, str);

            var dict = isExtension ? _languageExtensions : _languageNames;

            if (dict.TryGetValue(str, out LanguageConfiguration config)) {
                language = LoadLanguageContext(manager, config);
                return true;
            }

            language = null;
            return false;
        }

        private LanguageContext LoadLanguageContext(ScriptDomainManager manager, LanguageConfiguration config) {
            var language = config.LoadLanguageContext(manager, out bool alreadyLoaded);

            if (!alreadyLoaded) {
                // Checks whether a single language is not registered under two different AQTNs.
                // We can only do it now because there is no way how to ensure that two AQTNs don't refer to the same type w/o loading the type.
                // The check takes place after config.LoadLanguageContext is called to avoid calling user code while holding a lock.
                lock (_loadedProviderTypes) {
                    Type type = language.GetType();
                    if (_loadedProviderTypes.TryGetValue(type, out LanguageConfiguration existingConfig)) {
                        throw new InvalidOperationException(
                            $"Language implemented by type '{config.ProviderName}' has already been loaded using name '{existingConfig.ProviderName}'");
                    }

                    _loadedProviderTypes.Add(type, config);
                }
            }
            return language;
        }

        public string[] GetLanguageNames(LanguageContext context) {
            ContractUtils.RequiresNotNull(context, nameof(context));

            List<string> result = new List<string>();
            
            foreach (var entry in _languageNames) {
                if (entry.Value.LanguageContext == context) {
                    result.Add(entry.Key);
                }
            }

            return result.ToArray();
        }

        internal string[] GetLanguageNames(LanguageConfiguration config) {
            List<string> result = new List<string>();

            foreach (var entry in _languageNames) {
                if (entry.Value == config) {
                    result.Add(entry.Key);
                }
            }

            return result.ToArray();
        }

        public string[] GetLanguageNames() {
            return ArrayUtils.MakeArray<string>(_languageNames.Keys);
        }

        public string[] GetFileExtensions(LanguageContext context) {
            var result = new List<string>();
            foreach (var entry in _languageExtensions) {
                if (entry.Value.LanguageContext == context) {
                    result.Add(entry.Key);
                }
            }

            return result.ToArray();
        }

        internal string[] GetFileExtensions(LanguageConfiguration config) {
            var result = new List<string>();
            foreach (var entry in _languageExtensions) {
                if (entry.Value == config) {
                    result.Add(entry.Key);
                }
            }

            return result.ToArray();
        }

        public string[] GetFileExtensions() {
            return ArrayUtils.MakeArray<string>(_languageExtensions.Keys);
        }

        internal LanguageConfiguration GetLanguageConfig(LanguageContext context) {
            foreach (var config in _languageConfigurations.Values) {
                if (config.LanguageContext == context) {
                    return config;
                }
            }
            return null;
        }
    }
}