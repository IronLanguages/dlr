// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CONFIGURATION

using System;
using System.Configuration;

using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting.Configuration {

    public class OptionElement : ConfigurationElement {
        private const string _Option = "option";
        private const string _Value = "value";
        private const string _Language = "language";

        private static ConfigurationPropertyCollection _Properties = new ConfigurationPropertyCollection() {
            new ConfigurationProperty(_Option, typeof(string), string.Empty, ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey),
            new ConfigurationProperty(_Value, typeof(string), string.Empty, ConfigurationPropertyOptions.IsRequired),
            new ConfigurationProperty(_Language, typeof(string), string.Empty, ConfigurationPropertyOptions.IsKey),
        };

        protected override ConfigurationPropertyCollection Properties => _Properties;

        public string Name {
            get => (string)this[_Option];
            set => this[_Option] = value;
        }

        public string Value {
            get => (string)this[_Value];
            set => this[_Value] = value;
        }

        public string Language {
            get => (string)this[_Language];
            set => this[_Language] = value;
        }

        internal object GetKey() => new Key(Name, Language);

        internal sealed class Key : IEquatable<Key> {
            public string Option { get; }
            public string Language { get; }

            public Key(string option, string language) {
                Option = option;
                Language = language;
            }

            public override bool Equals(object obj) => Equals(obj as Key);

            public bool Equals(Key other) =>
                other != null &&
                DlrConfiguration.OptionNameComparer.Equals(Option, other.Option) &&
                DlrConfiguration.LanguageNameComparer.Equals(Language, other.Language);

            public override int GetHashCode() =>
                Option.GetHashCode() ^ (Language ?? string.Empty).GetHashCode();

            public override string ToString() =>
                (string.IsNullOrEmpty(Language) ? string.Empty : Language + ":") + Option;
        }
    }
}
#endif
