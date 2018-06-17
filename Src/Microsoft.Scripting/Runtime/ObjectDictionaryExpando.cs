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

using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Wraps a an IDictionary[object, object] and exposes it as an IDynamicMetaObjectProvider so that
    /// users can access string attributes using member accesses.
    /// </summary>
    public sealed class ObjectDictionaryExpando : IDynamicMetaObjectProvider {
        private readonly IDictionary<object, object> _data;

        public ObjectDictionaryExpando(IDictionary<object, object> dictionary) {
            _data = dictionary;
        }

        public IDictionary<object, object> Dictionary => _data;

        private static object TryGetMember(object adapter, string name) {
            if (((ObjectDictionaryExpando)adapter)._data.TryGetValue(name, out object result)) {
                return result;
            }
            return StringDictionaryExpando._getFailed;
        }

        private static void TrySetMember(object adapter, string name, object value) {
            ((ObjectDictionaryExpando)adapter)._data[name] = value;
        }

        private static bool TryDeleteMember(object adapter, string name) {
            return ((ObjectDictionaryExpando)adapter)._data.Remove(name);
        }

        #region IDynamicMetaObjectProvider Members

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new DictionaryExpandoMetaObject(parameter, this, _data.Keys, TryGetMember, TrySetMember, TryDeleteMember);
        }

        #endregion
    }
}
