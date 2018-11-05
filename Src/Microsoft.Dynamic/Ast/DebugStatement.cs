// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System.Diagnostics;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast {
    public partial class Utils {
        public static Expression DebugMarker(string marker) {
            ContractUtils.RequiresNotNull(marker, nameof(marker));
#if DEBUG
            return CallDebugWriteLine(marker);
#else
            return Utils.Empty();
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "marker")]
        public static Expression DebugMark(Expression expression, string marker) {
            ContractUtils.RequiresNotNull(expression, nameof(expression));
            ContractUtils.RequiresNotNull(marker, nameof(marker));

#if DEBUG
            return Expression.Block(
                CallDebugWriteLine(marker),
                expression
            );
#else
            return expression;
#endif
        }

#if DEBUG
        private static MethodCallExpression CallDebugWriteLine(string marker) {
            return Expression.Call(
                typeof(Debug).GetMethod("WriteLine", new[] { typeof(string) }),
                Constant(marker)
            );
        }
#endif

        public static Expression AddDebugInfo(Expression expression, SymbolDocumentInfo document, SourceLocation start, SourceLocation end) {
            if (document == null || !start.IsValid || !end.IsValid) {
                return expression;
            }
            return AddDebugInfo(expression, document, start.Line, start.Column, end.Line, end.Column);
        }

        //The following method does not check the validaity of the span
        public static Expression AddDebugInfo(Expression expression, SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn) {
            if (expression == null) {
                throw new System.ArgumentNullException(nameof(expression));
            }

            var sequencePoint = Expression.DebugInfo(document,
                startLine, startColumn, endLine, endColumn);

            var clearance = Expression.ClearDebugInfo(document);
            //always attach a clearance
            if (expression.Type == typeof(void)) {
                return Expression.Block(
                    sequencePoint,
                    expression,
                    clearance
                );
            }
            
            //save the expression to a variable
            var p = Expression.Parameter(expression.Type, null);
            return Expression.Block(
                new[] { p },
                sequencePoint,
                Expression.Assign(p, expression),
                clearance,
                p
            );
        }
    }
}
