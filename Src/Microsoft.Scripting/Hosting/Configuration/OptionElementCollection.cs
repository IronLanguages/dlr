﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CONFIGURATION

using System.Configuration;

namespace Microsoft.Scripting.Hosting.Configuration {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    public class OptionElementCollection : ConfigurationElementCollection {
        public OptionElementCollection() {
            AddElementName = "set";
        }

        public override ConfigurationElementCollectionType CollectionType =>
            ConfigurationElementCollectionType.AddRemoveClearMap;

        protected override ConfigurationElement CreateNewElement() => new OptionElement();

        protected override bool ThrowOnDuplicate => false;

        protected override object GetElementKey(ConfigurationElement element) =>
            ((OptionElement)element).GetKey();
    }
}

#endif
