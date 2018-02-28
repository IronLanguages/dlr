// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CONFIGURATION

using System.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.Scripting.Hosting.Configuration {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    public class LanguageElementCollection : ConfigurationElementCollection {
        public override ConfigurationElementCollectionType CollectionType {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override bool ThrowOnDuplicate {
            get { return false; }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new LanguageElement();
        }

        protected override string ElementName {
            get { return "language"; }
        }

        protected override object GetElementKey(ConfigurationElement element) {
            return ((LanguageElement)element).Type;
        }
    }
}

#endif
