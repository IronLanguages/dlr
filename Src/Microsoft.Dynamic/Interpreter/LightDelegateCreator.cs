// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {

    /// <summary>
    /// Manages creation of interpreted delegates. These delegates will get
    /// compiled if they are executed often enough.
    /// </summary>
    internal sealed class LightDelegateCreator {
        // null if we are forced to compile
        private readonly Expression _lambda;

        // Adaptive compilation support:
        private Type _compiledDelegateType;
        private Delegate _compiled;
        private readonly object _compileLock = new object();

        internal LightDelegateCreator(Interpreter interpreter, LambdaExpression lambda) {
            Assert.NotNull(lambda);
            Interpreter = interpreter;
            _lambda = lambda;
        }

        internal LightDelegateCreator(Interpreter interpreter, LightLambdaExpression lambda) {
            Assert.NotNull(lambda);
            Interpreter = interpreter;
            _lambda = lambda;
        }

        internal Interpreter Interpreter { get; }

        private bool HasClosure => Interpreter != null && Interpreter.ClosureSize > 0;

        internal bool HasCompiled => _compiled != null;

        /// <summary>
        /// true if the compiled delegate has the same type as the lambda;
        /// false if the type was changed for interpretation.
        /// </summary>
        internal bool SameDelegateType => _compiledDelegateType == DelegateType;

        internal Delegate CreateDelegate() {
            return CreateDelegate(null);
        }

        internal Delegate CreateDelegate(StrongBox<object>[] closure) {
            if (_compiled != null) {
                // If the delegate type we want is not a Func/Action, we can't
                // use the compiled code directly. So instead just fall through
                // and create an interpreted LightLambda, which will pick up
                // the compiled delegate on its first run.
                //
                // Ideally, we would just rebind the compiled delegate using
                // Delegate.CreateDelegate. Unfortunately, it doesn't work on
                // dynamic methods.
                if (SameDelegateType) {
                    return CreateCompiledDelegate(closure);
                }
            }

            if (Interpreter == null) {
                // We can't interpret, so force a compile
                Compile(null);
                Delegate compiled = CreateCompiledDelegate(closure);
                Debug.Assert(compiled.GetType() == DelegateType);
                return compiled;
            }

            // Otherwise, we'll create an interpreted LightLambda
            return new LightLambda(this, closure, Interpreter._compilationThreshold).MakeDelegate(DelegateType);
        }

        private Type DelegateType {
            get {
                if (_lambda is LambdaExpression le) {
                    return le.Type;
                }

                return ((LightLambdaExpression)_lambda).Type;
            }
        }

        /// <summary>
        /// Used by LightLambda to get the compiled delegate.
        /// </summary>
        internal Delegate CreateCompiledDelegate(StrongBox<object>[] closure) {
            Debug.Assert(HasClosure == (closure != null));

            if (HasClosure) {
                // We need to apply the closure to get the actual delegate.
                var applyClosure = (Func<StrongBox<object>[], Delegate>)_compiled;
                return applyClosure(closure);
            }
            return _compiled;
        }

        /// <summary>
        /// Create a compiled delegate for the LightLambda, and saves it so
        /// future calls to Run will execute the compiled code instead of
        /// interpreting.
        /// </summary>
        internal void Compile(object state) {
            if (_compiled != null) {
                return;
            }

            // Compilation is expensive, we only want to do it once.
            lock (_compileLock) {
                if (_compiled != null) {
                    return;
                }

                PerfTrack.NoteEvent(PerfTrack.Categories.Compiler, "Interpreted lambda compiled");
                
                // Interpreter needs a standard delegate type.
                // So change the lambda's delegate type to Func<...> or
                // Action<...> so it can be called from the LightLambda.Run
                // methods.
                LambdaExpression lambda = (_lambda as LambdaExpression) ?? (LambdaExpression)((LightLambdaExpression)_lambda).Reduce();
                if (Interpreter != null) {
                    _compiledDelegateType = GetFuncOrAction(lambda);
                    lambda = Expression.Lambda(_compiledDelegateType, lambda.Body, lambda.Name, lambda.Parameters);
                }

                if (HasClosure) {
                    _compiled = LightLambdaClosureVisitor.BindLambda(lambda, Interpreter.ClosureVariables);
                } else {
                    _compiled = lambda.Compile();
                }
            }
        }

        private static Type GetFuncOrAction(LambdaExpression lambda) {
            Type delegateType;
            bool isVoid = lambda.ReturnType == typeof(void);

            if (isVoid && lambda.Parameters.Count == 2 &&
                lambda.Parameters[0].IsByRef && lambda.Parameters[1].IsByRef) {
                return typeof(ActionRef<,>).MakeGenericType(lambda.Parameters.Map(p => p.Type));
            }

            Type[] types = lambda.Parameters.Map(p => p.IsByRef ? p.Type.MakeByRefType() : p.Type);
            if (isVoid) {
                if (Expression.TryGetActionType(types, out delegateType)) {
                    return delegateType;
                }
            } else {
                types = types.AddLast(lambda.ReturnType);
                if (Expression.TryGetFuncType(types, out delegateType)) {
                    return delegateType;
                }
            }

            return lambda.Type;
        }
    }
}
