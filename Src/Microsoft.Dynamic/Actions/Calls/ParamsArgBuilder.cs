// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    using Ast = Expression;

    internal sealed class ParamsArgBuilder : ArgBuilder {
        private readonly int _start;
        private readonly int _expandedCount;
        private readonly Type _elementType;

        internal ParamsArgBuilder(ParameterInfo info, Type elementType, int start, int expandedCount) 
            : base(info) {

            Assert.NotNull(elementType);
            Debug.Assert(start >= 0);
            Debug.Assert(expandedCount >= 0);

            _start = start;
            _expandedCount = expandedCount;
            _elementType = elementType;
        }

        // Consumes all expanded arguments. 
        // Collapsed arguments are fetched from resolver provided storage, not from actual argument expressions.
        public override int ConsumedArgumentCount {
            get { return _expandedCount; }
        }

        public override int Priority {
            get { return 4; }
        }

        internal protected override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            var actualArgs = resolver.GetActualArguments();
            int splatIndex = actualArgs.SplatIndex;
            int collapsedCount = actualArgs.CollapsedCount;
            int firstSplatted = actualArgs.FirstSplattedArg;

            var result = new Expression[2 + _expandedCount + (collapsedCount > 0 ? 2 : 0)];
            var arrayVariable = resolver.GetTemporary(_elementType.MakeArrayType(), "a");
            int e = 0;
            result[e++] = Ast.Assign(arrayVariable, Ast.NewArrayBounds(_elementType, Ast.Constant(_expandedCount + collapsedCount)));

            int itemIndex = 0;
            int i = _start;
            while (true) {
                // inject loop copying collapsed items:
                if (i == splatIndex) {
                    var indexVariable = resolver.GetTemporary(typeof(int), "t");

                    // for (int t = 0; t <= {collapsedCount}; t++) {
                    //   a[{itemIndex} + t] = CONVERT<ElementType>(list.get_Item({splatIndex - firstSplatted} + t))
                    // }
                    result[e++] = Ast.Assign(indexVariable, AstUtils.Constant(0));
                    result[e++] = AstUtils.Loop(
                        Ast.LessThan(indexVariable, Ast.Constant(collapsedCount)),
                        // TODO: not implemented in the old interpreter
                        // Ast.PostIncrementAssign(indexVariable),
                        Ast.Assign(indexVariable, Ast.Add(indexVariable, AstUtils.Constant(1))),
                        Ast.Assign(
                            Ast.ArrayAccess(arrayVariable, Ast.Add(AstUtils.Constant(itemIndex), indexVariable)),
                            resolver.Convert(
                                new DynamicMetaObject(
                                    resolver.GetSplattedItemExpression(Ast.Add(AstUtils.Constant(splatIndex - firstSplatted), indexVariable)), 
                                    BindingRestrictions.Empty
                                ),
                                null,
                                ParameterInfo, 
                                _elementType
                            )
                        ),
                        null
                    );

                    itemIndex += collapsedCount;
                }

                if (i >= _start + _expandedCount) {
                    break;
                }

                Debug.Assert(!hasBeenUsed[i]);
                hasBeenUsed[i] = true;                

                result[e++] = Ast.Assign(
                    Ast.ArrayAccess(arrayVariable, AstUtils.Constant(itemIndex++)),
                    resolver.Convert(args.GetObject(i), args.GetType(i), ParameterInfo, _elementType)
                );

                i++;
            }

            result[e++] = arrayVariable;

            Debug.Assert(e == result.Length);
            return Ast.Block(result);
        }

        public override Type Type {
            get {
                return _elementType.MakeArrayType();
            }
        }

        public override ArgBuilder Clone(ParameterInfo newType) {
            return new ParamsArgBuilder(newType, newType.ParameterType.GetElementType(), _start, _expandedCount);
        }
    }
}
