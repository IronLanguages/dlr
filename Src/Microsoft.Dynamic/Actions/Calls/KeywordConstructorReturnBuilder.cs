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

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    using Ast = Expression;    

    /// <summary>
    /// Updates fields/properties of the returned value with unused keyword parameters.
    /// </summary>
    internal sealed class KeywordConstructorReturnBuilder : ReturnBuilder {
        private readonly ReturnBuilder _builder;
        private readonly int _kwArgCount;
        private readonly int[] _indexesUsed;
        private readonly MemberInfo[] _membersSet;
        private readonly bool _privateBinding;

        public KeywordConstructorReturnBuilder(ReturnBuilder builder, int kwArgCount, int[] indexesUsed, MemberInfo[] membersSet,
            bool privateBinding)
            : base(builder.ReturnType) {
            _builder = builder;
            _kwArgCount = kwArgCount;
            _indexesUsed = indexesUsed;
            _membersSet = membersSet;
            _privateBinding = privateBinding;
        }

        internal override Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret) {
            List<Expression> sets = new List<Expression>();

            ParameterExpression tmp = resolver.GetTemporary(ret.Type, "val");
            sets.Add(
                Ast.Assign(tmp, ret)
            );

            for (int i = 0; i < _indexesUsed.Length; i++) {
                Expression value = args.GetObject(args.Length - _kwArgCount + _indexesUsed[i]).Expression;
                
                PropertyInfo pi;
                FieldInfo fi;
                if ((fi = _membersSet[i] as FieldInfo) != null) {
                    if (!fi.IsLiteral && !fi.IsInitOnly) {
                        sets.Add(
                            Ast.Assign(
                                Ast.Field(tmp, fi),
                                ConvertToHelper(resolver, value, fi.FieldType)
                            )
                        );
                    } else {
                        // call a helper which throws the error but "returns object"
                        sets.Add(
                            Ast.Convert(
                                Ast.Call(
                                    typeof(ScriptingRuntimeHelpers).GetMethod("ReadOnlyAssignError"),
                                    AstUtils.Constant(true),
                                    AstUtils.Constant(fi.Name)
                                ),
                                fi.FieldType
                            )
                        );
                    }                        
                } else if ((pi = _membersSet[i] as PropertyInfo) != null) {
                    if (pi.GetSetMethod(_privateBinding) != null) {
                            sets.Add(
                                Ast.Assign(
                                    Ast.Property(tmp, pi),
                                    ConvertToHelper(resolver, value, pi.PropertyType)
                                )
                            );
                        } else {
                            // call a helper which throws the error but "returns object"
                            sets.Add(
                                Ast.Convert(
                                    Ast.Call(
                                        typeof(ScriptingRuntimeHelpers).GetMethod("ReadOnlyAssignError"),
                                        AstUtils.Constant(false),
                                        AstUtils.Constant(pi.Name)
                                    ),
                                    pi.PropertyType
                                )
                            );
                        }
                }
            }

            sets.Add(
                tmp
            );

            Expression newCall = Ast.Block(
                sets.ToArray()
            );

            return _builder.ToExpression(resolver, builders, args, newCall);
        }

        // TODO: revisit
        private static Expression ConvertToHelper(OverloadResolver resolver, Expression value, Type type) {
            if (type == value.Type) {
                return value;
            }

            if (type.IsAssignableFrom(value.Type)) {
                return AstUtils.Convert(value, type);
            }

            return resolver.GetDynamicConversion(value, type);
        }
    }
}
