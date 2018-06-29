// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// This attribute marks a parameter that is not allowed to be null.
    /// It is used by the method binding infrastructure to generate better error 
    /// messages and method selection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class NotNullAttribute : Attribute {
    }

    /// <summary>
    /// This attribute marks a parameter whose type is an array that is not allowed to have null items.
    /// It is used by the method binding infrastructure to generate better error 
    /// messages and method selection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class NotNullItemsAttribute : Attribute {
    }
}
