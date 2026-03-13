// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Indicates that a DynamicMetaObject might be convertible to a CLR type.
    /// </summary>
    public interface IConvertibleMetaObject {
        bool CanConvertTo(Type type, bool isExplicit);
    }
}
