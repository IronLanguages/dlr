// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    ///  Reduces an <see cref="AsyncExpression"/> to a <c>Task&lt;object&gt;</c>-valued
    ///  expression tree that yields each <see cref="AwaitExpression"/>'s operand and
    ///  hands the resulting state machine to <see cref="Microsoft.Scripting.Runtime.AsyncHelpers.DriveAsync"/>.
    /// </summary>
    internal sealed class AsyncRewriter {
        private static readonly MethodInfo s_driveMethod
            = typeof(Microsoft.Scripting.Runtime.AsyncHelpers).GetMethod("DriveAsync")!;
        private static readonly FieldInfo s_valueSlotField
            = typeof(StrongBox<object?>).GetField(nameof(StrongBox<object?>.Value))!;
        private static readonly FieldInfo s_exceptionSlotField
            = typeof(StrongBox<Exception?>).GetField(nameof(StrongBox<Exception?>.Value))!;
        private static readonly ConstructorInfo s_valueSlotCtor
            = typeof(StrongBox<object?>).GetConstructor(Type.EmptyTypes)!;
        private static readonly ConstructorInfo s_exceptionSlotCtor
            = typeof(StrongBox<Exception?>).GetConstructor(Type.EmptyTypes)!;

        private readonly AsyncExpression _node;

        public AsyncRewriter(AsyncExpression node) {
            _node = node;
        }

        public Expression Reduce() {
            // valueSlot is value cell shared with AsyncHelpers.DriveAsync.
            //  - At each await: the runner writes the awaited result here just before resuming the body, and the body reads it via the
            //    `readSlot` expression the rewriter inserts after each yield.
            //  - At the end of the body: the body's final return value is written here (see captureFinalValue below). After MoveNext()
            //    returns false, DriveAsync reads the same slot and returns it as the Task's result.
            //
            // The two uses do not overlap — by the time captureFinalValue runs, the last per-await read has already been consumed or
            // discarded by the surrounding expression.
            ParameterExpression valueSlot = Expression.Variable(typeof(StrongBox<object?>), "$asyncValue");

            // exceptionSlot is where the runner stashes a faulted-await exception so the state machine can rethrow at the resumed position
            // (lets a Python try/except around `await` observe e.g. StopAsyncIteration).
            ParameterExpression exceptionSlot = Expression.Variable(typeof(StrongBox<Exception?>), "$awaitException");
            LabelTarget yieldLabel = Expression.Label(typeof(object), "$asyncYield");

            // Rewrite AwaitExpression(e) -> { yield e; rethrow-if-pending; valueSlot.Value }
            var rewriter = new AwaitToYieldRewriter(yieldLabel, valueSlot, exceptionSlot);
            Expression rewrittenBody = rewriter.Visit(_node.Body);

            // After the body completes, the function's final value must live in valueSlot for DriveAsync to pick up. For a value-typed body this
            // is a single assignment of the body expression into the slot. For a void body, the body has no value to assign, but we still
            // must clear the slot — otherwise DriveAsync would return whatever the last await happened to stash there. (IronPython doesn't emit
            // void async bodies today, but AsyncExpression is language-agnostic.)
            Expression valueField = Expression.Field(valueSlot, s_valueSlotField);
            Expression captureFinalValue;
            if (_node.Body.Type == typeof(void)) {
                captureFinalValue = Expression.Block(
                    typeof(void),
                    rewrittenBody,
                    Expression.Assign(valueField, Expression.Constant(null, typeof(object))));
            } else {
                Expression asObject = rewrittenBody.Type == typeof(object)
                    ? rewrittenBody
                    : Expression.Convert(rewrittenBody, typeof(object));
                captureFinalValue = Expression.Assign(valueField, asObject);
            }
            Expression generatorBody = Expression.Block(typeof(void), captureFinalValue);

            Expression generator = Utils.Generator(
                _node.Name ?? "$async",
                yieldLabel,
                generatorBody,
                typeof(IEnumerator<object>),
                rewriteAssignments: false);

            Expression drive = Expression.Call(
                s_driveMethod,
                generator,
                valueSlot,
                exceptionSlot,
                _node.CancellationToken,
                _node.CancellationException);

            return Expression.Block(
                typeof(Task<object?>),
                [valueSlot, exceptionSlot],
                Expression.Assign(valueSlot, Expression.New(s_valueSlotCtor)),
                Expression.Assign(exceptionSlot, Expression.New(s_exceptionSlotCtor)),
                drive);
        }

        private sealed class AwaitToYieldRewriter : ExpressionVisitor {
            private static readonly MethodInfo s_captureMethod
                = typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Capture))!;
            private static readonly MethodInfo s_throwMethod
                = typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Throw), Type.EmptyTypes)!;

            private readonly LabelTarget _yieldLabel;
            private readonly ParameterExpression _resultSlot;
            private readonly ParameterExpression _exceptionSlot;

            public AwaitToYieldRewriter(LabelTarget yieldLabel, ParameterExpression resultSlot, ParameterExpression exceptionSlot) {
                _yieldLabel = yieldLabel;
                _resultSlot = resultSlot;
                _exceptionSlot = exceptionSlot;
            }

            protected override Expression VisitExtension(Expression node) {
                if (node is AwaitExpression aw) {
                    Expression operand = Visit(aw.Operand);
                    Expression boxed = operand.Type == typeof(object) ? operand : Expression.Convert(operand, typeof(object));
                    Expression readException = Expression.Field(_exceptionSlot, s_exceptionSlotField);
                    Expression readSlot = Expression.Field(_resultSlot, s_valueSlotField);

                    // After the yield, if the runner stored an exception, rethrow it preserving the original stack trace 
                    // (so the body's try/except observes the right exception object).
                    // Otherwise return the awaited result.
                    Expression rethrow = Expression.IfThen(
                        Expression.ReferenceNotEqual(readException, Expression.Constant(null, typeof(Exception))),
                        Expression.Call(
                            Expression.Call(s_captureMethod, readException),
                            s_throwMethod));

                    return Expression.Block(
                        typeof(object),
                        Utils.YieldReturn(_yieldLabel, boxed),
                        rethrow,
                        readSlot);
                }
                return base.VisitExtension(node);
            }
        }
    }
}
