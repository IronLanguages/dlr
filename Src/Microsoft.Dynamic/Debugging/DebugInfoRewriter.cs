// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Debugging {
    using Ast = MSAst.Expression;
    using System.Threading;
    
    /// <summary>
    /// Used to rewrite expressions containing DebugInfoExpressions.
    /// </summary>
    internal class DebugInfoRewriter : MSAst.DynamicExpressionVisitor {
        private readonly DebugContext _debugContext;
        private readonly bool _transformToGenerator;
        private readonly Expression _thread;
        private readonly Expression _frame;
        private readonly Expression _debugMarker;
        private readonly Expression _traceLocations;
        private readonly Dictionary<ParameterExpression, MSAst.ParameterExpression> _replacedLocals;
        private readonly Dictionary<ParameterExpression, VariableInfo> _localsToVarInfos;
        private readonly Stack<BlockExpression> _currentLocals;
        private readonly Dictionary<int, DebugSourceSpan> _markerLocationMap;
        private readonly Dictionary<int, IList<VariableInfo>> _variableScopeMap;
        private readonly Dictionary<BlockExpression, IList<VariableInfo>> _variableScopeMapCache;
        private readonly Dictionary<DebugSourceFile, ParameterExpression> _sourceFilesToVariablesMap;
        private readonly Expression _globalDebugMode;
        private readonly LabelTarget _generatorLabelTarget;
        private readonly ConstantExpression _debugYieldValue;
        private readonly Expression _pushFrame;
        private readonly DebugLambdaInfo _lambdaInfo;
        private int _locationCookie;
        private bool _hasUnconditionalFunctionCalls;
        private bool _insideConditionalBlock;

        internal DebugInfoRewriter(
            DebugContext debugContext,
            bool transformToGenerator,
            Expression traceLocations,
            Expression thread,
            Expression frame,
            Expression pushFrame,
            Expression debugMarker,
            Expression globalDebugMode,
            Dictionary<DebugSourceFile, ParameterExpression> sourceFilesToVariablesMap,
            LabelTarget generatorLabel,
            Dictionary<ParameterExpression, ParameterExpression> replacedLocals,
            Dictionary<ParameterExpression, VariableInfo> localsToVarInfos,
            DebugLambdaInfo lambdaInfo) {

            _debugContext = debugContext;
            _transformToGenerator = transformToGenerator;
            _traceLocations = traceLocations;
            _thread = thread;
            _frame = frame;
            _pushFrame = pushFrame;

            if (_transformToGenerator) {
                _debugYieldValue = Ast.Constant(DebugContext.DebugYieldValue);

                // When transforming to generator we'll also create marker-location and position-handler maps
                _markerLocationMap = new Dictionary<int, DebugSourceSpan>();
                _variableScopeMap = new Dictionary<int, IList<VariableInfo>>();
                _currentLocals = new Stack<BlockExpression>();
                _variableScopeMapCache = new Dictionary<BlockExpression, IList<VariableInfo>>();
            }

            _debugMarker = debugMarker;
            _globalDebugMode = globalDebugMode;
            _sourceFilesToVariablesMap = sourceFilesToVariablesMap;
            _generatorLabelTarget = generatorLabel;
            _replacedLocals = replacedLocals;
            _localsToVarInfos = localsToVarInfos;
            _lambdaInfo = lambdaInfo;
        }

        internal DebugSourceSpan[] DebugMarkerLocationMap {
            get {
                DebugSourceSpan[] locationArray = new DebugSourceSpan[_locationCookie];
                for (int i = 0; i < locationArray.Length; i++) {
                    if (_markerLocationMap.TryGetValue(i, out DebugSourceSpan location)) {
                        locationArray[i] = location;
                    }
                }

                return locationArray;
            }
        }

        internal bool HasUnconditionalFunctionCalls {
            get { return _hasUnconditionalFunctionCalls; }
        }

        internal IList<VariableInfo>[] VariableScopeMap {
            get {
                IList<VariableInfo>[] scopeArray = new IList<VariableInfo>[_locationCookie];
                for (int i = 0; i < scopeArray.Length; i++) {
                    IList<VariableInfo> scope;
                    if (_variableScopeMap.TryGetValue(i, out scope)) {
                        scopeArray[i] = scope;
                    }
                }

                return scopeArray;
            }
        }

        protected override Expression VisitLambda<T>(MSAst.Expression<T> node) {
            // Explicitely don't walk nested lambdas.  They should already have been transformed
            return node;
        }

        // We remove all nested variables declared inside the block.
        protected override Expression VisitBlock(MSAst.BlockExpression node) {
            if (_transformToGenerator) {
                _currentLocals.Push(node);
            }
            try {
                BlockExpression modifiedBlock = Ast.Block(node.Type, node.Expressions);
                return base.VisitBlock(modifiedBlock);
            } finally {
                if (_transformToGenerator) {
#if DEBUG
                    BlockExpression poppedBlock =
#endif
                    _currentLocals.Pop();
#if DEBUG
                    Debug.Assert(Type.ReferenceEquals(node, poppedBlock));
#endif
                }
            }
        }

        protected override Expression VisitTry(TryExpression node) {
            MSAst.Expression b = Visit(node.Body);
            ReadOnlyCollection<CatchBlock> h = Visit(node.Handlers, VisitCatchBlock);
            MSAst.Expression y = Visit(node.Finally);
            MSAst.Expression f;

            _insideConditionalBlock = true;
            try {
                f = Visit(node.Fault);
            } finally {
                _insideConditionalBlock = false;
            }

            node = Ast.MakeTry(node.Type, b, y, f, h);

            List<MSAst.CatchBlock> newHandlers = null;
            MSAst.Expression newFinally = null;

            // If the TryStatement has any Catch blocks we need to insert the exception
            // event as a first statement so that we can be notified of first-chance exceptions.
            if (node.Handlers != null && node.Handlers.Count > 0) {
                newHandlers = new List<CatchBlock>();

                foreach (var catchBlock in node.Handlers) {
                    ParameterExpression exceptionVar = catchBlock.Variable ?? Ast.Parameter(catchBlock.Test, null);

                    Expression debugMarker, thread;
                    if (_transformToGenerator) {
                        debugMarker = Ast.Call(
                            typeof(RuntimeOps).GetMethod("GetCurrentSequencePointForGeneratorFrame"),
                            _frame
                        );

                        thread = Ast.Call(typeof(RuntimeOps).GetMethod("GetThread"), _frame);
                    } else {
                        debugMarker = _debugMarker;
                        thread = _thread;
                    }

                    MSAst.Expression exceptionEvent = Ast.Block(
                        // Rethrow ForceToGeneratorLoopException
                        AstUtils.If(
                            Ast.TypeIs(
                                exceptionVar,
                                typeof(ForceToGeneratorLoopException)
                            ),
                            Ast.Throw(exceptionVar)
                        ),
                        AstUtils.If(
                            Ast.Equal(_globalDebugMode, AstUtils.Constant((int)DebugMode.FullyEnabled)),
                            _pushFrame ?? Ast.Empty(),
                            Ast.Call(
                                typeof(RuntimeOps).GetMethod("OnTraceEvent"),
                               thread,
                               debugMarker,
                               exceptionVar
                            )
                        )
                    );

                    newHandlers.Add(Ast.MakeCatchBlock(
                        catchBlock.Test,
                        exceptionVar,
                        Ast.Block(
                            exceptionEvent,
                            catchBlock.Body
                        ),
                        catchBlock.Filter
                    ));
                }
            }

            if (!_transformToGenerator && node.Finally != null) {
                // Prevent the user finally block from running if the frame is currently remapping to generator
                newFinally = AstUtils.If(
                    Ast.Not(
                        Ast.Call(
                            typeof(RuntimeOps).GetMethod("IsCurrentLeafFrameRemappingToGenerator"),
                            _thread
                        )
                    ),
                    node.Finally
                );
            }

            if (newHandlers != null || newFinally != null) {
                node = Ast.MakeTry(
                    node.Type,
                    node.Body,
                    newFinally ?? node.Finally,
                    node.Fault,
                    newHandlers != null ? (IEnumerable<CatchBlock>)newHandlers : node.Handlers
                );
            }

            return node;
        }

        protected override MSAst.CatchBlock VisitCatchBlock(MSAst.CatchBlock node) {
            MSAst.ParameterExpression v = VisitAndConvert(node.Variable, "VisitCatchBlock");

            MSAst.Expression f;
            MSAst.Expression b;

            _insideConditionalBlock = true;
            try {
                f = Visit(node.Filter);
                b = Visit(node.Body);
            } finally {
                _insideConditionalBlock = false;
            }

            if (v == node.Variable && b == node.Body && f == node.Filter) {
                return node;
            }
            return Ast.MakeCatchBlock(node.Test, v, b, f);
        }

        protected override MSAst.Expression VisitConditional(MSAst.ConditionalExpression node) {
            MSAst.Expression t = Visit(node.Test);

            MSAst.Expression l;
            MSAst.Expression r;

            _insideConditionalBlock = true;
            try {
                l = Visit(node.IfTrue);
                r = Visit(node.IfFalse);
            } finally {
                _insideConditionalBlock = false;
            }

            if (t == node.Test && l == node.IfTrue && r == node.IfFalse) {
                return node;
            }
            return Ast.Condition(t, l, r, node.Type);
        }

        protected override MSAst.SwitchCase VisitSwitchCase(MSAst.SwitchCase node) {
            _insideConditionalBlock = true;
            try {
                return base.VisitSwitchCase(node);
            } finally {
                _insideConditionalBlock = false;
            }
        }

        protected override MSAst.Expression VisitDynamic(MSAst.DynamicExpression node) {
            return VisitCall(base.VisitDynamic(node));
        }

        protected override MSAst.Expression VisitMethodCall(MSAst.MethodCallExpression node) {
            return VisitCall(base.VisitMethodCall(node));
        }

        protected override MSAst.Expression VisitInvocation(MSAst.InvocationExpression node) {
            return VisitCall(base.VisitInvocation(node));
        }

        protected override MSAst.Expression VisitNew(MSAst.NewExpression node) {
            return VisitCall(base.VisitNew(node));
        }

        // This method does 2 things:
        //  1. It records whether a call expression is within a conditional block.
        //  2. It inserts push-frame expression before call expression.  This is done on 2nd tree-walk, and
        //     only if there are no unconditional func. calls.
        internal MSAst.Expression VisitCall(MSAst.Expression node) {
            if (_lambdaInfo.OptimizeForLeafFrames && (_lambdaInfo.CompilerSupport == null || _lambdaInfo.CompilerSupport.IsCallToDebuggableLambda(node))) {
                // If we're inside a conditional block record the fact that we have uncodintional function calls
                if (!_insideConditionalBlock) {
                    _hasUnconditionalFunctionCalls = true;
                }

                // Insert the push-frame expression
                if (!_transformToGenerator && _pushFrame != null) {
                    return Ast.Block(_pushFrame, node);
                }
            }

            return node;
        }

        protected override MSAst.Expression VisitParameter(MSAst.ParameterExpression node) {
            if (_replacedLocals == null) {
                return base.VisitParameter(node);
            }

            if (_replacedLocals.TryGetValue(node, out ParameterExpression replacement)) {
                return replacement;
            }

            return base.VisitParameter(node);
        }

        protected override MSAst.Expression VisitDebugInfo(MSAst.DebugInfoExpression node) {
            if (!node.IsClear) {
                MSAst.Expression transformedExpression;

                // Verify that DebugInfoExpression has valid SymbolDocumentInfo
                if (node.Document == null) {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ErrorStrings.DebugInfoWithoutSymbolDocumentInfo,
                            _locationCookie));
                }

                DebugSourceFile sourceFile = _debugContext.GetDebugSourceFile(
                    String.IsNullOrEmpty(node.Document.FileName) ? "<compile>" : node.Document.FileName);

                // Update the location cookie
                int locationCookie = _locationCookie++;
                if (!_transformToGenerator) {
                    MSAst.Expression tracebackCall = null;
                    if (locationCookie == 0) {
                        tracebackCall = Ast.Empty();
                    } else {
                        tracebackCall = Ast.Call(
                            typeof(RuntimeOps).GetMethod("OnTraceEvent"),
                            _thread,
                            AstUtils.Constant(locationCookie),
                            Ast.Convert(Ast.Constant(null), typeof(Exception))
                        );
                    }

                    transformedExpression = Ast.Block(
                        Ast.Assign(
                            _debugMarker,
                            AstUtils.Constant(locationCookie)
                        ),
                        Ast.IfThen(
                            Ast.GreaterThan(
                                Ast.Property(_sourceFilesToVariablesMap[sourceFile], "Mode"),
                                Ast.Constant((int)DebugMode.ExceptionsOnly)
                            ),
                            Ast.IfThen(
                                Ast.OrElse(
                                    Ast.Equal(
                                        Ast.Property(_sourceFilesToVariablesMap[sourceFile], "Mode"),
                                        Ast.Constant((int)DebugMode.FullyEnabled)
                                    ),
                                    Ast.ArrayIndex(
                                        _traceLocations,
                                        AstUtils.Constant(locationCookie)
                                    )
                                ),
                                Ast.Block(
                                    _pushFrame ?? Ast.Empty(),
                                    tracebackCall
                                )
                            )
                        )
                    );
                } else {
                    Debug.Assert(_generatorLabelTarget != null);

                    transformedExpression = Ast.Block(
                        AstUtils.YieldReturn(
                            _generatorLabelTarget,
                            _debugYieldValue,
                            locationCookie
                        )
                    );

                    // Update the variable scope map
                    if (_currentLocals.Count > 0) {
                        BlockExpression curentBlock = _currentLocals.Peek();
                        if (!_variableScopeMapCache.TryGetValue(curentBlock, out IList<VariableInfo> scopedVaribles)) {
                            scopedVaribles = new List<VariableInfo>();
                            BlockExpression[] blocks = _currentLocals.ToArray();
                            for (int i = blocks.Length - 1; i >= 0; i--) {
                                foreach (var variable in blocks[i].Variables) {
                                    scopedVaribles.Add(_localsToVarInfos[variable]);
                                }
                            }

                            _variableScopeMapCache.Add(curentBlock, scopedVaribles);
                        }

                        _variableScopeMap.Add(locationCookie, scopedVaribles);
                    }

                    DebugSourceSpan span = new DebugSourceSpan(
                        sourceFile,
                        node.StartLine,
                        node.StartColumn,
                        node.EndLine,
                        node.EndColumn);

                    // Update the location-span map
                    _markerLocationMap.Add(locationCookie, span);
                }

                return transformedExpression;
            }

            return Ast.Empty();
        }
    }
}
