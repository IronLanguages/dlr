// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.Threading;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {

    /// <summary>
    /// Hosting API counterpart for <see cref="ScriptCode"/>.
    /// </summary>
    public sealed class CompiledCode : MarshalByRefObject {
        private ScriptScope _defaultScope;

        internal ScriptCode ScriptCode { get; }

        internal CompiledCode(ScriptEngine engine, ScriptCode code) {
            Assert.NotNull(engine);
            Assert.NotNull(code);

            Engine = engine;
            ScriptCode = code;
        }

        /// <summary>
        /// Engine that compiled this code.
        /// </summary>
        public ScriptEngine Engine { get; }

        /// <summary>
        /// Default scope for this code.
        /// </summary>
        public ScriptScope DefaultScope {
            get {
                if (_defaultScope == null) {
                    Interlocked.CompareExchange(ref _defaultScope, new ScriptScope(Engine, ScriptCode.CreateScope()), null);
                }
                return _defaultScope; 
            }
        }

        /// <summary>
        /// Executes code in a default scope.
        /// </summary>
        public dynamic Execute() {
            return ScriptCode.Run(DefaultScope.Scope);
        }

        /// <summary>
        /// Execute code within a given scope and returns the result.
        /// </summary>
        public dynamic Execute(ScriptScope scope) {
            ContractUtils.RequiresNotNull(scope, nameof(scope));
            return ScriptCode.Run(scope.Scope);
        }

        /// <summary>
        /// Executes code in in a default scope and converts to a given type.
        /// </summary>
        public T Execute<T>() {
            return Engine.Operations.ConvertTo<T>((object)Execute());
        }

        /// <summary>
        /// Execute code within a given scope and converts result to a given type.
        /// </summary>
        public T Execute<T>(ScriptScope scope) {
            return Engine.Operations.ConvertTo<T>((object)Execute(scope));
        }


#if FEATURE_REMOTING
        /// <summary>
        /// Executes the code in an empty scope.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// </summary>
        public ObjectHandle ExecuteAndWrap() {
            return new ObjectHandle((object)Execute());
        }

        /// <summary>
        /// Executes the code in the specified scope.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// </summary>
        public ObjectHandle ExecuteAndWrap(ScriptScope scope) {
            return new ObjectHandle((object)Execute(scope));
        }

        /// <summary>
        /// Executes the code in an empty scope.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// 
        /// If an exception is thrown the exception is caught and an ObjectHandle to
        /// the exception is provided.
        /// </summary>
        /// <remarks>
        /// Use this API to handle non-serializable exceptions (exceptions might not be serializable due to security restrictions) 
        /// or if an exception serialization loses information.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ObjectHandle ExecuteAndWrap(out ObjectHandle exception) {
            exception = null;
            try {
                return new ObjectHandle((object)Execute());
            } catch (Exception e) {
                exception = new ObjectHandle(e);
                return null;
            }
        }

        /// <summary>
        /// Executes the expression in the specified scope and return a result.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// 
        /// If an exception is thrown the exception is caught and an ObjectHandle to
        /// the exception is provided.
        /// </summary>
        /// <remarks>
        /// Use this API to handle non-serializable exceptions (exceptions might not be serializable due to security restrictions) 
        /// or if an exception serialization loses information.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ObjectHandle ExecuteAndWrap(ScriptScope scope, out ObjectHandle exception) {
            exception = null;
            try{
                return new ObjectHandle((object)Execute(scope));
            } catch (Exception e) {
                exception = new ObjectHandle(e);
                return null;
            }
        }

        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
    }
}
