// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Scripting.Metadata;
using Microsoft.Scripting.Utils;

namespace Metadata {
    public sealed class TypeNestings {
        private readonly MetadataTables _tables;
        private readonly Dictionary<MetadataToken, List<MetadataToken>> _mapping;
        private static readonly TypeDef[] _EmptyTypeDefs = new TypeDef[0];

        public TypeNestings(MetadataTables tables) {
            ContractUtils.Requires(tables != null);
            _tables = tables;
            _mapping = new Dictionary<MetadataToken, List<MetadataToken>>();
            Populate();
        }

        private void Populate() {
            foreach (TypeNesting nesting in _tables.TypeNestings) {
                var enclosing = nesting.EnclosingType.Record.Token;
                if (!_mapping.TryGetValue(enclosing, out List<MetadataToken> nested)) {
                    _mapping.Add(enclosing, nested = new List<MetadataToken>());
                }
                nested.Add(nesting.NestedType.Record.Token);
            }
        }

        public IEnumerable<TypeDef> GetEnclosingTypes() {
            return from enclosing in _mapping.Keys select _tables.GetRecord(enclosing).TypeDef;
        }

        public IEnumerable<TypeDef> GetNestedTypes(TypeDef typeDef) {
            return GetNestedTypes(typeDef, out int count);
        }

        public IEnumerable<TypeDef> GetNestedTypes(TypeDef typeDef, out int count) {
            ContractUtils.Requires(((MetadataRecord)typeDef).Tables.Equals(_tables));

            if (_mapping.TryGetValue(typeDef.Record.Token, out List<MetadataToken> nestedList)) {
                count = nestedList.Count;
                return from nested in nestedList select _tables.GetRecord(nested).TypeDef;
            }

            count = 0;
            return _EmptyTypeDefs;
        }
    }
}
