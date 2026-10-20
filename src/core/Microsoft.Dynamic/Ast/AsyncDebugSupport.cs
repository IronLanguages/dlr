// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Debugging;
using Microsoft.Scripting.Debugging.CompilerServices;

namespace Microsoft.Scripting.Ast {
    /// <summary>
    ///   Compiles an async body for tracing.
    /// </summary>
    /// <remarks>
    ///   Used by <see cref="AsyncRewriter"/> and <c>AsyncEnumerableRewriter</c> when the async node carries a debug hook (set by the host
    ///   only when it builds the tracing variant of the code, e.g. when <c>sys.settrace</c> is active). In two steps:
    ///   <list type="number">
    ///     <item><see cref="InjectDebugPoints"/> instruments the body before it is sliced into a state machine, so that the debug points
    ///     end up in the same state machine as the awaits and yields.</item>
    ///     <item><see cref="TraceStateMachine"/> wraps the resulting state machine in a <see cref="TracingStateMachine"/>, which dispatches
    ///     the trace events while the driver pumps it.</item>
    ///   </list>
    ///
    ///   <para>The body's locals are not lifted, so its frame reports its location but no locals; the scope map is therefore uniform
    ///   (every sequence point maps to no locals).</para>
    /// </remarks>
    internal sealed class AsyncDebugSupport {
        private static readonly ConstructorInfo s_tracingStateMachineCtor
            = typeof(TracingStateMachine).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
                [typeof(IEnumerator<object?>), typeof(DebugContext), typeof(FunctionInfo), typeof(StrongBox<object?>), typeof(StrongBox<Exception?>), typeof(bool)],
                modifiers: null)!;
        private static readonly MethodInfo s_onExceptionCaughtMethod
            = typeof(TracingStateMachine).GetMethod(nameof(TracingStateMachine.OnExceptionCaught), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private readonly DebugContext _debugContext;
        private readonly DebugLambdaInfo _lambdaInfo;
        private readonly string? _name;
        private readonly ParameterExpression _exceptionSlot;

        // Holds the TracingStateMachine, so that the body's exception handlers can report to it.
        private readonly ParameterExpression _tracer = Expression.Variable(typeof(TracingStateMachine), "$tracer");

        // Null until the body is instrumented, and also if it has no sequence points (then there is nothing to trace).
        private FunctionInfo? _functionInfo;

        /// <param name="exceptionSlot">The body's exception slot, which the body rethrows from at each resume point.</param>
        internal AsyncDebugSupport(DebugContext debugContext, DebugLambdaInfo lambdaInfo, string? name, ParameterExpression exceptionSlot) {
            _debugContext = debugContext;
            _lambdaInfo = lambdaInfo;
            _name = name;
            _exceptionSlot = exceptionSlot;
        }


        /// <summary>
        ///   Instruments <paramref name="body"/> to report its sequence points and the exceptions it handles.
        /// </summary>
        /// <remarks>
        ///   Each <see cref="DebugInfoExpression"/> becomes <c>{ yield DebugPoint(cookie); rethrow-if-pending }</c> on
        ///   <paramref name="yieldLabel"/>, the label the body's awaits and yields are sliced on, and each exception handler first reports
        ///   the exception it handles. The rethrow lets an exception raised by the trace callback surface inside the body.
        /// </remarks>
        internal Expression InjectDebugPoints(Expression body, LabelTarget yieldLabel) {
            var injector = new DebugPointInjector(_debugContext, yieldLabel, _exceptionSlot, _tracer);
            Expression instrumentedBody = injector.Visit(body);
            if (injector.SequencePoints.Length == 0) {
                return body;
            }

            _functionInfo = DebugContext.CreateFunctionInfo(
                generatorFactory: null,     // the frame cannot be remapped to a generator, so it cannot change its location either
                _name ?? "<async>",
                injector.SequencePoints,
                injector.VariableScopeMap,
                Array.Empty<VariableInfo>(),
                _lambdaInfo.CustomPayload);
            return instrumentedBody;
        }


        /// <summary>
        ///   Wraps <paramref name="stateMachine"/>, the state machine of the instrumented body, in a <see cref="TracingStateMachine"/>.
        /// </summary>
        /// <param name="resultSlot">Where the body leaves its return value, or null if it has none.</param>
        /// <param name="isAsyncEnumerable">Whether the state machine is driven by <c>AsyncHelpers.DriveAsyncEnumerable</c>.</param>
        internal Expression TraceStateMachine(Expression stateMachine, ParameterExpression? resultSlot, bool isAsyncEnumerable) {
            if (_functionInfo is null) {
                return stateMachine;
            }

            return Expression.Block(
                typeof(TracingStateMachine),
                [_tracer],
                Expression.Assign(
                    _tracer,
                    Expression.New(
                        s_tracingStateMachineCtor,
                        stateMachine,
                        Expression.Constant(_debugContext),
                        Expression.Constant(_functionInfo),
                        (Expression?)resultSlot ?? Expression.Constant(null, typeof(StrongBox<object?>)),
                        _exceptionSlot,
                        Expression.Constant(isAsyncEnumerable))));
        }


        /// <summary>
        ///   Rewrites the debug info and the exception handlers of an async body for <see cref="TracingStateMachine"/>.
        /// </summary>
        /// <remarks>
        ///   Each <see cref="DebugInfoExpression"/> becomes a yield of a <see cref="DebugPoint"/> on the body's own yield label, recording
        ///   the source span per cookie, and each catch block first reports the exception to the tracer. This mirrors what
        ///   <c>DebugInfoRewriter</c> does to a synchronous body.
        /// </remarks>
        private sealed class DebugPointInjector : ExpressionVisitor {
            private readonly DebugContext _debugContext;
            private readonly LabelTarget _yieldLabel;
            private readonly ParameterExpression _exceptionSlot;
            private readonly ParameterExpression _tracer;
            private readonly List<DebugSourceSpan> _spans = new();

            internal DebugPointInjector(DebugContext debugContext, LabelTarget yieldLabel, ParameterExpression exceptionSlot, ParameterExpression tracer) {
                _debugContext = debugContext;
                _yieldLabel = yieldLabel;
                _exceptionSlot = exceptionSlot;
                _tracer = tracer;
            }

            internal DebugSourceSpan[] SequencePoints => _spans.ToArray();

            internal IList<VariableInfo>[] VariableScopeMap {
                get {
                    var map = new IList<VariableInfo>[_spans.Count];
                    for (int i = 0; i < map.Length; i++) {
                        map[i] = Array.Empty<VariableInfo>();
                    }
                    return map;
                }
            }

            protected override Expression VisitDebugInfo(DebugInfoExpression node) {
                if (node.IsClear) {
                    return Expression.Empty();
                }

                if (node.Document is null) {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, "DebugInfoExpression at sequence point {0} has no SymbolDocumentInfo", _spans.Count));
                }

                DebugSourceFile sourceFile = _debugContext.GetDebugSourceFile(
                    string.IsNullOrEmpty(node.Document.FileName) ? "<compile>" : node.Document.FileName);

                int cookie = _spans.Count;
                _spans.Add(new DebugSourceSpan(sourceFile, node.StartLine, node.StartColumn, node.EndLine, node.EndColumn));

                return Expression.Block(
                    typeof(void),
                    Utils.YieldReturn(_yieldLabel, Expression.Constant(new DebugPoint(cookie))),
                    AsyncRewriter.RethrowIfPending(_exceptionSlot));
            }

            protected override Expression VisitTry(TryExpression node) {
                var visited = (TryExpression)base.VisitTry(node);
                if (visited.Handlers.Count == 0) {
                    return visited;
                }

                var handlers = new List<CatchBlock>(visited.Handlers.Count);
                foreach (CatchBlock handler in visited.Handlers) {
                    ParameterExpression variable = handler.Variable ?? Expression.Variable(handler.Test, null);
                    Expression exception = variable.Type == typeof(Exception) ? variable : Expression.Convert(variable, typeof(Exception));
                    handlers.Add(Expression.MakeCatchBlock(
                        handler.Test,
                        variable,
                        Expression.Block(Expression.Call(_tracer, s_onExceptionCaughtMethod, exception), handler.Body),
                        handler.Filter));
                }
                return Expression.MakeTry(visited.Type, visited.Body, visited.Finally, visited.Fault, handlers);
            }

            protected override Expression VisitLambda<T>(Expression<T> node) {
                // Don't descend into nested lambdas — they have their own debug context.
                return node;
            }

            protected override Expression VisitExtension(Expression node) {
                // A nested async node is sliced into its own state machine against its own label; leave it opaque.
                if (node is AsyncExpression
#if NET
                    or AsyncEnumerableExpression
#endif
                    ) {
                    return node;
                }
                return base.VisitExtension(node);
            }
        }
    }
}
