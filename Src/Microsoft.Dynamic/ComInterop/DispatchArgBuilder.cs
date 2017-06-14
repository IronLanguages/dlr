﻿/* ****************************************************************************
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

#if FEATURE_COM
#if FEATURE_CORE_DLR
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.Scripting.ComInterop {

    internal class DispatchArgBuilder : SimpleArgBuilder {
        private readonly bool _isWrapper;

        internal DispatchArgBuilder(Type parameterType)
            : base(parameterType) {

            _isWrapper = parameterType == typeof(DispatchWrapper);
        }

        internal override Expression Marshal(Expression parameter) {
            parameter = base.Marshal(parameter);

            // parameter.WrappedObject
            if (_isWrapper) {
                parameter = Expression.Property(
                    Helpers.Convert(parameter, typeof(DispatchWrapper)),
                    typeof(DispatchWrapper).GetProperty("WrappedObject")
                );
            }

            return Helpers.Convert(parameter, typeof(object));
        }

        internal override Expression MarshalToRef(Expression parameter) {
            parameter = Marshal(parameter);

            // parameter == null ? IntPtr.Zero : Marshal.GetIDispatchForObject(parameter);
            return Expression.Condition(
                Expression.Equal(parameter, Expression.Constant(null)),
                Expression.Constant(IntPtr.Zero),
                Expression.Call(
                    typeof(Marshal).GetMethod("GetIDispatchForObject"),
                    parameter
                )
            );
        }

        internal override Expression UnmarshalFromRef(Expression value) {
            // value == IntPtr.Zero ? null : Marshal.GetObjectForIUnknown(value);
            Expression unmarshal = Expression.Condition(
                Expression.Equal(value, Expression.Constant(IntPtr.Zero)),
                Expression.Constant(null),
                Expression.Call(
                    typeof(Marshal).GetMethod("GetObjectForIUnknown"),
                    value
                )
            );

            if (_isWrapper) {
                unmarshal = Expression.New(
                    typeof(DispatchWrapper).GetConstructor(new Type[] { typeof(object) }),
                    unmarshal
                );
            }

            return base.UnmarshalFromRef(unmarshal);

        }
    }
}

#endif
