﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CONFIGURATION

using System;
using System.Configuration;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Configuration {
    // <language names="IronPython;Python;py" extensions=".py" type="AQTN" displayName="IronPython v2">
    //    <option name="foo" value="bar" />
    // </language>
    public class LanguageElement : ConfigurationElement {
        private const string _Names = "names";
        private const string _Extensions = "extensions";
        private const string _Type = "type";
        private const string _DisplayName = "displayName";

        private static ConfigurationPropertyCollection _Properties = new ConfigurationPropertyCollection {
            new ConfigurationProperty(_Names, typeof(string), null),
            new ConfigurationProperty(_Extensions, typeof(string), null),
            new ConfigurationProperty(_Type, typeof(string), null, ConfigurationPropertyOptions.IsRequired),
            new ConfigurationProperty(_DisplayName, typeof(string), null)
        };

        protected override ConfigurationPropertyCollection Properties => _Properties;

        public string Names {
            get => (string)this[_Names];
            set => this[_Names] = value;
        }

        public string Extensions {
            get => (string)this[_Extensions];
            set => this[_Extensions] = value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public string Type {
            get => (string)this[_Type];
            set => this[_Type] = value;
        }

        public string DisplayName {
            get => (string)this[_DisplayName];
            set => this[_DisplayName] = value;
        }

        public string[] GetNamesArray() => Split(Names);

        public string[] GetExtensionsArray() => Split(Extensions);

        private static string[] Split(string str) =>
            str != null
                ? str.Split(new[] {';', ','}, StringSplitOptions.RemoveEmptyEntries)
                : ArrayUtils.EmptyStrings;
    }
}

#endif
