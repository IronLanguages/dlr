// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Microsoft.Scripting.Hosting;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Provides language specific documentation for live objects.
    /// </summary>
    public abstract class DocumentationProvider {
        public abstract ICollection<MemberDoc> GetMembers(object value);
        public abstract ICollection<OverloadDoc> GetOverloads(object value);
    }
}
