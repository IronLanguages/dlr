// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.Utils {

    /// <summary>
    /// Changes the semantics of GC handle to return <c>null</c> instead of throwing
    /// an <see cref="InvalidOperationException"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")] // TODO: fix
    public struct WeakHandle {

        private readonly GCHandle _gcHandle;

        public WeakHandle(object target, bool trackResurrection) {
            _gcHandle = GCHandle.Alloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
        }

        public object Target {
            get {
                if (!_gcHandle.IsAllocated)
                    return null;
                try {
                    return _gcHandle.Target;
                } catch (InvalidOperationException) {
                    return null;
                }
            }
        }

        public void Free() {
            if (!_gcHandle.IsAllocated)
                return;
            try {
                _gcHandle.Free();
            }
            catch (InvalidOperationException) {
            }
        }
    }
}
