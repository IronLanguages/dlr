// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Represents a host-provided variables for executable code.  The variables are
    /// typically backed by a host-provided dictionary. Languages can also associate per-language
    /// information with the context by using scope extensions.  This can be used for tracking
    /// state which is used across multiple executions, for providing custom forms of 
    /// storage (for example object keyed access), or other language specific semantics.
    /// 
    /// Scope objects are thread-safe as long as their underlying storage is thread safe.
    /// 
    /// Script hosts can choose to use thread safe or thread unsafe modules but must be sure
    /// to constrain the code they right to be single-threaded if using thread unsafe
    /// storage.
    /// </summary>
    public sealed class Scope : IDynamicMetaObjectProvider {
        private ScopeExtension[] _extensions; // resizable
        private readonly object _extensionsLock = new object();
        private readonly IDynamicMetaObjectProvider _storage;

        /// <summary>
        /// Creates a new scope with a new empty thread-safe dictionary.  
        /// </summary>
        public Scope() {
            _extensions = ScopeExtension.EmptyArray;
            _storage = new ScopeStorage();
        }

        public Scope(IDictionary<string, object> dictionary) {
            _extensions = ScopeExtension.EmptyArray;
            _storage = new StringDictionaryExpando(dictionary);
        }

        /// <summary>
        /// Creates a new scope which is backed by an arbitrary object for it's storage.
        /// </summary>
        /// <param name="storage"></param>
        public Scope(IDynamicMetaObjectProvider storage) {
            _extensions = ScopeExtension.EmptyArray;
            _storage = storage;
        }

        /// <summary>
        /// Gets the ScopeExtension associated with the provided ContextId.
        /// </summary>
        public ScopeExtension GetExtension(ContextId languageContextId) {
            return (languageContextId.Id < _extensions.Length) ? _extensions[languageContextId.Id] : null;
        }
        
        /// <summary>
        /// Sets the ScopeExtension to the provided value for the given ContextId.  
        /// 
        /// The extension can only be set once.  The returned value is either the new ScopeExtension
        /// if no value was previously set or the previous value.
        /// </summary>
        public ScopeExtension SetExtension(ContextId languageContextId, ScopeExtension extension) {
            ContractUtils.RequiresNotNull(extension, nameof(extension));

            lock (_extensionsLock) {
                if (languageContextId.Id >= _extensions.Length) {
                    Array.Resize(ref _extensions, languageContextId.Id + 1);
                }

                return _extensions[languageContextId.Id] ?? (_extensions[languageContextId.Id] = extension);
            }
        }

        public dynamic Storage => _storage;

        internal sealed class MetaScope : DynamicMetaObject {
            public MetaScope(Expression parameter, Scope scope)
                : base(parameter, BindingRestrictions.Empty, scope) {
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {
                return Restrict(StorageMetaObject.BindGetMember(binder));
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) {
                return Restrict(StorageMetaObject.BindInvokeMember(binder, args));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) {                
                return Restrict(StorageMetaObject.BindSetMember(binder, value));
            }

            public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder) {
                return Restrict(StorageMetaObject.BindDeleteMember(binder));
            }

            private DynamicMetaObject Restrict(DynamicMetaObject result) {
                if (Expression.Type == typeof(Scope)) {
                    // ideal binding, we add no new restrictions if we're binding against a strongly typed Scope
                    return result;
                }

                // Un-ideal binding: we add restrictions.
                return new DynamicMetaObject(result.Expression, BindingRestrictions.GetTypeRestriction(Expression, typeof(Scope)).Merge(result.Restrictions));
            }

            private DynamicMetaObject StorageMetaObject => DynamicMetaObject.Create(Value._storage, StorageExpression);

            private MemberExpression StorageExpression => Expression.Property(
                Expression.Convert(Expression, typeof(Scope)),
                typeof(Scope).GetProperty("Storage")
            );

            public override IEnumerable<string> GetDynamicMemberNames() {
                return StorageMetaObject.GetDynamicMemberNames();
            }

            public new Scope Value => (Scope)base.Value;
        }

        #region IDynamicMetaObjectProvider Members

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new MetaScope(parameter, this);
        }

        #endregion
    }
}
