// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Stores information needed to setup a language
    /// </summary>
    [Serializable]
    public sealed class LanguageSetup {
        private string _typeName;
        private string _displayName;
        private bool _frozen;
        private bool? _interpretedMode, _exceptionDetail, _perfStats, _noAdaptiveCompilation;

        /// <summary>
        /// Creates a new LanguageSetup
        /// </summary>
        /// <param name="typeName">assembly qualified type name of the language
        /// provider</param>
        public LanguageSetup(string typeName)
            : this(typeName, "", ArrayUtils.EmptyStrings, ArrayUtils.EmptyStrings) {
        }

        /// <summary>
        /// Creates a new LanguageSetup with the provided options
        /// TODO: remove this overload?
        /// </summary>
        public LanguageSetup(string typeName, string displayName)
            : this(typeName, displayName, ArrayUtils.EmptyStrings, ArrayUtils.EmptyStrings) {
        }

        /// <summary>
        /// Creates a new LanguageSetup with the provided options
        /// </summary>
        public LanguageSetup(string typeName, string displayName, IEnumerable<string> names, IEnumerable<string> fileExtensions) {
            ContractUtils.RequiresNotEmpty(typeName, nameof(typeName));
            ContractUtils.RequiresNotNull(displayName, nameof(displayName));
            ContractUtils.RequiresNotNull(names, nameof(names));
            ContractUtils.RequiresNotNull(fileExtensions, nameof(fileExtensions));

            _typeName = typeName;
            _displayName = displayName;
            Names = new List<string>(names);
            FileExtensions = new List<string>(fileExtensions);
            Options = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets an option as a strongly typed value.
        /// </summary>
        public T GetOption<T>(string name, T defaultValue) {
            if (Options != null && Options.TryGetValue(name, out object value)) {
                if (value is T variable) {
                    return variable;
                }
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
            }
            return defaultValue;
        }

        /// <summary>
        /// The assembly qualified type name of the language provider
        /// </summary>
        public string TypeName {
            get => _typeName;
            set {
                ContractUtils.RequiresNotEmpty(value, nameof(value));
                CheckFrozen();
                _typeName = value;
            }
        }

        /// <summary>
        /// Display name of the language. If empty, it will be set to the first
        /// name in the Names list.
        /// </summary>
        public string DisplayName {
            get => _displayName;
            set {
                ContractUtils.RequiresNotNull(value, nameof(value));
                CheckFrozen();
                _displayName = value;
            }
        }

        /// <remarks>
        /// Case-insensitive language names.
        /// </remarks>
        public IList<string> Names { get; private set; }

        /// <remarks>
        /// Case-insensitive file extension, optionally starts with a dot.
        /// </remarks>
        public IList<string> FileExtensions { get; private set; }

        /// <remarks>
        /// Option names are case-sensitive.
        /// </remarks>
        public IDictionary<string, object> Options { get; private set; }

        [Obsolete("This option is ignored")]
        public bool InterpretedMode {
            get { return GetCachedOption("InterpretedMode", ref _interpretedMode); }
            set { 
                CheckFrozen();
                Options["InterpretedMode"] = value; 
            }
        }

        [Obsolete("Use Options[\"NoAdaptiveCompilation\"] instead.")]
        public bool NoAdaptiveCompilation {
            get { return GetCachedOption("NoAdaptiveCompilation", ref _noAdaptiveCompilation); }
            set {
                CheckFrozen();
                Options["NoAdaptiveCompilation"] = value;
            }
        }

        public bool ExceptionDetail {
            get { return GetCachedOption("ExceptionDetail", ref _exceptionDetail); }
            set {
                CheckFrozen();
                Options["ExceptionDetail"] = value;
            }
        }

        [Obsolete("Use Options[\"PerfStats\"] instead.")]
        public bool PerfStats {
            get { return GetCachedOption("PerfStats", ref _perfStats); }
            set {
                CheckFrozen();
                Options["PerfStats"] = value;
            }
        }

        private bool GetCachedOption(string name, ref bool? storage) {
            if (storage.HasValue) {
                return storage.Value;
            }

            if (_frozen) {
                storage = GetOption<bool>(name, false);
                return storage.Value;
            }

            return GetOption<bool>(name, false);
        }

        internal void Freeze() {
            _frozen = true;

            Names = new ReadOnlyCollection<string>(ArrayUtils.MakeArray(Names));
            FileExtensions = new ReadOnlyCollection<string>(ArrayUtils.MakeArray(FileExtensions));
            Options = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(Options));
        }

        private void CheckFrozen() {
            if (_frozen) {
                throw new InvalidOperationException("Cannot modify LanguageSetup after it has been used to create a ScriptRuntime");
            }
        }        
    }
}
