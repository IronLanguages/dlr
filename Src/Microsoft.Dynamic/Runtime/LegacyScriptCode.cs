﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if OBSOLETE // TODO: FEATURE_FILESYSTEM
using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Scripting.Utils;
using System.Threading;
using Microsoft.Scripting.Generation;
using System.Reflection.Emit;
using System.Reflection;

namespace Microsoft.Scripting.Runtime {
    public class LegacyScriptCode : SavableScriptCode {
        private DlrMainCallTarget _target;
        private LambdaExpression _code;

        public LegacyScriptCode(LambdaExpression code, SourceUnit sourceUnit)
            : this(code, null, sourceUnit) {
        }

        public LegacyScriptCode(LambdaExpression code, DlrMainCallTarget target, SourceUnit sourceUnit) : base(sourceUnit) {
            ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");

            _target = target;
            _code = code;
        }

        public override object Run() {
            return EnsureTarget(_code)(CreateScope(), SourceUnit.LanguageContext);
        }

        public override object Run(Scope scope) {
            return EnsureTarget(_code)(scope, SourceUnit.LanguageContext);            
        }

        public void EnsureCompiled() {
            EnsureTarget(_code);
        }

        protected virtual object InvokeTarget(LambdaExpression code, Scope scope) {
            return EnsureTarget(code)(scope, LanguageContext);
        }

        private DlrMainCallTarget EnsureTarget(LambdaExpression code) {
            if (_target == null) {
                var lambda = code as Expression<DlrMainCallTarget>;
                if (lambda == null) {
                    // If language APIs produced the wrong delegate type,
                    // rewrite the lambda with the correct type
                    lambda = Expression.Lambda<DlrMainCallTarget>(code.Body, code.Name, code.Parameters);
                }
                Interlocked.CompareExchange(ref _target, lambda.Compile(SourceUnit.EmitDebugSymbols), null);
            }
            return _target;
        }

        protected override KeyValuePair<MethodBuilder, Type> CompileForSave(TypeGen typeGen) {
            var lambda = RewriteForSave(typeGen, _code);

            MethodBuilder mb = typeGen.TypeBuilder.DefineMethod(lambda.Name ?? "lambda_method", CompilerHelpers.PublicStatic | MethodAttributes.SpecialName);
            lambda.CompileToMethod(mb, false);
            return new KeyValuePair<MethodBuilder, Type>(mb, typeof(DlrMainCallTarget));
        }

        public static ScriptCode Load(DlrMainCallTarget method, LanguageContext language, string path) {
            SourceUnit su = new SourceUnit(language, NullTextContentProvider.Null, path, SourceCodeKind.File);
            return new LegacyScriptCode(null, method, su);
        }
    }
}
#endif