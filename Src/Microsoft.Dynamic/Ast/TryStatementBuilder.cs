// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    public sealed class TryStatementBuilder {
        private readonly List<CatchBlock> _catchBlocks = new List<CatchBlock>();
        private Expression _try;
        private Expression _finally, _fault;
        private bool _enableJumpsFromFinally;

        internal TryStatementBuilder(Expression body) {
            _try = body;
        }

        public TryStatementBuilder Catch(Type type, Expression body) {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(body, nameof(body));
            if (_finally != null) {
                throw Error.FinallyAlreadyDefined();
            }

            _catchBlocks.Add(Expression.Catch(type, body));
            return this;
        }

        public TryStatementBuilder Catch(Type type, Expression expr0, Expression expr1) {
            return Catch(type, Expression.Block(expr0, expr1));
        }

        public TryStatementBuilder Catch(Type type, Expression expr0, Expression expr1, Expression expr2) {
            return Catch(type, Expression.Block(expr0, expr1, expr2));
        }

        public TryStatementBuilder Catch(Type type, Expression expr0, Expression expr1, Expression expr2, Expression expr3) {
            return Catch(type, Expression.Block(expr0, expr1, expr2, expr3));
        }

        public TryStatementBuilder Catch(Type type, params Expression[] body) {
            return Catch(type, Expression.Block(body));
        }

        public TryStatementBuilder Catch(ParameterExpression holder, Expression expr0, Expression expr1) {
            return Catch(holder, Expression.Block(expr0, expr1));
        }

        public TryStatementBuilder Catch(ParameterExpression holder, Expression expr0, Expression expr1, Expression expr2) {
            return Catch(holder, Expression.Block(expr0, expr1, expr2));
        }

        public TryStatementBuilder Catch(ParameterExpression holder, Expression expr0, Expression expr1, Expression expr2, Expression expr3) {
            return Catch(holder, Expression.Block(expr0, expr1, expr2, expr3));
        }

        public TryStatementBuilder Catch(ParameterExpression holder, params Expression[] body) {
            return Catch(holder, Utils.Block(body));
        }

        public TryStatementBuilder Catch(ParameterExpression holder, Expression body) {
            ContractUtils.RequiresNotNull(holder, nameof(holder));
            ContractUtils.RequiresNotNull(body, nameof(body));

            if (_finally != null) {
                throw Error.FinallyAlreadyDefined();
            }

            _catchBlocks.Add(Expression.Catch(holder, body));
            return this;
        }

        public TryStatementBuilder Filter(Type type, Expression condition, params Expression[] body) {
            return Filter(type, condition, Utils.Block(body));
        }

        public TryStatementBuilder Filter(Type type, Expression condition, Expression body) {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(condition, nameof(condition));
            ContractUtils.RequiresNotNull(body, nameof(body));

            _catchBlocks.Add(Expression.Catch(type, body, condition));
            return this;
        }

        public TryStatementBuilder Filter(ParameterExpression holder, Expression condition, params Expression[] body) {
            return Filter(holder, condition, Utils.Block(body));
        }

        public TryStatementBuilder Filter(ParameterExpression holder, Expression condition, Expression body) {
            ContractUtils.RequiresNotNull(holder, nameof(holder));
            ContractUtils.RequiresNotNull(condition, nameof(condition));
            ContractUtils.RequiresNotNull(body, nameof(body));

            _catchBlocks.Add(Expression.Catch(holder, body, condition));
            return this;
        }

        public TryStatementBuilder Finally(params Expression[] body) {
            return Finally(Utils.BlockVoid(body));
        }

        public TryStatementBuilder Finally(Expression body) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            if (_finally != null) {
                throw Error.FinallyAlreadyDefined();
            }

            if (_fault != null) {
                throw Error.CannotHaveFaultAndFinally();
            }

            _finally = body;
            return this;
        }

        public TryStatementBuilder FinallyWithJumps(params Expression[] body) {
            _enableJumpsFromFinally = true;
            return Finally(body);
        }

        public TryStatementBuilder FinallyWithJumps(Expression body) {
            _enableJumpsFromFinally = true;
            return Finally(body);
        }

        public TryStatementBuilder Fault(params Expression[] body) {
            ContractUtils.RequiresNotNullItems(body, nameof(body));

            if (_finally != null) {
                throw Error.CannotHaveFaultAndFinally();
            }

            if (_fault != null) {
                throw Error.FaultAlreadyDefined();
            }

            _fault = body.Length == 1 ? body[0] : Utils.BlockVoid(body);

            return this;
        }

        public static implicit operator Expression(TryStatementBuilder builder) {
            ContractUtils.RequiresNotNull(builder, nameof(builder));
            return builder.ToExpression();
        }

        public Expression ToExpression() {

            //
            // We can't emit a real filter or fault because they don't
            // work in DynamicMethods. Instead we do a simple transformation:
            //   fault -> catch (Exception) { ...; rethrow }
            //   filter -> catch (ExceptionType) { if (!filter) rethrow; ... }
            //
            // Note that the filter transformation is not quite equivalent to
            // real CLR semantics, but it's what IronPython and IronRuby
            // expect. If we get CLR support we'll switch over to real filter
            // and fault blocks.
            //

            var handlers = new List<CatchBlock>(_catchBlocks);
            for (int i = 0, n = handlers.Count; i < n ; i++) {
                CatchBlock handler = handlers[i];
                if (handler.Filter != null) {
                    handlers[i] = Expression.MakeCatchBlock(
                        handler.Test,
                        handler.Variable,
                        Expression.Condition(
                            handler.Filter,
                            handler.Body,
                            Expression.Rethrow(handler.Body.Type)
                        ),
                        null
                    );
                }
            }

            if (_fault != null) {
                ContractUtils.Requires(handlers.Count == 0, "fault cannot be used with catch or finally clauses");
                handlers.Add(Expression.Catch(typeof(Exception), Expression.Block(_fault, Expression.Rethrow(_try.Type))));
            }

            var result = Expression.MakeTry(null, _try, _finally, null, handlers);
            if (_enableJumpsFromFinally) {
                return Utils.FinallyFlowControl(result);
            }
            return result;
        }
    }

#if TODO // better support for fault in interpreter
    public class TryFaultExpression : Expression, IInstructionProvider {
        private readonly Expression _body;
        private readonly Expression _fault;

        internal TryFaultExpression(Expression body, Expression fault) {
            _body = body;
            _fault = fault;
        }

        protected override ExpressionType NodeTypeImpl() {
            return ExpressionType.Extension;
        }

        protected override Type/*!*/ TypeImpl() {
            return _body.Type;
        }

        public override bool CanReduce {
            get {
                return true;
            }
        }

        public override Expression/*!*/ Reduce() {
            return Expression.TryCatch(
                _body,
                Expression.Catch(typeof(Exception), Expression.Block(_fault, Expression.Rethrow(_body.Type)))
            );
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression body = visitor(_body);
            Expression fault = visitor(_fault);
            if (body != _body || fault != _fault) {
                return new TryFaultExpression(body, fault);
            }

            return this;
        }

        public void AddInstructions(LightCompiler compiler) {
            compiler.Compile(Expression.TryFault(_body, _fault));
        }
    }
#endif

    public partial class Utils {
        public static TryStatementBuilder Try(Expression body) {
            ContractUtils.RequiresNotNull(body, nameof(body));
            return new TryStatementBuilder(body);
        }

        public static TryStatementBuilder Try(Expression expr0, Expression expr1) {
            return new TryStatementBuilder(Expression.Block(expr0, expr1));
        }

        public static TryStatementBuilder Try(Expression expr0, Expression expr1, Expression expr2) {
            return new TryStatementBuilder(Expression.Block(expr0, expr1, expr2));
        }

        public static TryStatementBuilder Try(Expression expr0, Expression expr1, Expression expr2, Expression expr3) {
            return new TryStatementBuilder(Expression.Block(expr0, expr1, expr2, expr3));
        }

        public static TryStatementBuilder Try(params Expression[] body) {
            return new TryStatementBuilder(Expression.Block(body));
        }
    }
}
