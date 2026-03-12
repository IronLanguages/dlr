// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Scripting {
    /// <summary>
    /// marks a field, class, or struct as being safe to have statics which can be accessed
    /// from multiple runtimes.
    /// 
    /// Static fields which are not read-only or marked with this attribute will be flagged 
    /// by a test which looks for state being shared between runtimes.  Before applying this
    /// attribute you should ensure that it is safe to share the state.  This is typically
    /// state which is lazy initialized or state which is caching values which are identical
    /// in all runtimes and are immutable.
    /// </summary>
    [Conditional("DEBUG")]
    [AttributeUsage(AttributeTargets.Field)]   
    public sealed class MultiRuntimeAwareAttribute : Attribute {
    }
}
