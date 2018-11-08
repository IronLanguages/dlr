// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CONFIGURATION

using System.Configuration;
using System;
using System.Collections.Generic;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting.Configuration {

    public class OptionElement : ConfigurationElement {
        private const string _Option = "option";
        private const string _Value = "value";
        private const string _Language = "language";

        private static ConfigurationPropertyCollection _Properties = new ConfigurationPropertyCollection() {
            new ConfigurationProperty(_Option, typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey),
            new ConfigurationProperty(_Value, typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired),
            new ConfigurationProperty(_Language, typeof(string), String.Empty, ConfigurationPropertyOptions.IsKey),
        };

        protected override ConfigurationPropertyCollection Properties {
            get { return _Properties; }
        }

        public string Name {
            get { return (string)this[_Option]; }
            set { this[_Option] = value; }
        }

        public string Value {
            get { return (string)this[_Value]; }
            set { this[_Value] = value; }
        }

        public string Language {
            get { return (string)this[_Language]; }
            set { this[_Language] = value; }
        }

        internal object GetKey() {
            return new Key(Name, Language);
        }

        internal sealed class Key : IEquatable<Key> {
            private readonly string _option;
            private readonly string _language;

            public string Option { get { return _option; } }
            public string Language { get { return _language; } }
            
            public Key(string option, string language) {
                _option = option;
                _language = language;
            }

            public override bool Equals(object obj) => Equals(obj as Key);

            public bool Equals(Key other) {
                return other != null &&
                    DlrConfiguration.OptionNameComparer.Equals(_option, other._option) &&
                    DlrConfiguration.LanguageNameComparer.Equals(_language, other._language);
            }

            public override int GetHashCode() {
                return _option.GetHashCode() ^ (_language ?? String.Empty).GetHashCode();
            }

            public override string ToString() {
                return (String.IsNullOrEmpty(_language) ? String.Empty : _language + ":") + _option;
            }
        }
    }
}
#endif
