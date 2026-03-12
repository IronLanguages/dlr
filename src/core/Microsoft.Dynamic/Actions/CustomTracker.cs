// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// A custom member tracker which enables languages to plug in arbitrary
    /// members into the lookup process.
    /// </summary>
    public abstract class CustomTracker : MemberTracker {
        protected CustomTracker() {
        }

        public sealed override TrackerTypes MemberType => TrackerTypes.Custom;
    }
}
