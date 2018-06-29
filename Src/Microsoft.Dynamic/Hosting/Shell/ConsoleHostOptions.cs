// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_FULL_CONSOLE

using System.Collections.Generic;
using Microsoft.Scripting.Utils;
using System.Reflection;

namespace Microsoft.Scripting.Hosting.Shell {

    public class ConsoleHostOptions {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")] // TODO: fix
        public enum Action {
            None,
            RunConsole,
            RunFile,
            DisplayHelp
        }

        public List<string> IgnoredArgs { get; } = new List<string>();
        public string RunFile { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")] // TODO: fix
        public string[] SourceUnitSearchPaths { get; set; }
        public Action RunAction { get; set; }
        public List<string> EnvironmentVars { get; } = new List<string>();
        public string LanguageProvider { get; set; }
        public bool HasLanguageProvider { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")] // TODO: fix
        public string[,] GetHelp() {
            return new string[,] {
                { "/help",                     "Displays this help." },
                { "/lang:<extension>",         "Specify language by the associated extension (py, js, vb, rb). Determined by an extension of the first file. Defaults to IronPython." },
                { "/paths:<file-path-list>",   "Semicolon separated list of import paths (/run only)." },
                { "/setenv:<var1=value1;...>", "Sets specified environment variables for the console process. Not available on Silverlight." },
            };
        }
    }
}
#endif