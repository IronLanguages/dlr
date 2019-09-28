// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CONFIGURATION

using System.Configuration;

namespace Microsoft.Scripting.Hosting.Configuration {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    public class LanguageElementCollection : ConfigurationElementCollection {
        public override ConfigurationElementCollectionType CollectionType =>
            ConfigurationElementCollectionType.BasicMap;

        protected override bool ThrowOnDuplicate => false;

        protected override ConfigurationElement CreateNewElement() => new LanguageElement();

        protected override string ElementName => "language";

        protected override object GetElementKey(ConfigurationElement element) =>
            ((LanguageElement)element).Type;
    }
}

#endif
