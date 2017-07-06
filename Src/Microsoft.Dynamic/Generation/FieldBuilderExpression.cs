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
#if FEATURE_REFEMIT

#if FEATURE_CORE_DLR
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation {
    /// <summary>
    /// Provides a simple expression which enables embedding FieldBuilder's
    /// in an AST before the type is complete.
    /// </summary>
    public class FieldBuilderExpression : Expression {
        private readonly FieldBuilder _builder;

        public FieldBuilderExpression(FieldBuilder builder) {
            _builder = builder;
        }

        public override bool CanReduce {
            get {
                return true;
            }
        }

        public sealed override ExpressionType NodeType {
            get { return ExpressionType.Extension; }
        }

        public sealed override Type Type {
            get { return _builder.FieldType; }
        }

        public override Expression Reduce() {
            FieldInfo fi = GetFieldInfo();
            Debug.Assert(fi.Name == _builder.Name);
            return Field(
                null,
                fi
            );
        }

        private FieldInfo GetFieldInfo() {
            // turn the field builder back into a FieldInfo
            return _builder.DeclaringType.Module.ResolveField(
                _builder.GetToken().Token
            );
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            return this;
        }
    }
}

#endif