// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    public sealed class RestrictedArguments {
        private readonly DynamicMetaObject[] _objects;
        private readonly Type[] _types;

        public RestrictedArguments(DynamicMetaObject[] objects, Type[] types, bool hasUntypedRestrictions) {
            Assert.NotNullItems(objects);
            Assert.NotNull(types);
            Debug.Assert(objects.Length == types.Length);

            _objects = objects;
            _types = types;
            HasUntypedRestrictions = hasUntypedRestrictions;
        }

        public int Length => _objects.Length;

        public DynamicMetaObject GetObject(int i) {
            return _objects[i];
        }

        public Type GetType(int i) {
            return _types[i];
        }

        /// <summary>
        /// True if there are restrictions beyond just simple type restrictions
        /// </summary>
        public bool HasUntypedRestrictions { get; }

        public BindingRestrictions GetAllRestrictions() {
            return BindingRestrictions.Combine(_objects);
        }

        public IList<DynamicMetaObject> GetObjects() {
            return new ReadOnlyCollection<DynamicMetaObject>(_objects);
        }

        public IList<Type> GetTypes() {
            return new ReadOnlyCollection<Type>(_types);
        }
    }
}
