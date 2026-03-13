// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Marks a method which may return a light exception.  Such
    /// methods need to have their return value checked and the exception
    /// will need to be thrown if the caller is not light exception aware.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LightThrowingAttribute : Attribute {
    }
}
