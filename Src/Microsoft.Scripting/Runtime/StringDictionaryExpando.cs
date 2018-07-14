// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Exposes a IDictionary[string, object] as a dynamic object.  Gets/sets/deletes turn
    /// into accesses on the underlying dictionary.
    /// </summary>
    public sealed class StringDictionaryExpando : IDynamicMetaObjectProvider {
        private readonly IDictionary<string, object> _data;
        internal static readonly object _getFailed = new object();

        public StringDictionaryExpando(IDictionary<string, object> data) {
            _data = data;
        }

        public IDictionary<string, object> Dictionary => _data;

        private static object TryGetMember(object adapter, string name) {
            if (((StringDictionaryExpando)adapter)._data.TryGetValue(name, out object result)) {
                return result;
            }
            return _getFailed;
        }

        private static void TrySetMember(object adapter, string name, object value) {
            ((StringDictionaryExpando)adapter)._data[name] = value;
        }

        private static bool TryDeleteMember(object adapter, string name) {
            return ((StringDictionaryExpando)adapter)._data.Remove(name);
        }

        #region IDynamicMetaObjectProvider Members

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new DictionaryExpandoMetaObject(parameter, this, _data.Keys, TryGetMember, TrySetMember, TryDeleteMember);
        }

        #endregion
    }


    internal sealed class DictionaryExpandoMetaObject : DynamicMetaObject {
        private readonly Func<object, string, object> _getMember;
        private readonly Action<object, string, object> _setMember;
        private readonly Func<object, string, bool> _deleteMember;
        private readonly IEnumerable _keys;

        public DictionaryExpandoMetaObject(Expression parameter, object storage, IEnumerable keys, Func<object, string, object> getMember, Action<object, string, object> setMember, Func<object, string, bool> deleteMember)
            : base(parameter, BindingRestrictions.Empty, storage) {
            _getMember = getMember;
            _setMember = setMember;
            _deleteMember = deleteMember;
            _keys = keys;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {
            return DynamicTryGetMember(binder.Name,
                binder.FallbackGetMember(this).Expression,
                tmp => tmp
            );
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) {
            return DynamicTryGetMember(binder.Name,
                binder.FallbackInvokeMember(this, args).Expression,
                tmp => binder.FallbackInvoke(new DynamicMetaObject(tmp, BindingRestrictions.Empty), args, null).Expression
            );
        }

        private DynamicMetaObject DynamicTryGetMember(string name, Expression fallback, Func<Expression, Expression> resultOp) {
            var tmp = Expression.Parameter(typeof(object));
            return new DynamicMetaObject(
                Expression.Block(
                    new[] { tmp },
                    Expression.Condition(
                        Expression.NotEqual(
                            Expression.Assign(
                                tmp,
                                Expression.Invoke(
                                    Expression.Constant(_getMember),
                                    Expression,
                                    Expression.Constant(name)
                                )
                            ),
                            Expression.Constant(StringDictionaryExpando._getFailed)
                        ),
                        ExpressionUtils.Convert(resultOp(tmp), typeof(object)),
                        ExpressionUtils.Convert(fallback, typeof(object))
                    )
                ),
                GetRestrictions()
            );
        }

        private BindingRestrictions GetRestrictions() {
            return BindingRestrictions.GetTypeRestriction(Expression, Value.GetType());
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) {
            return new DynamicMetaObject(
                Expression.Block(
                    Expression.Invoke(
                        Expression.Constant(_setMember),
                        Expression,
                        Expression.Constant(binder.Name),
                        Expression.Convert(
                            value.Expression,
                            typeof(object)
                        )
                    ),
                    value.Expression
                ),
                GetRestrictions()
            );
        }

        public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder) {
            return new DynamicMetaObject(
                Expression.Condition(
                    Expression.Invoke(
                        Expression.Constant(_deleteMember),
                        Expression,
                        Expression.Constant(binder.Name)
                    ),
                    Expression.Default(binder.ReturnType),
                    binder.FallbackDeleteMember(this).Expression
                ),
                GetRestrictions()
            );
        }

        public override IEnumerable<string> GetDynamicMemberNames() {
            foreach (object o in _keys) {
                if (o is string s) {
                    yield return s;
                }
            }
        }
    }
}
