// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// Defines a method overload abstraction for the purpose of overload resolution. 
    /// It provides the overload resolver the metadata it needs to perform the resolution.
    /// </summary>
    /// <remarks>
    /// WARNING: This is a temporary API that will undergo breaking changes in future versions.
    /// </remarks>
    [DebuggerDisplay("{(object)ReflectionInfo ?? Name}")]
    public abstract class OverloadInfo {
        public abstract string Name { get; }
        public abstract IList<ParameterInfo> Parameters { get; }

        public virtual int ParameterCount => Parameters.Count;

        /// <summary>
        /// Null for constructors.
        /// </summary>
        public abstract ParameterInfo ReturnParameter { get; }

        public virtual bool ProhibitsNull(int parameterIndex) {
            return Parameters[parameterIndex].ProhibitsNull();
        }

        public virtual bool ProhibitsNullItems(int parameterIndex) {
            return Parameters[parameterIndex].ProhibitsNullItems();
        }

        public virtual bool IsParamArray(int parameterIndex) {
            return Parameters[parameterIndex].IsParamArray();
        }

        public virtual bool IsParamDictionary(int parameterIndex) {
            return Parameters[parameterIndex].IsParamDictionary();
        }

        public abstract Type DeclaringType { get; }
        public abstract Type ReturnType { get; }
        public abstract MethodAttributes Attributes { get; }
        public abstract bool IsConstructor { get; }
        public abstract bool IsExtension { get; }

        /// <summary>
        /// The method arity can vary, i.e. the method has params array or params dict parameters.
        /// </summary>
        public abstract bool IsVariadic { get; }
        
        public abstract bool IsGenericMethodDefinition { get; }
        public abstract bool IsGenericMethod { get; }
        public abstract bool ContainsGenericParameters { get; }
        public abstract IList<Type> GenericArguments { get; }
        public abstract OverloadInfo MakeGenericMethod(Type[] genericArguments);

        public virtual CallingConventions CallingConvention => CallingConventions.Standard;

        public virtual MethodBase ReflectionInfo => null;

        // TODO: remove
        public virtual bool IsInstanceFactory => IsConstructor;

        public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;

        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

        public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

        public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;

        public bool IsProtected => IsFamily || IsFamilyOrAssembly;

        public bool IsStatic => IsConstructor || (Attributes & MethodAttributes.Static) != 0;

        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;

        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;

        public bool IsFinal => (Attributes & MethodAttributes.Final) != 0;
    }

    /// <summary>
    /// Represents a method overload that is bound to a <see cref="T:System.Reflection.MethodBase"/>.
    /// </summary>
    /// <remarks>
    /// Not thread safe.
    /// WARNING: This is a temporary API that will undergo breaking changes in future versions. 
    /// </remarks>
    public class ReflectionOverloadInfo : OverloadInfo {
        [Flags]
        private enum _Flags {
            None = 0,
            IsVariadic = 1,
            KnownVariadic = 2,
            ContainsGenericParameters = 4,
            KnownContainsGenericParameters = 8,
            IsExtension = 16,
            KnownExtension = 32,
        }

        private readonly MethodBase _method;
        private ReadOnlyCollection<ParameterInfo> _parameters; // lazy
        private ReadOnlyCollection<Type> _genericArguments; // lazy
        private _Flags _flags; // lazy

        public ReflectionOverloadInfo(MethodBase method) {
            _method = method;
        }

        public override MethodBase ReflectionInfo => _method;

        public override string Name => _method.Name;

        public override IList<ParameterInfo> Parameters => _parameters ?? (_parameters = new ReadOnlyCollection<ParameterInfo>(_method.GetParameters()));

        public override ParameterInfo ReturnParameter {
            get {
                MethodInfo method = _method as MethodInfo;
                return method != null ? method.ReturnParameter : null;
            }
        }
        
        public override IList<Type> GenericArguments => _genericArguments ?? (_genericArguments = new ReadOnlyCollection<Type>(_method.GetGenericArguments()));

        public override Type DeclaringType => _method.DeclaringType;

        public override Type ReturnType => _method.GetReturnType();

        public override CallingConventions CallingConvention => _method.CallingConvention;

        public override MethodAttributes Attributes => _method.Attributes;

        public override bool IsInstanceFactory => CompilerHelpers.IsConstructor(_method);

        public override bool IsConstructor => _method.IsConstructor;

        public override bool IsExtension {
            get {
                if ((_flags & _Flags.KnownExtension) == 0) {
                    _flags |= _Flags.KnownExtension | (_method.IsExtension() ? _Flags.IsExtension : 0);
                }
                return (_flags & _Flags.IsExtension) != 0;
            }
        }

        public override bool IsVariadic {
            get { 
                if ((_flags & _Flags.KnownVariadic) == 0) {
                    _flags |= _Flags.KnownVariadic | (IsVariadicInternal() ? _Flags.IsVariadic : 0);
                }
                return (_flags & _Flags.IsVariadic) != 0;
            }
        }

        private bool IsVariadicInternal() {
            var ps = Parameters;
            for (int i = ps.Count - 1; i >= 0; i--) {
                if (ps[i].IsParamArray() || ps[i].IsParamDictionary()) {
                    return true;
                }
            }
            return false;
        }

        public override bool IsGenericMethod => _method.IsGenericMethod;

        public override bool IsGenericMethodDefinition => _method.IsGenericMethodDefinition;

        public override bool ContainsGenericParameters {
            get { 
                if ((_flags & _Flags.KnownContainsGenericParameters) == 0) {
                    _flags |= _Flags.KnownContainsGenericParameters | (_method.ContainsGenericParameters ? _Flags.ContainsGenericParameters : 0);
                }
                return (_flags & _Flags.ContainsGenericParameters) != 0;
            }
        }

        public override OverloadInfo MakeGenericMethod(Type[] genericArguments) {
            return new ReflectionOverloadInfo(((MethodInfo)_method).MakeGenericMethod(genericArguments));
        }

        public static OverloadInfo[] CreateArray(MethodBase[] methods) {
            return ArrayUtils.ConvertAll(methods, m => new ReflectionOverloadInfo(m));
        }
    }
}
