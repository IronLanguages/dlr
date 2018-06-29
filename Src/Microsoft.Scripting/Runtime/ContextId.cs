/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Represents a language context.  Typically there is at most 1 context 
    /// associated with each language, but some languages may use more than one context
    /// to identify code that should be treated differently.  Contexts are used during
    /// member and operator lookup.
    /// </summary>
    [Serializable]
    public readonly struct ContextId : IEquatable<ContextId> {
        private static Dictionary<object, ContextId> _contexts = new Dictionary<object,ContextId>();
        private static int _maxId = 1;

        public static readonly ContextId Empty = new ContextId();

        internal ContextId(int id) {
            Id = id;
        }

        /// <summary>
        /// Registers a language within the system with the specified name.
        /// </summary>
        public static ContextId RegisterContext(object identifier) {
            lock(_contexts) {
                if (_contexts.TryGetValue(identifier, out ContextId _)) {
                    throw Error.LanguageRegistered();
                }

                return new ContextId(_maxId++);
            }
        }

        /// <summary>
        /// Looks up the context ID for the specified context identifier
        /// </summary>
        public static ContextId LookupContext(object identifier) {
            lock (_contexts) {
                if (_contexts.TryGetValue(identifier, out ContextId res)) {
                    return res;
                }
            }

            return Empty;
        }

        public int Id { get; }

        #region IEquatable<ContextId> Members

        public bool Equals(ContextId other) => Id == other.Id;

        #endregion

        #region Object overrides

        public override int GetHashCode() => Id;

        public override bool Equals(object obj) => obj is ContextId other && Equals(other);

        #endregion

        public static bool operator ==(ContextId self, ContextId other) {
            return self.Equals(other);
        }

        public static bool operator !=(ContextId self, ContextId other) {
            return !self.Equals(other);
        }
    }
}
