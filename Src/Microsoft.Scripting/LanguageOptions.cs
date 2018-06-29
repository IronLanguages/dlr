// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;
using System.IO;
using System.Threading;
using System.Globalization;

namespace Microsoft.Scripting {

    [Serializable]
    public class LanguageOptions {
        private bool _exceptionDetail;
        private bool _showClrExceptions;
        private bool _interpretedMode;
        private readonly bool _perfStats;
        private readonly bool _noAdaptiveCompilation;
        private readonly int _compilationThreshold;
        private readonly ReadOnlyCollection<string> _searchPaths;

        /// <summary>
        /// Dynamically choose between interpreting, simple compilation and compilation
        /// that takes advantage of runtime history.
        /// </summary>
        public bool NoAdaptiveCompilation {
            get { return _noAdaptiveCompilation; }
        } 
        
        /// <summary>
        /// The number of iterations before the interpreter starts compiling.s
        /// </summary>
        public int CompilationThreshold {
            get { return _compilationThreshold; }
        }

        // TODO: remove
        [Obsolete("No longer used.")]
        public bool InterpretedMode {
            get { return _interpretedMode; }
            set { _interpretedMode = value; }
        }

        /// <summary>
        ///  Display exception detail (callstack) when exception gets caught
        /// </summary>
        public bool ExceptionDetail {
            get { return _exceptionDetail; }
            set { _exceptionDetail = value; }
        }

        public bool ShowClrExceptions {
            get { return _showClrExceptions; }
            set { _showClrExceptions = value; }
        }

        /// <summary>
        /// Whether to gather performance statistics.
        /// </summary>
        public bool PerfStats {
            get { return _perfStats; }
        }

        /// <summary>
        /// Initial file search paths provided by the host.
        /// </summary>
        public ReadOnlyCollection<string> SearchPaths {
            get { return _searchPaths; }
        }

        public LanguageOptions() 
            : this(null) {
        }

        public LanguageOptions(IDictionary<string, object> options) {
            _interpretedMode = GetOption(options, "InterpretedMode", false);
            _exceptionDetail = GetOption(options, "ExceptionDetail", false);
            _showClrExceptions = GetOption(options, "ShowClrExceptions", false);
            _perfStats = GetOption(options, "PerfStats", false);
            _noAdaptiveCompilation = GetOption(options, "NoAdaptiveCompilation", false);
            _compilationThreshold = GetOption(options, "CompilationThreshold", -1);
            _searchPaths = GetSearchPathsOption(options) ?? new ReadOnlyCollection<string>(new string[0]);
        }

        public static T GetOption<T>(IDictionary<string, object> options, string name, T defaultValue) {
            if (options != null && options.TryGetValue(name, out object value)) {
                if (value is T variable) {
                    return variable;
                }
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Reads an option whose value is expected to be a collection of non-null strings.
        /// Reaturns a read-only copy of the option's value.
        /// </summary>
        public static ReadOnlyCollection<string> GetStringCollectionOption(IDictionary<string, object> options, string name, params char[] separators) {
            if (options == null || !options.TryGetValue(name, out object value)) {
                return null;
            }

            // a collection:
            if (value is ICollection<string> collection) {
                foreach (var item in collection) {
                    if (item == null) {
                        throw new ArgumentException(
                            $"Invalid value for option {name}: collection shouldn't containt null items");
                    }
                }

                return new ReadOnlyCollection<string>(ArrayUtils.MakeArray(collection));
            }

            // a string:
            if (value is string strValue && separators != null && separators.Length > 0) {
                return new ReadOnlyCollection<string>(StringUtils.Split(strValue, separators, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries));
            }

            throw new ArgumentException($"Invalid value for option {name}");
        }

        public static ReadOnlyCollection<string> GetSearchPathsOption(IDictionary<string, object> options) {
            return GetStringCollectionOption(options, "SearchPaths", Path.PathSeparator);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly ReadOnlyCollection<string> EmptyStringCollection = new ReadOnlyCollection<string>(ArrayUtils.EmptyStrings);
    }
}
