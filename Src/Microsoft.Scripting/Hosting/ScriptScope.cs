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
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.Serialization;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {
    /// <summary>
    /// A ScriptScope is a unit of execution for code.  It consists of a global Scope which
    /// all code executes in.  A ScriptScope can have an arbitrary initializer and arbitrary
    /// reloader. 
    /// 
    /// ScriptScope is not thread safe. Host should either lock when multiple threads could 
    /// access the same module or should make a copy for each thread.
    ///
    /// Hosting API counterpart for <see cref="Scope"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(ScriptScope.DebugView))]
    public sealed class ScriptScope : MarshalByRefObject, IDynamicMetaObjectProvider {
        internal ScriptScope(ScriptEngine engine, Scope scope) {
            Assert.NotNull(engine, scope);
            Scope = scope;
            Engine = engine;
        }

        internal Scope Scope { get; }

        /// <summary>
        /// Gets an engine for the language associated with this scope.
        /// Returns invariant engine if the scope is language agnostic.
        /// </summary>
        public ScriptEngine Engine { get; }

        /// <summary>
        /// Gets a value stored in the scope under the given name.
        /// </summary>
        /// <exception cref="MissingMemberException">The specified name is not defined in the scope.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public dynamic GetVariable(string name) {
            return Engine.LanguageContext.ScopeGetVariable(Scope, name);
        }

        /// <summary>
        /// Gets a value stored in the scope under the given name.
        /// Converts the result to the specified type using the conversion that the language associated with the scope defines.
        /// If no language is associated with the scope, the default CLR conversion is attempted.
        /// </summary>
        /// <exception cref="MissingMemberException">The specified name is not defined in the scope.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public T GetVariable<T>(string name) {
            return Engine.LanguageContext.ScopeGetVariable<T>(Scope, name);
        }

        /// <summary>
        /// Tries to get a value stored in the scope under the given name.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public bool TryGetVariable(string name, out dynamic value) {
            return Engine.LanguageContext.ScopeTryGetVariable(Scope, name, out value);
        }

        /// <summary>
        /// Tries to get a value stored in the scope under the given name.
        /// Converts the result to the specified type using the conversion that the language associated with the scope defines.
        /// If no language is associated with the scope, the default CLR conversion is attempted.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public bool TryGetVariable<T>(string name, out T value) {
            if(Engine.LanguageContext.ScopeTryGetVariable(Scope, name, out object result)) {
                value = Engine.Operations.ConvertTo<T>(result);
                return true;
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// Sets the name to the specified value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public void SetVariable(string name, object value) {
            Engine.LanguageContext.ScopeSetVariable(Scope, name, value);
        }

#if FEATURE_REMOTING
        /// <summary>
        /// Gets a handle for a value stored in the scope under the given name.
        /// </summary>
        /// <exception cref="MissingMemberException">The specified name is not defined in the scope.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public ObjectHandle GetVariableHandle(string name) {
            return new ObjectHandle((object)GetVariable(name));
        }

        /// <summary>
        /// Tries to get a handle for a value stored in the scope under the given name.
        /// Returns <c>true</c> if there is such name, <c>false</c> otherwise. 
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public bool TryGetVariableHandle(string name, out ObjectHandle handle) {
            if (TryGetVariable(name, out object value)) {
                handle = new ObjectHandle(value);
                return true;
            }

            handle = null;
            return false;
        }

        /// <summary>
        /// Sets the name to the specified value.
        /// </summary>
        /// <exception cref="SerializationException">
        /// The value held by the handle isn't from the scope's app-domain and isn't serializable or MarshalByRefObject.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="handle"/> is a <c>null</c> reference.</exception>
        public void SetVariable(string name, ObjectHandle handle) {
            ContractUtils.RequiresNotNull(handle, nameof(handle));
            SetVariable(name, handle.Unwrap());
        }
#endif

        /// <summary>
        /// Determines if this context or any outer scope contains the defined name.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public bool ContainsVariable(string name) {
            object dummy;
            return TryGetVariable(name, out dummy);
        }

        /// <summary>
        /// Removes the variable of the given name from this scope.
        /// </summary> 
        /// <returns><c>true</c> if the value existed in the scope before it has been removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <c>null</c> reference.</exception>
        public bool RemoveVariable(string name) {
            if (Engine.Operations.ContainsMember(Scope, name)) {
                Engine.Operations.RemoveMember(Scope, name);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a list of variable names stored in the scope.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<string> GetVariableNames() {
            // Remoting: we eagerly enumerate all variables to avoid cross domain calls for each item.
            return Engine.Operations.GetMemberNames((object)Scope.Storage);
        }

        /// <summary>
        /// Gets an array of variable names and their values stored in the scope.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<KeyValuePair<string, dynamic>> GetItems() {
            // Remoting: we eagerly enumerate all variables to avoid cross domain calls for each item.
            var result = new List<KeyValuePair<string, object>>();
            
            foreach (string name in GetVariableNames()) {
                result.Add(new KeyValuePair<string, object>(name, (object)Engine.Operations.GetMember((object)Scope.Storage, name)));
            }

            result.TrimExcess();
            return result;
        }

        #region DebugView

        internal sealed class DebugView {
            private readonly ScriptScope _scope;

            public DebugView(ScriptScope scope) {
                Assert.NotNull(scope);
                _scope = scope;
            }

            public ScriptEngine Language => _scope.Engine;

            public System.Collections.Hashtable Variables {
                get {
                    System.Collections.Hashtable result = new System.Collections.Hashtable();
                    foreach (var variable in _scope.GetItems()) {
                        result[variable.Key] = (object)variable.Value;
                    }
                    return result;
                }
            }
        }

        #endregion

        #region IDynamicMetaObjectProvider implementation

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new Meta(parameter, this);
        }

        private sealed class Meta : DynamicMetaObject {
            internal Meta(Expression parameter, ScriptScope scope)
                : base(parameter, BindingRestrictions.Empty, scope) {
            }

            // TODO: support for IgnoreCase in underlying ScriptScope APIs
            public override DynamicMetaObject BindGetMember(GetMemberBinder action) {
                var result = Expression.Variable(typeof(object), "result");
                var fallback = action.FallbackGetMember(this);

                return new DynamicMetaObject(
                    Expression.Block(
                        new ParameterExpression[] { result },
                        Expression.Condition(
                            Expression.Call(
                                Expression.Convert(Expression, typeof(ScriptScope)),
                                typeof(ScriptScope).GetMethod("TryGetVariable", new[] { typeof(string), typeof(object).MakeByRefType() }),
                                Expression.Constant(action.Name),
                                result
                            ),
                            result,
                            Expression.Convert(fallback.Expression, typeof(object))
                        )
                    ),
                    BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope)).Merge(fallback.Restrictions)
                );
            }

            // TODO: support for IgnoreCase in underlying ScriptScope APIs
            public override DynamicMetaObject BindSetMember(SetMemberBinder action, DynamicMetaObject value) {
                Expression objValue = Expression.Convert(value.Expression, typeof(object));
                return new DynamicMetaObject(
                    Expression.Block(
                        Expression.Call(
                            Expression.Convert(Expression, typeof(ScriptScope)),
                            typeof(ScriptScope).GetMethod("SetVariable", new[] { typeof(string), typeof(object) }),
                            Expression.Constant(action.Name),
                            objValue
                        ),
                        objValue
                    ),
                    Restrictions.Merge(value.Restrictions).Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope)))
                );
            }

            // TODO: support for IgnoreCase in underlying ScriptScope APIs
            public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder action) {
                var fallback = action.FallbackDeleteMember(this);
                return new DynamicMetaObject(
                    Expression.IfThenElse(
                        Expression.Call(
                            Expression.Convert(Expression, typeof(ScriptScope)),
                            typeof(ScriptScope).GetMethod("RemoveVariable"),
                            Expression.Constant(action.Name)
                        ),
                        Expression.Empty(),
                        fallback.Expression
                    ),
                    Restrictions.Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope))).Merge(fallback.Restrictions)
                );
            }

            // TODO: support for IgnoreCase in underlying ScriptScope APIs
            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder action, DynamicMetaObject[] args) {
                var fallback = action.FallbackInvokeMember(this, args);
                var result = Expression.Variable(typeof(object), "result");

                var fallbackInvoke = action.FallbackInvoke(new DynamicMetaObject(result, BindingRestrictions.Empty), args, null);

                return new DynamicMetaObject(
                    Expression.Block(
                        new ParameterExpression[] { result },
                        Expression.Condition(
                            Expression.Call(
                                Expression.Convert(Expression, typeof(ScriptScope)),
                                typeof(ScriptScope).GetMethod("TryGetVariable", new[] { typeof(string), typeof(object).MakeByRefType() }),
                                Expression.Constant(action.Name),
                                result
                            ),
                            Expression.Convert(fallbackInvoke.Expression, typeof(object)),
                            Expression.Convert(fallback.Expression, typeof(object))
                        )
                    ),
                    BindingRestrictions.Combine(args).Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope))).Merge(fallback.Restrictions)
                );
            }

            public override IEnumerable<string> GetDynamicMemberNames() {
                return ((ScriptScope)Value).GetVariableNames();
            }
        }

        #endregion

#if FEATURE_REMOTING
        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
    }
}
