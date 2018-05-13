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

#if FEATURE_CODEDOM

using System.CodeDom;
using System.Dynamic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
    public abstract class CodeDomCodeGen {
        // This is the key used in the UserData of the CodeDom objects to track
        // the source location of the CodeObject in the original source file.
        protected static readonly object SourceSpanKey = typeof(SourceSpan);

        // Stores the code as it is generated
        private PositionTrackingWriter _writer;
        protected PositionTrackingWriter Writer { get { return _writer; } }

        abstract protected void WriteExpressionStatement(CodeExpressionStatement s);
        abstract protected void WriteFunctionDefinition(CodeMemberMethod func);
        abstract protected string QuoteString(string val);

        public SourceUnit GenerateCode(CodeMemberMethod codeDom, LanguageContext context, string path, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(codeDom, nameof(codeDom));
            ContractUtils.RequiresNotNull(context, nameof(context));
            ContractUtils.Requires(path == null || path.Length > 0, nameof(path));

            // Convert the CodeDom to source code
            _writer?.Close();
            _writer = new PositionTrackingWriter();

            WriteFunctionDefinition(codeDom);

            return CreateSourceUnit(context, path, kind);
        }

        private SourceUnit CreateSourceUnit(LanguageContext context, string path, SourceCodeKind kind) {
            string code = _writer.ToString();
            SourceUnit src = context.CreateSnippet(code, path, kind);
            src.SetLineMapping(_writer.GetLineMap());
            return src;
        }

        protected virtual void WriteArgumentReferenceExpression(CodeArgumentReferenceExpression e) {
            _writer.Write(e.ParameterName);
        }

        protected virtual void WriteSnippetExpression(CodeSnippetExpression e) {
            _writer.Write(e.Value);
        }

        protected virtual void WriteSnippetStatement(CodeSnippetStatement s) {
            _writer.Write(s.Value);
            _writer.Write('\n');
        }

        protected void WriteStatement(CodeStatement s) {
            // Save statement source location
            if (s.LinePragma != null) {
                _writer.MapLocation(s.LinePragma);
            }

            switch (s) {
                case CodeExpressionStatement statement:
                    WriteExpressionStatement(statement);
                    break;
                case CodeSnippetStatement statement:
                    WriteSnippetStatement(statement);
                    break;
            }
        }

        protected void WriteExpression(CodeExpression e) {
            switch (e) {
                case CodeSnippetExpression expression:
                    WriteSnippetExpression(expression);
                    break;
                case CodePrimitiveExpression expression:
                    WritePrimitiveExpression(expression);
                    break;
                case CodeMethodInvokeExpression expression:
                    WriteCallExpression(expression);
                    break;
                case CodeArgumentReferenceExpression expression:
                    WriteArgumentReferenceExpression(expression);
                    break;
            }
        }

        protected void WritePrimitiveExpression(CodePrimitiveExpression e) {
            object val = e.Value;

            if (val is string strVal) {
                _writer.Write(QuoteString(strVal));
            } else {
                _writer.Write(val);
            }
        }

        protected void WriteCallExpression(CodeMethodInvokeExpression m) {
            if (m.Method.TargetObject != null) {
                WriteExpression(m.Method.TargetObject);
                _writer.Write(".");
            }

            _writer.Write(m.Method.MethodName);
            _writer.Write("(");
            for (int i = 0; i < m.Parameters.Count; ++i) {
                if (i != 0) {
                    _writer.Write(",");
                }
                WriteExpression(m.Parameters[i]);
            }
            _writer.Write(")");
        }
    }
}

#endif
