// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Stores information needed to setup a ScriptRuntime
    /// </summary>
    [Serializable]
    public sealed class ScriptRuntimeSetup {
        // host specification:
        private Type _hostType;
        private IList<object> _hostArguments;

        // DLR options:
        private bool _debugMode;
        private bool _privateBinding;

        // true if the ScriptRuntimeSetup is no longer mutable because it's been
        // used to start a ScriptRuntime
        private bool _frozen;

        public ScriptRuntimeSetup() {
            LanguageSetups = new List<LanguageSetup>();
            Options = new Dictionary<string, object>();
            _hostType = typeof(ScriptHost);
            _hostArguments = ArrayUtils.EmptyObjects;
        }

        /// <summary>
        /// The list of language setup information for languages to load into
        /// the runtime
        /// </summary>
        public IList<LanguageSetup> LanguageSetups { get; private set; }

        /// <summary>
        /// Indicates that the script runtime is in debug mode.
        /// This means:
        /// 
        /// 1) Symbols are emitted for debuggable methods (methods associated with SourceUnit).
        /// 2) Debuggable methods are emitted to non-collectable types (this is due to CLR limitations on dynamic method debugging).
        /// 3) JIT optimization is disabled for all methods
        /// 4) Languages may disable optimizations based on this value.
        /// </summary>
        public bool DebugMode {
            get => _debugMode;
            set {
                CheckFrozen();
                _debugMode = value; 
            }
        }

        /// <summary>
        /// Ignore CLR visibility checks
        /// </summary>
        public bool PrivateBinding {
            get => _privateBinding;
            set {
                CheckFrozen();
                _privateBinding = value; 
            }
        }

        /// <summary>
        /// Can be any derived class of ScriptHost. When set, it allows the
        /// host to override certain methods to control behavior of the runtime
        /// </summary>
        public Type HostType {
            get => _hostType;
            set {
                ContractUtils.RequiresNotNull(value, nameof(value));
                ContractUtils.Requires(typeof(ScriptHost).IsAssignableFrom(value), nameof(value), "Must be ScriptHost or a derived type of ScriptHost");
                CheckFrozen();
                _hostType = value;
            }
        }

        /// <remarks>
        /// Option names are case-sensitive.
        /// </remarks>
        public IDictionary<string, object> Options { get; private set; }

        /// <summary>
        /// Arguments passed to the host type when it is constructed
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IList<object> HostArguments {
            get => _hostArguments;
            set {
                ContractUtils.RequiresNotNull(value, nameof(value));
                CheckFrozen();
                _hostArguments = value;
            }
        }

        internal DlrConfiguration ToConfiguration() {
            ContractUtils.Requires(LanguageSetups.Count > 0, "ScriptRuntimeSetup must have at least one LanguageSetup");

            // prepare
            ReadOnlyCollection<LanguageSetup> setups = new ReadOnlyCollection<LanguageSetup>(ArrayUtils.MakeArray(LanguageSetups));
            var hostArguments = new ReadOnlyCollection<object>(ArrayUtils.MakeArray(_hostArguments));
            var options = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(Options));            
            var config = new DlrConfiguration(_debugMode, _privateBinding, options);

            // validate
            foreach (var language in setups) {
                config.AddLanguage(
                    language.TypeName,
                    language.DisplayName,
                    language.Names,
                    language.FileExtensions,
                    language.Options
                );
            }

            // commit
            LanguageSetups = setups;
            Options = options;
            _hostArguments = hostArguments;

            Freeze(setups);

            return config;
        }

        private void Freeze(ReadOnlyCollection<LanguageSetup> setups) {
            foreach (var language in setups) {
                language.Freeze();
            }

            _frozen = true;
        }

        private void CheckFrozen() {
            if (_frozen) {
                throw new InvalidOperationException("Cannot modify ScriptRuntimeSetup after it has been used to create a ScriptRuntime");
            }            
        }
        
        /// <summary>
        /// Reads setup from .NET configuration system (.config files).
        /// If there is no configuration available returns an empty setup.
        /// </summary>
        public static ScriptRuntimeSetup ReadConfiguration() {
#if FEATURE_CONFIGURATION
            var setup = new ScriptRuntimeSetup();
            Configuration.Section.LoadRuntimeSetup(setup, null);
            return setup;
#else
            return new ScriptRuntimeSetup();
#endif
        }

#if FEATURE_CONFIGURATION
        /// <summary>
        /// Reads setup from a specified XML stream.
        /// </summary>
        public static ScriptRuntimeSetup ReadConfiguration(Stream configFileStream) {
            ContractUtils.RequiresNotNull(configFileStream, nameof(configFileStream));
            var setup = new ScriptRuntimeSetup();
            Configuration.Section.LoadRuntimeSetup(setup, configFileStream);
            return setup;
        }

        /// <summary>
        /// Reads setup from a specified XML file.
        /// </summary>
        public static ScriptRuntimeSetup ReadConfiguration(string configFilePath) {
            ContractUtils.RequiresNotNull(configFilePath, nameof(configFilePath));

            using (var stream = File.OpenRead(configFilePath)) {
                return ReadConfiguration(stream);
            }
        }
#endif
    }
}
