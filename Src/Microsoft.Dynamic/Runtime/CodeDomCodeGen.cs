// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_CODEDOM

using System.CodeDom;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
    public abstract class CodeDomCodeGen {
        // This is the key used in the UserData of the CodeDom objects to track
        // the source location of the CodeObject in the original source file.
        protected static readonly object SourceSpanKey = typeof(SourceSpan);

        // Stores the code as it is generated
        protected PositionTrackingWriter Writer { get; private set; }

        protected abstract void WriteExpressionStatement(CodeExpressionStatement s);
        protected abstract void WriteFunctionDefinition(CodeMemberMethod func);
        protected abstract string QuoteString(string val);

        public SourceUnit GenerateCode(CodeMemberMethod codeDom, LanguageContext context, string path, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(codeDom, nameof(codeDom));
            ContractUtils.RequiresNotNull(context, nameof(context));
            ContractUtils.Requires(path == null || path.Length > 0, nameof(path));

            // Convert the CodeDom to source code
            Writer?.Close();
            Writer = new PositionTrackingWriter();

            WriteFunctionDefinition(codeDom);

            return CreateSourceUnit(context, path, kind);
        }

        private SourceUnit CreateSourceUnit(LanguageContext context, string path, SourceCodeKind kind) {
            string code = Writer.ToString();
            SourceUnit src = context.CreateSnippet(code, path, kind);
            src.SetLineMapping(Writer.GetLineMap());
            return src;
        }

        protected virtual void WriteArgumentReferenceExpression(CodeArgumentReferenceExpression e) {
            Writer.Write(e.ParameterName);
        }

        protected virtual void WriteSnippetExpression(CodeSnippetExpression e) {
            Writer.Write(e.Value);
        }

        protected virtual void WriteSnippetStatement(CodeSnippetStatement s) {
            Writer.Write(s.Value);
            Writer.Write('\n');
        }

        protected void WriteStatement(CodeStatement s) {
            // Save statement source location
            if (s.LinePragma != null) {
                Writer.MapLocation(s.LinePragma);
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
                Writer.Write(QuoteString(strVal));
            } else {
                Writer.Write(val);
            }
        }

        protected void WriteCallExpression(CodeMethodInvokeExpression m) {
            if (m.Method.TargetObject != null) {
                WriteExpression(m.Method.TargetObject);
                Writer.Write(".");
            }

            Writer.Write(m.Method.MethodName);
            Writer.Write("(");
            for (int i = 0; i < m.Parameters.Count; ++i) {
                if (i != 0) {
                    Writer.Write(",");
                }
                WriteExpression(m.Parameters[i]);
            }
            Writer.Write(")");
        }
    }
}

#endif
