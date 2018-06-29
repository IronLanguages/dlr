// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using Microsoft.Scripting.Interpreter;

namespace Microsoft.Scripting.Ast {
    public class LightLambdaExpression : Expression {
        internal LightLambdaExpression(Type retType, Expression body, string name, IList<ParameterExpression> args) {
            Body = body;
            Name = name;
            Parameters = args;
            ReturnType = retType;
        }

        public Expression Body { get; }

        public string Name { get; }

        public IList<ParameterExpression> Parameters { get; }

        internal virtual LambdaExpression ReduceToLambdaWorker() {
            throw new InvalidOperationException();
        }

        public Delegate Compile() {
            return Compile(-1);
        }

        public Delegate Compile(int compilationThreshold) {
            return new LightCompiler(compilationThreshold).CompileTop(this).CreateDelegate();
        }

        public override ExpressionType NodeType {
            get { return ExpressionType.Extension; }
        }

        public override bool CanReduce {
            get { return true; }
        }

        public override Expression Reduce() {
            return ReduceToLambdaWorker();
        }

        public Type ReturnType { get; }
    }

    internal class TypedLightLambdaExpression : LightLambdaExpression {
        private readonly Type _delegateType;

        internal TypedLightLambdaExpression(Type retType, Type delegateType, Expression body, string name, IList<ParameterExpression> args)
            : base(retType, body, name, args) {
            _delegateType = delegateType;
        }

        internal override LambdaExpression ReduceToLambdaWorker() {
            return Expression.Lambda(
                _delegateType,
                Body,
                Name,
                Parameters
            );
        }

        public override Type Type {
            get { return _delegateType; }
        }
    }

    public class LightExpression<T> : LightLambdaExpression {
        internal LightExpression(Type retType, Expression body, string name, IList<ParameterExpression> args)
            : base(retType, body, name, args) {
        }

        public Expression<T> ReduceToLambda() {
            return Expression.Lambda<T>(Body, Name, Parameters);
        }

        public override Type Type {
            get { return typeof(T); }
        }

        public new T Compile() {
            return Compile(-1);
        }

        public new T Compile(int compilationThreshold) {
            return (T)(object)new LightCompiler(compilationThreshold).CompileTop(this).CreateDelegate();
        }

        internal override LambdaExpression ReduceToLambdaWorker() {
            return ReduceToLambda();
        }
    }

    public static partial class Utils {
        public static LightExpression<T> LightLambda<T>(Type retType, Expression body, string name, IList<ParameterExpression> args) {
            return new LightExpression<T>(retType, body, name, args);
        }

        public static LightLambdaExpression LightLambda(Type retType, Type delegateType, Expression body, string name, IList<ParameterExpression> args) {
            return new TypedLightLambdaExpression(retType, delegateType, body, name, args);
        }
    }

}
