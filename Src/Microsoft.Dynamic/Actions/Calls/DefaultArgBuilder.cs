// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls {

    /// <summary>
    /// ArgBuilder which provides a default parameter value for a method call.
    /// </summary>
    internal sealed class DefaultArgBuilder : ArgBuilder {
        public DefaultArgBuilder(ParameterInfo info) 
            : base(info) {
            Assert.NotNull(info);
        }

        public override int Priority {
            get { return 2; }
        }

        public override int ConsumedArgumentCount {
            get { return 0; }
        }

        internal protected override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            object value = ParameterInfo.GetDefaultValue();
            if (value is Missing) {
                value = CompilerHelpers.GetMissingValue(ParameterInfo.ParameterType);
            }

            if (ParameterInfo.ParameterType.IsByRef) {
                return AstUtils.Constant(value, ParameterInfo.ParameterType.GetElementType());
            }

            var metaValue = new DynamicMetaObject(AstUtils.Constant(value), BindingRestrictions.Empty, value);
            return resolver.Convert(metaValue, CompilerHelpers.GetType(value), ParameterInfo, ParameterInfo.ParameterType);
        }
    }
}
