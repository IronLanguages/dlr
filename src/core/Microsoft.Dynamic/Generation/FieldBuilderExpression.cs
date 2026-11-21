// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

#if FEATURE_REFEMIT

using System.Linq.Expressions;

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

#if FEATURE_REFEMIT_FULL
        public FieldBuilderExpression(FieldBuilder builder) {
            _builder = builder;
        }
#else
        private readonly StrongBox<Type> _finishedType;

        // Get something which can be updated w/ the final type.
        public FieldBuilderExpression(FieldBuilder builder, StrongBox<Type> finishedType) {
            _builder = builder;
            _finishedType = finishedType;
        }
#endif

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
#if FEATURE_REFEMIT_FULL
            return _builder.DeclaringType.Module.ResolveField(
                _builder.GetToken().Token
            );
#else
            Type finishedType = _finishedType.Value ?? throw new InvalidOperationException("The type declaring the field has not been finished.");
            return finishedType.GetDeclaredField(_builder.Name) ?? throw new InvalidOperationException($"Field {_builder.Name} not found in {finishedType}.");
#endif
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            return this;
        }
    }
}

#endif
