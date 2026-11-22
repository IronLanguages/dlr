// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    ///   <para>The body's frame exposes two kinds of variables, which are lifted the way a synchronous debuggable body lifts its locals
    ///   (through <see cref="Expression.RuntimeVariables(ParameterExpression[])"/>, so the frame sees and changes the variables
    ///   themselves):</para>
    ///   <list type="bullet">
    ///     <item>The variables of the enclosing scopes that the host passes with the debug hook, typically the parameters and locals of the
    ///     function the body implements. To the body, they are what parameters are to a lambda: they come from outside and are visible at
    ///     every location of the body, so they are laid out as parameters.</item>
    ///     <item>The variables the body declares, each visible at the locations within its block. The blocks that contain a debug point
    ///     give up their variables, which are declared outside of the state machine instead, where the frame can reach them. This hoists
    ///     no variable that is not hoisted anyway, as the generator hoists the variables of each block that contains a yield, which a debug
    ///     point is.</item>
    ///   </list>
    /// </remarks>
    internal sealed class AsyncDebugSupport {
        private static readonly ConstructorInfo s_tracingStateMachineCtor
            = typeof(TracingStateMachine).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
                [typeof(IEnumerator<object?>), typeof(DebugContext), typeof(FunctionInfo), typeof(StrongBox<object?>), typeof(StrongBox<Exception?>),
                 typeof(bool), typeof(IRuntimeVariables)],
                modifiers: null)!;
        private static readonly MethodInfo s_onExceptionCaughtMethod
            = typeof(TracingStateMachine).GetMethod(nameof(TracingStateMachine.OnExceptionCaught), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private readonly DebugContext _debugContext;
        private readonly DebugLambdaInfo _lambdaInfo;
        private readonly string? _name;
        private readonly ParameterExpression _exceptionSlot;

        // Holds the TracingStateMachine, so that the body's exception handlers can report to it.
        private readonly ParameterExpression _tracer = Expression.Variable(typeof(TracingStateMachine), "$tracer");

        // The variables the frame exposes, and their descriptions, both in the order of the frame's slots: those of the enclosing scopes
        // first, then those the body declares.
        private readonly List<ParameterExpression> _variables = new();
        private readonly List<VariableInfo> _variableInfos = new();
        private readonly Dictionary<ParameterExpression, VariableInfo> _variableInfoMap = new();
        private readonly HashSet<string> _variableNames = new();
        private readonly int _enclosingVariableCount;

        // Null until the body is instrumented, and also if it has no sequence points (then there is nothing to trace).
        private FunctionInfo? _functionInfo;

        /// <param name="variables">The variables of the enclosing scopes that the body's frame exposes.</param>
        /// <param name="exceptionSlot">The body's exception slot, which the body rethrows from at each resume point.</param>
        internal AsyncDebugSupport(DebugContext debugContext, DebugLambdaInfo lambdaInfo, ReadOnlyCollection<ParameterExpression> variables,
                                   string? name, ParameterExpression exceptionSlot) {
            _debugContext = debugContext;
            _lambdaInfo = lambdaInfo;
            _name = name;
            _exceptionSlot = exceptionSlot;

            foreach (ParameterExpression variable in variables) {
                AddVariable(variable, isParameter: true);
            }
            _enclosingVariableCount = _variables.Count;
        }


        /// <summary>
        ///   Instruments <paramref name="body"/> to report its sequence points and the exceptions it handles.
        /// </summary>
        /// <remarks>
        ///   Each <see cref="DebugInfoExpression"/> becomes <c>{ yield DebugPoint(cookie); rethrow-if-pending }</c> on
        ///   <paramref name="yieldLabel"/>, the label the body's awaits and yields are sliced on, and each exception handler first reports
        ///   the exception it handles. The rethrow lets an exception raised by the trace callback surface inside the body. The blocks that
        ///   contain a debug point give up their variables to <see cref="TraceStateMachine"/>.
        /// </remarks>
        internal Expression InjectDebugPoints(Expression body, LabelTarget yieldLabel) {
            var injector = new DebugPointInjector(this, yieldLabel);
            Expression instrumentedBody = injector.Visit(body);
            if (injector.SequencePoints.Length == 0) {
                return body;
            }

            _functionInfo = DebugContext.CreateFunctionInfo(
                generatorFactory: null,     // the frame cannot be remapped to a generator, so it cannot change its location either
                _name ?? "<async>",
                injector.SequencePoints,
                injector.VariableScopeMap,
                _variableInfos,
                _lambdaInfo.CustomPayload);
            return instrumentedBody;
        }


        /// <summary>
        ///   Wraps <paramref name="stateMachine"/>, the state machine of the instrumented body, in a <see cref="TracingStateMachine"/>.
        /// </summary>
        /// <remarks>
        ///   The variables that the body's blocks gave up are declared here, around the state machine, and handed to the wrapper along with
        ///   those of the enclosing scopes.
        /// </remarks>
        /// <param name="resultSlot">Where the body leaves its return value, or null if it has none.</param>
        /// <param name="isAsyncEnumerable">Whether the state machine is driven by <c>AsyncHelpers.DriveAsyncEnumerable</c>.</param>
        internal Expression TraceStateMachine(Expression stateMachine, ParameterExpression? resultSlot, bool isAsyncEnumerable) {
            if (_functionInfo is null) {
                return stateMachine;
            }

            var blockVariables = new List<ParameterExpression>(1 + _variables.Count - _enclosingVariableCount) { _tracer };
            for (int i = _enclosingVariableCount; i < _variables.Count; i++) {
                blockVariables.Add(_variables[i]);
            }

            return Expression.Block(
                typeof(TracingStateMachine),
                blockVariables,
                Expression.Assign(
                    _tracer,
                    Expression.New(
                        s_tracingStateMachineCtor,
                        stateMachine,
                        Expression.Constant(_debugContext),
                        Expression.Constant(_functionInfo),
                        (Expression?)resultSlot ?? Expression.Constant(null, typeof(StrongBox<object?>)),
                        _exceptionSlot,
                        Expression.Constant(isAsyncEnumerable),
                        Expression.RuntimeVariables(_variables))));
        }


        /// <summary>
        ///   Lays out <paramref name="variable"/> as the next slot of the frame, unless it already has one.
        /// </summary>
        /// <remarks>
        ///   Names and hides the variable according to the host's <see cref="DebugLambdaInfo"/>, and gives it a name of its own, since the
        ///   frame looks its variables up by name (as <c>DebuggableLambdaBuilder</c> does).
        /// </remarks>
        private VariableInfo AddVariable(ParameterExpression variable, bool isParameter) {
            if (_variableInfoMap.TryGetValue(variable, out VariableInfo? info)) {
                return info;
            }

            if (_lambdaInfo.VariableAliases is null || !_lambdaInfo.VariableAliases.TryGetValue(variable, out string? alias)) {
                alias = variable.Name;
            }
            bool isHidden = _lambdaInfo.HiddenVariables is not null && _lambdaInfo.HiddenVariables.Contains(variable);
            if (alias is null) {
                alias = "local";
                isHidden = true;
            }

            string name = alias;
            for (int count = 1; !_variableNames.Add(name); count++) {
                name = alias + count.ToString(CultureInfo.InvariantCulture);
            }

            int index = _variables.Count;
            info = new VariableInfo(name, variable.Type, isParameter, isHidden, strongBoxed: true, localIndex: index, globalIndex: index);
            _variables.Add(variable);
            _variableInfos.Add(info);
            _variableInfoMap.Add(variable, info);
            return info;
        }


        /// <summary>
        ///   Rewrites the debug info and the exception handlers of an async body for <see cref="TracingStateMachine"/>.
        /// </summary>
        /// <remarks>
        ///   Each <see cref="DebugInfoExpression"/> becomes a yield of a <see cref="DebugPoint"/> on the body's own yield label, recording
        ///   the source span and the variables in scope per cookie, and each catch block first reports the exception to the tracer. This
        ///   mirrors what <c>DebugInfoRewriter</c> does to a synchronous body.
        /// </remarks>
        private sealed class DebugPointInjector : ExpressionVisitor {
            private readonly AsyncDebugSupport _support;
            private readonly LabelTarget _yieldLabel;
            private readonly List<DebugSourceSpan> _spans = new();

            // The variables in scope at each cookie, besides those of the enclosing scopes, which are in scope everywhere.
            private readonly List<IList<VariableInfo>> _scopes = new();

            // The blocks with variables that enclose the node being visited, and the variables in scope within each, for the frame to
            // find them by list (it caches its views of the variables per list).
            private readonly Stack<BlockExpression> _blocks = new();
            private readonly Dictionary<BlockExpression, IList<VariableInfo>> _blockScopes = new();

            internal DebugPointInjector(AsyncDebugSupport support, LabelTarget yieldLabel) {
                _support = support;
                _yieldLabel = yieldLabel;
            }

            internal DebugSourceSpan[] SequencePoints => _spans.ToArray();

            internal IList<VariableInfo>[] VariableScopeMap => _scopes.ToArray();

            protected override Expression VisitDebugInfo(DebugInfoExpression node) {
                if (node.IsClear) {
                    return Expression.Empty();
                }

                if (node.Document is null) {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, "DebugInfoExpression at sequence point {0} has no SymbolDocumentInfo", _spans.Count));
                }

                DebugSourceFile sourceFile = _support._debugContext.GetDebugSourceFile(
                    string.IsNullOrEmpty(node.Document.FileName) ? "<compile>" : node.Document.FileName);

                int cookie = _spans.Count;
                _spans.Add(new DebugSourceSpan(sourceFile, node.StartLine, node.StartColumn, node.EndLine, node.EndColumn));
                _scopes.Add(GetCurrentScope());

                return Expression.Block(
                    typeof(void),
                    Utils.YieldReturn(_yieldLabel, Expression.Constant(new DebugPoint(cookie))),
                    AsyncRewriter.RethrowIfPending(_support._exceptionSlot));
            }

            protected override Expression VisitBlock(BlockExpression node) {
                if (node.Variables.Count == 0) {
                    return base.VisitBlock(node);
                }

                int cookies = _spans.Count;
                _blocks.Push(node);
                BlockExpression visited;
                try {
                    visited = (BlockExpression)base.VisitBlock(node);
                } finally {
                    _blocks.Pop();
                }

                // Without a debug point inside, the variables are never in scope at one, so the block keeps them; otherwise they have
                // been laid out by GetCurrentScope, and TraceStateMachine declares them.
                if (_spans.Count == cookies) {
                    return visited;
                }
                return Expression.Block(visited.Type, visited.Expressions);
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
                        Expression.Block(Expression.Call(_support._tracer, s_onExceptionCaughtMethod, exception), handler.Body),
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
                if (node is AsyncExpression or GeneratorExpression
#if NET
                    or AsyncEnumerableExpression
#endif
                    ) {
                    return node;
                }
                if (node is YieldExpression { Value: not null } yield && yield.Target == _yieldLabel) {
                    return Expression.Block(
                        base.VisitExtension(node),
                        AsyncRewriter.RethrowIfPending(_support._exceptionSlot));
                }
                return base.VisitExtension(node);
            }

            /// <summary>
            ///   Gets the variables declared by the body that are in scope at the current location, outermost first, laying them out.
            /// </summary>
            private IList<VariableInfo> GetCurrentScope() {
                if (_blocks.Count == 0) {
                    return Array.Empty<VariableInfo>();
                }

                BlockExpression innermost = _blocks.Peek();
                if (!_blockScopes.TryGetValue(innermost, out IList<VariableInfo>? scope)) {
                    var variables = new List<VariableInfo>();
                    BlockExpression[] blocks = _blocks.ToArray();
                    for (int i = blocks.Length - 1; i >= 0; i--) {
                        foreach (ParameterExpression variable in blocks[i].Variables) {
                            VariableInfo info = _support.AddVariable(variable, isParameter: false);
                            // A variable of an enclosing scope is visible anyway.
                            if (!info.IsParameter && !variables.Contains(info)) {
                                variables.Add(info);
                            }
                        }
                    }
                    _blockScopes.Add(innermost, scope = variables);
                }
                return scope;
            }
        }
    }
}
