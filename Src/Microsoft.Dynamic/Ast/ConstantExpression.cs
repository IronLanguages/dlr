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

using Complex = System.Numerics.Complex;

using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Math;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public static partial class Utils {
        private static readonly ConstantExpression TrueLiteral = Expression.Constant(true, typeof(bool));
        private static readonly ConstantExpression FalseLiteral = Expression.Constant(false, typeof(bool));
        private static readonly ConstantExpression NullLiteral = Expression.Constant(null, typeof(object));
        private static readonly ConstantExpression EmptyStringLiteral = Expression.Constant(String.Empty, typeof(string));
        private static readonly ConstantExpression[] IntCache = new ConstantExpression[100];

        /// <summary>
        /// Wraps the given value in a WeakReference and returns a tree that will retrieve
        /// the value from the WeakReference.
        /// </summary>
        public static MemberExpression WeakConstant(object value) {
            System.Diagnostics.Debug.Assert(!(value is Expression));
            return Expression.Property(
                Constant(new WeakReference(value)),
                typeof(WeakReference).GetDeclaredProperty("Target")
            );
        }

        public static ConstantExpression Constant(object value, Type type) {
            return Expression.Constant(value, type);
        }

        // The helper API should return ConstantExpression after SymbolConstantExpression goes away
        public static Expression Constant(object value) {
            switch (value) {
                case null:
                    return NullLiteral;
                case BigInteger bigInteger:
                    return BigIntegerConstant(bigInteger);
                case Complex complex:
                    return ComplexConstant(complex);
                case Complex64 complex64:
                    return Complex64Constant(complex64);
                case Type type:
                    return Expression.Constant(value, typeof(Type));
                case ConstructorInfo constructorInfo:
                    return Expression.Constant(value, typeof(ConstructorInfo));
                case EventInfo eventInfo:
                    return Expression.Constant(value, typeof(EventInfo));
                case FieldInfo fieldInfo:
                    return Expression.Constant(value, typeof(FieldInfo));
                case MethodInfo methodInfo:
                    return Expression.Constant(value, typeof(MethodInfo));
                case PropertyInfo propertyInfo:
                    return Expression.Constant(value, typeof(PropertyInfo));
                default: {
                    Type t = value.GetType();
                    if (!t.IsEnum) {
                        switch (t.GetTypeCode()) {
                            case TypeCode.Boolean:
                                return (bool)value ? TrueLiteral : FalseLiteral;
                            case TypeCode.Int32:
                                int x = (int)value;
                                int cacheIndex = x + 2;
                                if (cacheIndex >= 0 && cacheIndex < IntCache.Length) {
                                    ConstantExpression res;
                                    if ((res = IntCache[cacheIndex]) == null) {
                                        IntCache[cacheIndex] = res = Constant(x, typeof(int));
                                    }
                                    return res;
                                }
                                break;
                            case TypeCode.String:
                                if (String.IsNullOrEmpty((string)value)) {
                                    return EmptyStringLiteral;
                                }
                                break;
                        }
                    }
                    return Expression.Constant(value);
                }
            }
        }

        private static Expression BigIntegerConstant(BigInteger value) {
            int ival;
            if (value.AsInt32(out ival)) {
                return Expression.Call(
                    new Func<int, BigInteger>(CompilerHelpers.CreateBigInt).GetMethodInfo(),
                    Constant(ival)
                );
            }

            long lval;
            if (value.AsInt64(out lval)) {
                return Expression.Call(
                    new Func<long, BigInteger>(CompilerHelpers.CreateBigInt).GetMethodInfo(),
                    Constant(lval)
                );
            }

            return Expression.Call(
                new Func<bool, byte[], BigInteger>(CompilerHelpers.CreateBigInt).GetMethodInfo(),
                Constant(value.Sign < 0),
                CreateArray<byte>(value.Abs().ToByteArray())
            );
        }

        private static Expression CreateArray<T>(T[] array) {
            // TODO: could we use blobs?
            Expression[] init = new Expression[array.Length];
            for (int i = 0; i < init.Length; i++) {
                init[i] = Constant(array[i]);
            }
            return Expression.NewArrayInit(typeof(T), init);
        }

        private static Expression ComplexConstant(Complex value) {
            if (value.Real != 0.0) {
                if (value.Imaginary() != 0.0) {
                    return Expression.Call(
                        new Func<double, double, Complex>(MathUtils.MakeComplex).GetMethodInfo(),
                        Constant(value.Real),
                        Constant(value.Imaginary())
                    );
                } else {
                    return Expression.Call(
                        new Func<double, Complex>(MathUtils.MakeReal).GetMethodInfo(),
                        Constant(value.Real)
                    );
                }
            } else {
                return Expression.Call(
                    new Func<double, Complex>(MathUtils.MakeImaginary).GetMethodInfo(),
                    Constant(value.Imaginary())
                );
            }
        }

        private static Expression Complex64Constant(Complex64 value) {
            if (value.Real != 0.0) {
                if (value.Imag != 0.0) {
                    return Expression.Call(
                        new Func<double, double, Complex64>(Complex64.Make).GetMethodInfo(),
                        Constant(value.Real),
                        Constant(value.Imag)
                    );
                } else {
                    return Expression.Call(
                        new Func<double, Complex64>(Complex64.MakeReal).GetMethodInfo(),
                        Constant(value.Real)
                    );
                }
            } else {
                return Expression.Call(
                    new Func<double, Complex64>(Complex64.MakeImaginary).GetMethodInfo(),
                    Constant(value.Imag)
                );
            }
        }
    }
}
