// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Scripting.Runtime {

    /// <summary>
    /// Provides a list of all the members of an instance.  
    /// </summary>
    public interface IMembersList {
        IList<string> GetMemberNames();
    }
}
