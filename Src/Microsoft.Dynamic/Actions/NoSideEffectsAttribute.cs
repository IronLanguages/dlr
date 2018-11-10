// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Marks a method as not having side effects.  used by the combo binder
    /// to allow calls to methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public sealed class NoSideEffectsAttribute : Attribute {
    }
}
