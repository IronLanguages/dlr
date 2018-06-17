// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Implemented by binders which support light exceptions.  Dynamic objects
    /// binding against a binder which implements this interface can check 
    /// SupportsLightThrow to see if the binder currently supports safely 
    /// returning a light exception.  Light exceptions can be created with
    /// LightException.Throw.
    ///
    /// Binders also need to implement GetlightBinder.  This method
    /// returns a new call site binder which may return light  exceptions if 
    /// the binder supports them.
    /// </summary>
    public interface ILightExceptionBinder {
        /// <summary>
        /// Returns true if a callsite binding against this binder can
        /// return light exceptions.
        /// </summary>
        bool SupportsLightThrow { get; }

        /// <summary>
        /// Gets a binder which will support light exception if one is
        /// available.
        /// </summary>
        CallSiteBinder GetLightExceptionBinder();
    }
}
