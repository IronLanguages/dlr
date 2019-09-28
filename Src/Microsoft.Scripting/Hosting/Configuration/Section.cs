// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CONFIGURATION

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Configuration {

    //
    // <configSections>
    //   <section name="microsoft.scripting" type="Microsoft.Scripting.Hosting.Configuration.Section, Microsoft.Scripting" />
    // </configSections>
    //
    // <microsoft.scripting [debugMode="{bool}"]? [privateBinding="{bool}"]?>
    //   <languages>  <!-- BasicMap with key (type): inherits language nodes, overwrites previous nodes (last wins) -->
    //     <language names="{(semi)colon-separated}" extensions="{(semi)colon-separated-with-optional-dot}" type="{AQTN}" [displayName="{string}"]? />
    //   </languages>
    //
    //   <options>    <!-- AddRemoveClearMap with key (option, [language]?): inherits language nodes, overwrites previous nodes (last wins) -->
    //     <set option="{string}" value="{string}" [language="{language-name}"]? />
    //     <clear />
    //     <remove option="{string}" [language="{language-name}"]? />
    //   </options>
    //
    // </microsoft.scripting>
    //
    public class Section : ConfigurationSection {
        public static readonly string SectionName = "microsoft.scripting";

        private const string _DebugMode = "debugMode";
        private const string _PrivateBinding = "privateBinding";
        private const string _Languages = "languages";
        private const string _Options = "options";

        private static ConfigurationPropertyCollection _Properties = new ConfigurationPropertyCollection() {
            new ConfigurationProperty(_DebugMode, typeof(bool?), null), 
            new ConfigurationProperty(_PrivateBinding, typeof(bool?), null), 
            new ConfigurationProperty(_Languages, typeof(LanguageElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection), 
            new ConfigurationProperty(_Options, typeof(OptionElementCollection), null), 
        };

        protected override ConfigurationPropertyCollection Properties => _Properties;

        public bool? DebugMode {
            get => (bool?)base[_DebugMode];
            set => base[_DebugMode] = value;
        }

        public bool? PrivateBinding {
            get => (bool?)base[_PrivateBinding];
            set => base[_PrivateBinding] = value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<LanguageElement> GetLanguages() {
            if (!(this[_Languages] is LanguageElementCollection languages)) {
                yield break;
            }

            foreach (var languageConfig in languages) {
                yield return (LanguageElement)languageConfig;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<OptionElement> GetOptions() {
            if (!(this[_Options] is OptionElementCollection options)) {
                yield break;
            }

            foreach (var option in options) {
                yield return (OptionElement)option;
            }
        }

        private static Section LoadFromFile(Stream configFileStream) {
            var result = new Section();
            using (var reader = XmlReader.Create(configFileStream)) {
                if (reader.ReadToDescendant("configuration") && reader.ReadToDescendant(SectionName)) {
                    result.DeserializeElement(reader, false);
                } else {
                    return null;
                }
            }
            return result;
        }

        internal static void LoadRuntimeSetup(ScriptRuntimeSetup setup, Stream configFileStream) {
            var config = configFileStream != null
                ? LoadFromFile(configFileStream)
                : System.Configuration.ConfigurationManager.GetSection(SectionName) as Section;

            if (config == null) {
                return;
            }

            if (config.DebugMode.HasValue) {
                setup.DebugMode = config.DebugMode.Value;
            }
            if (config.PrivateBinding.HasValue) {
                setup.PrivateBinding = config.PrivateBinding.Value;
            }

            foreach (var languageConfig in config.GetLanguages()) {
                var provider = languageConfig.Type;
                var names = languageConfig.GetNamesArray();
                var extensions = languageConfig.GetExtensionsArray();
                var displayName = languageConfig.DisplayName ?? ((names.Length > 0) ? names[0] : languageConfig.Type);

                // Honor the latest-wins behavior of the <languages> tag for options that were already included in the setup object;
                // Keep the options though.
                bool found = false;
                foreach (var language in setup.LanguageSetups) {
                    if (language.TypeName == provider) {
                        language.Names.Clear();
                        foreach (string name in names) {
                            language.Names.Add(name);
                        }
                        language.FileExtensions.Clear();
                        foreach (string extension in extensions) {
                            language.FileExtensions.Add(extension);
                        }
                        language.DisplayName = displayName;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    setup.LanguageSetups.Add(new LanguageSetup(provider, displayName, names, extensions));
                }
            }

            foreach (var option in config.GetOptions()) {
                if (string.IsNullOrEmpty(option.Language)) {
                    // common option:
                    setup.Options[option.Name] = option.Value;
                } else {
                    // language specific option:
                    bool found = false;
                    foreach (var language in setup.LanguageSetups) {
                        if (language.Names.Any(s => DlrConfiguration.LanguageNameComparer.Equals(s, option.Language))) {
                            language.Options[option.Name] = option.Value;
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        throw new ConfigurationErrorsException($"Unknown language name: '{option.Language}'");
                    }
                }
            }
        }
    }
}

#endif
