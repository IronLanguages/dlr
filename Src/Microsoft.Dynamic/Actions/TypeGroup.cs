// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// A TypeCollision is used when we have a collision between
    /// two types with the same name.  Currently this is only possible w/ generic
    /// methods that should logically have arity as a portion of their name. For eg:
    ///      System.EventHandler and System.EventHandler[T]
    ///      System.Nullable and System.Nullable[T]
    ///      System.IComparable and System.IComparable[T]
    /// 
    /// The TypeCollision provides an indexer but also is a real type.  When used
    /// as a real type it is the non-generic form of the type.
    /// 
    /// The indexer allows the user to disambiguate between the generic and
    /// non-generic versions.  Therefore users must always provide additional
    /// information to get the generic version.
    /// </summary>
    public sealed class TypeGroup : TypeTracker {
        private readonly Dictionary<int, Type> _typesByArity;
        private readonly string _name;

        private TypeGroup(Type t1, int arity1, Type t2, int arity2) {
            // TODO: types of different arities might be inherited, but we don't support that yet:
            Debug.Assert(t1.DeclaringType == t2.DeclaringType);

            Debug.Assert(arity1 != arity2);
            _typesByArity = new Dictionary<int, Type>();
            _typesByArity[arity1] = t1;
            _typesByArity[arity2] = t2;

            _name = ReflectionUtils.GetNormalizedTypeName(t1);
            Debug.Assert(_name == ReflectionUtils.GetNormalizedTypeName(t2));
        }

        private TypeGroup(Type t1, TypeGroup existingTypes) {
            // TODO: types of different arities might be inherited, but we don't support that yet:
            Debug.Assert(t1.DeclaringType == existingTypes.DeclaringType);
            Debug.Assert(ReflectionUtils.GetNormalizedTypeName(t1) == existingTypes.Name);
            
            _typesByArity = new Dictionary<int, Type>(existingTypes._typesByArity);
            _typesByArity[GetGenericArity(t1)] = t1;
            _name = existingTypes.Name;
        }

        public override string ToString() {
            StringBuilder repr = new StringBuilder(base.ToString());
            repr.Append(":" + Name + "(");

            bool pastFirstType = false;
            foreach (Type type in Types) {
                if (pastFirstType) {
                    repr.Append(", ");
                }
                repr.Append(type.Name);
                pastFirstType = true;
            }
            repr.Append(")");

            return repr.ToString();
        }

        public override IList<string> GetMemberNames() {
            HashSet<string> members = new HashSet<string>();
            foreach (Type type in Types) {
                GetMemberNames(type, members);
            }

            return members.ToArray();
        }

        public TypeTracker GetTypeForArity(int arity) {
            if (!_typesByArity.TryGetValue(arity, out Type typeWithMatchingArity)) {
                return null;
            }
            return TypeTracker.GetTypeTracker(typeWithMatchingArity);
        }

        /// <param name="existingTypeEntity">The merged list so far. Could be null</param>
        /// <param name="newType">The new type(s) to add to the merged list</param>
        /// <returns>The merged list.  Could be a TypeTracker or TypeGroup</returns>
        public static TypeTracker UpdateTypeEntity(TypeTracker existingTypeEntity, TypeTracker newType) {
            Debug.Assert(newType != null);
            Debug.Assert(existingTypeEntity == null || (existingTypeEntity is NestedTypeTracker) || (existingTypeEntity is TypeGroup));

            if (existingTypeEntity == null) {
                return newType;
            }

            NestedTypeTracker existingType = existingTypeEntity as NestedTypeTracker;
            TypeGroup existingTypeCollision = existingTypeEntity as TypeGroup;

            if (existingType != null) {
                int existingArity = GetGenericArity(existingType.Type);
                int newArity = GetGenericArity(newType.Type);

                if (existingArity == newArity) {
                    return newType;
                }

                return new TypeGroup(existingType.Type, existingArity, newType.Type, newArity);
            }

            return new TypeGroup(newType.Type, existingTypeCollision);
        }

        /// <summary> Gets the arity of generic parameters</summary>
        private static int GetGenericArity(Type type) {
            if (!type.IsGenericType()) {
                return 0;
            }

            Debug.Assert(type.IsGenericTypeDefinition());
            return type.GetGenericArguments().Length;
        }

        /// <exception cref="TypeLoadException">No non-generic type is represented by this group.</exception>
        public Type GetNonGenericType() {
            if (TryGetNonGenericType(out Type nonGenericType)) {
                return nonGenericType;
            }

            throw Error.NonGenericWithGenericGroup(Name);
        }

        public bool TryGetNonGenericType(out Type nonGenericType) {
            return _typesByArity.TryGetValue(0, out nonGenericType);
        }

        private Type SampleType {
            get {
                using (IEnumerator<Type> e = Types.GetEnumerator()) {
                    e.MoveNext();
                    return e.Current;
                }
            }
        }

        public IEnumerable<Type> Types => _typesByArity.Values;

        public IDictionary<int, Type> TypesByArity => new ReadOnlyDictionary<int, Type>(_typesByArity);


        #region MemberTracker overrides

        public override TrackerTypes MemberType => TrackerTypes.TypeGroup;

        /// <summary>
        /// This returns the DeclaringType of all the types in the TypeGroup
        /// </summary>
        public override Type DeclaringType => SampleType.DeclaringType;

        /// <summary>
        /// This returns the base name of the TypeGroup (the name shared by all types minus arity)
        /// </summary>
        public override string Name => _name;

        /// <summary>
        /// This will return the result only for the non-generic type if one exists, and will throw 
        /// an exception if all types in the TypeGroup are generic
        /// </summary>
        public override Type Type => GetNonGenericType();

        public override bool IsGenericType => _typesByArity.Count > 0;

        /// <summary>
        /// This will return the result only for the non-generic type if one exists, and will throw 
        /// an exception if all types in the TypeGroup are generic
        /// </summary>
        public override bool IsPublic => GetNonGenericType().IsPublic();

        #endregion
    }
}
