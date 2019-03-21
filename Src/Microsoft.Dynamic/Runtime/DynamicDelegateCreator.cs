// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using System.Reflection;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Provides support for converting objects to delegates using the DLR binders
    /// available by the provided language context.
    /// 
    /// Primarily this supports converting objects implementing IDynamicMetaObjectProvider
    /// to the appropriate delegate type.  
    /// 
    /// If the provided object is already a delegate of the appropriate type then the 
    /// delegate will simply be returned.
    /// </summary>
    public class DynamicDelegateCreator {
        private readonly LanguageContext _languageContext;

        public DynamicDelegateCreator(LanguageContext languageContext) {
            ContractUtils.RequiresNotNull(languageContext, nameof(languageContext));

            _languageContext = languageContext;
        }

        /// <summary>
        /// Creates a delegate with a given signature that could be used to invoke this object from non-dynamic code (w/o code context).
        /// A stub is created that makes appropriate conversions/boxing and calls the object.
        /// The stub should be executed within a context of this object's language.
        /// </summary>
        /// <returns>The converted delegate.</returns>
        /// <exception cref="T:Microsoft.Scripting.ArgumentTypeException">The object is either a subclass of Delegate but not the requested type or does not implement IDynamicMetaObjectProvider.</exception>
        public Delegate GetDelegate(object callableObject, Type delegateType) {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));

            if (callableObject is Delegate result) {
                if (!delegateType.IsInstanceOfType(result)) {
                    throw ScriptingRuntimeHelpers.SimpleTypeError($"Cannot cast {result.GetType()} to {delegateType}.");
                }

                return result;
            }

            if (callableObject is IDynamicMetaObjectProvider dynamicObject) {

                MethodInfo invoke;

                if (!typeof(Delegate).IsAssignableFrom(delegateType) || (invoke = delegateType.GetMethod("Invoke")) == null) {
                    throw ScriptingRuntimeHelpers.SimpleTypeError("A specific delegate type is required.");
                }

                result = GetOrCreateDelegateForDynamicObject(callableObject, delegateType, invoke);
                if (result != null) {
                    return result;
                }
            }

            throw ScriptingRuntimeHelpers.SimpleTypeError("Object is not callable.");
        }

#if FEATURE_LCG
        // Table of dynamically generated delegates which are shared based upon method signature. 
        //
        // We generate a dynamic method stub and object[] closure template for each signature.
        // The stub does only depend on the signature, it doesn't depend on the dynamic object.
        // So we can reuse these stubs among multiple dynamic object for which a delegate was created with the same signature.
        // 
        private Publisher<DelegateSignatureInfo, DelegateInfo> _dynamicDelegateCache = new Publisher<DelegateSignatureInfo, DelegateInfo>();

        public Delegate GetOrCreateDelegateForDynamicObject(object dynamicObject, Type delegateType, MethodInfo invoke) {
            var signatureInfo = new DelegateSignatureInfo(invoke);
            DelegateInfo delegateInfo = _dynamicDelegateCache.GetOrCreateValue(
                signatureInfo, 
                () => new DelegateInfo(_languageContext, signatureInfo.ReturnType, signatureInfo.ParameterTypes)
            );

            return delegateInfo.CreateDelegate(delegateType, dynamicObject);
        }
#else
        //
        // Using Expression Trees we create a new stub for every dynamic object and every delegate type.
        // This is less efficient than with LCG since we can't reuse generated code for multiple dynamic objects and signatures.
        //
        private static ConditionalWeakTable<object, Dictionary<Type, Delegate>> _dynamicDelegateCache =
            new ConditionalWeakTable<object, Dictionary<Type, Delegate>>();

        private Delegate GetOrCreateDelegateForDynamicObject(object dynamicObject, Type delegateType, MethodInfo invoke) {
            var signatures = _dynamicDelegateCache.GetOrCreateValue(dynamicObject);
            lock (signatures) {
                Delegate result;
                if (!signatures.TryGetValue(delegateType, out result)) {
                    result = DelegateInfo.CreateDelegateForDynamicObject(_languageContext, dynamicObject, delegateType, invoke);
                    signatures.Add(delegateType, result);
                }

                return result;
            }
        }
#endif
    }
}
