// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Indicates that a MetaObject is already representing a restricted type.  Useful
    /// when we're already restricted to a known type but this isn't captured in
    /// the type info (e.g. the type is not sealed).
    /// </summary>
    public interface IRestrictedMetaObject {
        DynamicMetaObject Restrict(Type type);
    }
}
