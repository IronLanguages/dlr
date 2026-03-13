// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Generation;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    using Ast = Expression;
    
    public class InstanceBuilder {
        // Index of actual argument expression or -1 if the instance is null.
        private readonly int _index;

        public InstanceBuilder(int index) {
            Debug.Assert(index >= -1);
            _index = index;
        }

        public virtual bool HasValue => _index != -1;

        /// <summary>
        /// The number of actual arguments consumed by this builder.
        /// </summary>
        public virtual int ConsumedArgumentCount => 1;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")] // TODO
        protected internal virtual Expression ToExpression(ref MethodInfo method, OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) {
            if (_index == -1) {
                return AstUtils.Constant(null);
            }

            Debug.Assert(hasBeenUsed.Length == args.Length);
            Debug.Assert(_index < args.Length);
            Debug.Assert(!hasBeenUsed[_index]);
            hasBeenUsed[_index] = true;

            GetCallableMethod(args, ref method);
            return resolver.Convert(args.GetObject(_index), args.GetType(_index), null, method.DeclaringType);
        }

        private void GetCallableMethod(RestrictedArguments args, ref MethodInfo method) {
            // If we have a non-visible method see if we can find a better method which
            // will call the same thing but is visible. If this fails we still bind anyway - it's
            // the callers responsibility to filter out non-visible methods.
            //
            // We use limit type of the meta instance so that we can access methods inherited to that type 
            // as accessible via an interface implemented by the type. The type might be internal and the methods 
            // might not be accessible otherwise.
            method = CompilerHelpers.TryGetCallableMethod(args.GetObject(_index).LimitType, method);
        }
    }
}
