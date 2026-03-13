// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.Collections.Generic;

using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// Provides documentation against live objects for use in a REPL window.
    /// </summary>
    public sealed class DocumentationOperations : MarshalByRefObject {
        private readonly DocumentationProvider _provider;

        internal DocumentationOperations(DocumentationProvider provider) {
            _provider = provider;
        }
        
        /// <summary>
        /// Gets the available members defined on the provided object.
        /// </summary>
        public ICollection<MemberDoc> GetMembers(object value) {
            return _provider.GetMembers(value);
        }

        /// <summary>
        /// Gets the overloads available for the provided object if it is invokable.
        /// </summary>
        public ICollection<OverloadDoc> GetOverloads(object value) {
            return _provider.GetOverloads(value);
        }

#if FEATURE_REMOTING
        /// <summary>
        /// Gets the available members on the provided remote object.
        /// </summary>
        public ICollection<MemberDoc> GetMembers(ObjectHandle value) {
            return _provider.GetMembers(value.Unwrap());
        }

        /// <summary>
        /// Gets the overloads available for the provided remote object if it is invokable.
        /// </summary>
        public ICollection<OverloadDoc> GetOverloads(ObjectHandle value) {
            return _provider.GetOverloads(value.Unwrap());
        }

        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
 }
}
