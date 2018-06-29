// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    [Flags]
    public enum ParameterBindingFlags {
        None = 0,
        ProhibitNull = 1,
        ProhibitNullItems = 2,
        IsParamArray = 4,
        IsParamDictionary = 8,
        IsHidden = 16,
    }

    /// <summary>
    /// ParameterWrapper represents the logical view of a parameter. For eg. the byref-reduced signature
    /// of a method with byref parameters will be represented using a ParameterWrapper of the underlying
    /// element type, since the logical view of the byref-reduced signature is that the argument will be
    /// passed by value (and the updated value is included in the return value).
    /// 
    /// Contrast this with ArgBuilder which represents the real physical argument passed to the method.
    /// </summary>
    public sealed class ParameterWrapper {
        private readonly ParameterBindingFlags _flags;

        // Type and other properties may differ from the values on the info; info could also be unspecified.

        /// <summary>
        /// ParameterInfo is not available.
        /// </summary>
        [Obsolete("Use ParameterBindingAttributes overload")]
        public ParameterWrapper(Type type, string name, bool prohibitNull)
            : this(null, type, name, prohibitNull, false, false, false) {
        }

        [Obsolete("Use ParameterBindingAttributes overload")]
        public ParameterWrapper(ParameterInfo info, Type type, string name, bool prohibitNull, bool isParams, bool isParamsDict, bool isHidden) 
            : this(info, type, name, 
            (prohibitNull ? ParameterBindingFlags.ProhibitNull : 0) |
            (isParams ? ParameterBindingFlags.IsParamArray : 0) |
            (isParamsDict ? ParameterBindingFlags.IsParamDictionary : 0) |
            (isHidden ? ParameterBindingFlags.IsHidden : 0)) {
        }

        public ParameterWrapper(ParameterInfo info, Type type, string name, ParameterBindingFlags flags) {
            ContractUtils.RequiresNotNull(type, nameof(type));
            
            Type = type;
            ParameterInfo = info;
            _flags = flags;

            // params arrays & dictionaries don't allow assignment by keyword
            Name = (IsParamsArray || IsParamsDict || name == null) ? "<unknown>" : name;
        }

        public ParameterWrapper Clone(string name) {
            return new ParameterWrapper(ParameterInfo, Type, name, _flags);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public Type Type { get; }

        public ParameterInfo ParameterInfo { get; }

        public string Name { get; }

        public ParameterBindingFlags Flags => _flags;

        public bool ProhibitNull => (_flags & ParameterBindingFlags.ProhibitNull) != 0;

        public bool ProhibitNullItems => (_flags & ParameterBindingFlags.ProhibitNullItems) != 0;

        public bool IsHidden => (_flags & ParameterBindingFlags.IsHidden) != 0;

        public bool IsByRef => ParameterInfo != null && ParameterInfo.ParameterType.IsByRef;

        /// <summary>
        /// True if the wrapper represents a params-array parameter (false for parameters created by expansion of a params-array).
        /// </summary>
        // TODO: rename to IsParamArray
        public bool IsParamsArray => (_flags & ParameterBindingFlags.IsParamArray) != 0;

        /// <summary>
        /// True if the wrapper represents a params-dict parameter (false for parameters created by expansion of a params-dict).
        /// </summary>
        // TODO: rename to IsParamDictionary
        public bool IsParamsDict => (_flags & ParameterBindingFlags.IsParamDictionary) != 0;

        internal static int IndexOfParamsArray(IList<ParameterWrapper> parameters) {
            for (int i = 0; i < parameters.Count; i++) {
                if (parameters[i].IsParamsArray) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Creates a parameter that represents an expanded item of params-array.
        /// </summary>
        internal ParameterWrapper Expand() {
            Debug.Assert(IsParamsArray);
            return new ParameterWrapper(ParameterInfo, Type.GetElementType(), null, 
                (ProhibitNullItems ? ParameterBindingFlags.ProhibitNull : 0) | 
                (IsHidden ? ParameterBindingFlags.IsHidden : 0)
            );
        }
    }
}
