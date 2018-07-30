// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls {

    /// <summary>
    /// MethodCandidate represents the different possible ways of calling a method or a set of method overloads.
    /// A single method can result in multiple MethodCandidates. Some reasons include:
    /// - Every optional parameter or parameter with a default value will result in a candidate
    /// - The presence of ref and out parameters will add a candidate for languages which want to return the updated values as return values.
    /// - ArgumentKind.List and ArgumentKind.Dictionary can result in a new candidate per invocation since the list might be different every time.
    ///
    /// Each MethodCandidate represents the parameter type for the candidate using ParameterWrapper.
    /// </summary>
    public sealed class MethodCandidate {
        private readonly List<ParameterWrapper> _parameters;
        private readonly ParameterWrapper _paramsDict;
        private readonly InstanceBuilder _instanceBuilder;

        internal MethodCandidate(OverloadResolver resolver, OverloadInfo method, List<ParameterWrapper> parameters, ParameterWrapper paramsDict,
            ReturnBuilder returnBuilder, InstanceBuilder instanceBuilder, IList<ArgBuilder> argBuilders, Dictionary<DynamicMetaObject, BindingRestrictions> restrictions) {

            Assert.NotNull(resolver, method, instanceBuilder, returnBuilder);
            Assert.NotNullItems(parameters);
            Assert.NotNullItems(argBuilders);

            Resolver = resolver;
            Overload = method;
            _instanceBuilder = instanceBuilder;
            ArgBuilders = argBuilders;
            ReturnBuilder = returnBuilder;
            _parameters = parameters;
            _paramsDict = paramsDict;
            Restrictions = restrictions;

            ParamsArrayIndex = ParameterWrapper.IndexOfParamsArray(parameters);

            parameters.TrimExcess();
        }

        internal MethodCandidate ReplaceMethod(OverloadInfo newMethod, List<ParameterWrapper> parameters, IList<ArgBuilder> argBuilders, Dictionary<DynamicMetaObject, BindingRestrictions> restrictions) {
            return new MethodCandidate(Resolver, newMethod, parameters, _paramsDict, ReturnBuilder, _instanceBuilder, argBuilders, restrictions);
        }

        internal ReturnBuilder ReturnBuilder { get; }
        internal IList<ArgBuilder> ArgBuilders { get; }
        public OverloadResolver Resolver { get; }

        [Obsolete("Use Overload instead")]
        public MethodBase Method => Overload.ReflectionInfo;

        public OverloadInfo Overload { get; }
        internal Dictionary<DynamicMetaObject, BindingRestrictions> Restrictions { get; }

        public Type ReturnType => ReturnBuilder.ReturnType;

        public int ParamsArrayIndex { get; }

        public bool HasParamsArray => ParamsArrayIndex != -1;

        public bool HasParamsDictionary => _paramsDict != null;

        public ActionBinder Binder => Resolver.Binder;

        internal ParameterWrapper GetParameter(int argumentIndex, ArgumentBinding namesBinding) {
            return _parameters[namesBinding.ArgumentToParameter(argumentIndex)];
        }

        internal ParameterWrapper GetParameter(int parameterIndex) {
            return _parameters[parameterIndex];
        }

        internal int ParameterCount => _parameters.Count;

        internal int IndexOfParameter(string name) {
            for (int i = 0; i < _parameters.Count; i++) {
                if (_parameters[i].Name == name) {
                    return i;
                }
            }
            return -1;
        }

        public int GetVisibleParameterCount() {
            int result = 0;
            foreach (var parameter in _parameters) {
                if (!parameter.IsHidden) {
                    result++;
                }
            }
            return result;
        }

        public IList<ParameterWrapper> GetParameters() {
            return new ReadOnlyCollection<ParameterWrapper>(_parameters);
        }

        /// <summary>
        /// Builds a new MethodCandidate which takes count arguments and the provided list of keyword arguments.
        /// 
        /// The basic idea here is to figure out which parameters map to params or a dictionary params and
        /// fill in those spots w/ extra ParameterWrapper's.  
        /// </summary>
        internal MethodCandidate MakeParamsExtended(int count, IList<string> names) {
            Debug.Assert(Overload.IsVariadic);

            List<ParameterWrapper> newParameters = new List<ParameterWrapper>(count);
            
            // keep track of which named args map to a real argument, and which ones
            // map to the params dictionary.
            List<string> unusedNames = new List<string>(names);
            List<int> unusedNameIndexes = new List<int>();
            for (int i = 0; i < unusedNames.Count; i++) {
                unusedNameIndexes.Add(i);
            }

            // if we don't have a param array we'll have a param dict which is type object
            ParameterWrapper paramsArrayParameter = null;
            int paramsArrayIndex = -1;

            for (int i = 0; i < _parameters.Count; i++) {
                ParameterWrapper parameter = _parameters[i];

                if (parameter.IsParamsArray) {
                    paramsArrayParameter = parameter;
                    paramsArrayIndex = i;
                } else {
                    int j = unusedNames.IndexOf(parameter.Name);
                    if (j != -1) {
                        unusedNames.RemoveAt(j);
                        unusedNameIndexes.RemoveAt(j);
                    }
                    newParameters.Add(parameter);
                }
            }

            if (paramsArrayIndex != -1) {
                ParameterWrapper expanded = paramsArrayParameter.Expand();
                while (newParameters.Count < (count - unusedNames.Count)) {
                    newParameters.Insert(System.Math.Min(paramsArrayIndex, newParameters.Count), expanded);
                }
            }

            if (_paramsDict != null) {
                var flags = (Overload.ProhibitsNullItems(_paramsDict.ParameterInfo.Position) ? ParameterBindingFlags.ProhibitNull : 0) |
                            (_paramsDict.IsHidden ? ParameterBindingFlags.IsHidden : 0);

                foreach (string name in unusedNames) {
                    newParameters.Add(new ParameterWrapper(_paramsDict.ParameterInfo, typeof(object), name, flags));
                }
            } else if (unusedNames.Count != 0) {
                // unbound kw args and no where to put them, can't call...
                // TODO: We could do better here because this results in an incorrect arg # error message.
                return null;
            }

            // if we have too many or too few args we also can't call
            if (count != newParameters.Count) {
                return null;
            }

            return MakeParamsExtended(unusedNames.ToArray(), unusedNameIndexes.ToArray(), newParameters);
        }

        private MethodCandidate MakeParamsExtended(string[] names, int[] nameIndices, List<ParameterWrapper> parameters) {
            Debug.Assert(Overload.IsVariadic);

            List<ArgBuilder> newArgBuilders = new List<ArgBuilder>(ArgBuilders.Count);

            // current argument that we consume, initially skip this if we have it.
            int curArg = Overload.IsStatic ? 0 : 1;
            int kwIndex = -1;
            ArgBuilder paramsDictBuilder = null;

            foreach (ArgBuilder ab in ArgBuilders) {
                // TODO: define a virtual method on ArgBuilder implementing this functionality:

                if (ab is SimpleArgBuilder sab) {
                    // we consume one or more incoming argument(s)
                    if (sab.IsParamsArray) {
                        // consume all the extra arguments
                        int paramsUsed = parameters.Count -
                            GetConsumedArguments() -
                            names.Length +
                            (Overload.IsStatic ? 1 : 0);

                        newArgBuilders.Add(new ParamsArgBuilder(
                            sab.ParameterInfo,
                            sab.Type.GetElementType(),
                            curArg,
                            paramsUsed
                        ));

                        curArg += paramsUsed;
                    } else if (sab.IsParamsDict) {
                        // consume all the kw arguments
                        kwIndex = newArgBuilders.Count;
                        paramsDictBuilder = sab;
                    } else {
                        // consume the argument, adjust its position:
                        newArgBuilders.Add(sab.MakeCopy(curArg++));
                    }
                } else if (ab is KeywordArgBuilder) {
                    newArgBuilders.Add(ab);
                    curArg++;
                } else {
                    // CodeContext, null, default, etc...  we don't consume an 
                    // actual incoming argument.
                    newArgBuilders.Add(ab);
                }
            }

            if (kwIndex != -1) {
                newArgBuilders.Insert(kwIndex, new ParamsDictArgBuilder(paramsDictBuilder.ParameterInfo, curArg, names, nameIndices));
            }

            return new MethodCandidate(Resolver, Overload, parameters, null, ReturnBuilder, _instanceBuilder, newArgBuilders, null);
        }

        private int GetConsumedArguments() {
            int consuming = 0;
            foreach (ArgBuilder argb in ArgBuilders) {
                if (argb is SimpleArgBuilder sab && !sab.IsParamsDict || argb is KeywordArgBuilder) {
                    consuming++;
                }
            }
            return consuming;
        }

        public Type[] GetParameterTypes() {
            List<Type> res = new List<Type>(ArgBuilders.Count);
            for (int i = 0; i < ArgBuilders.Count; i++) {
                Type t = ArgBuilders[i].Type;
                if (t != null) {
                    res.Add(t);
                }
            }

            return res.ToArray();
        }

        #region MakeExpression

        internal Expression MakeExpression(RestrictedArguments restrictedArgs) {
            Expression[] callArgs = GetArgumentExpressions(restrictedArgs, out bool[] usageMarkers, out Expression[] spilledArgs);

            Expression call;
            MethodBase mb = Overload.ReflectionInfo;

            // TODO: make MakeExpression virtual on OverloadInfo?
            if (mb == null) {
                throw new InvalidOperationException("Cannot generate an expression for an overload w/o MethodBase");
            }

            MethodInfo mi = mb as MethodInfo;
            if (mi != null) {
                Expression instance;
                if (mi.IsStatic) {
                    instance = null;
                } else {
                    Debug.Assert(mi != null);
                    instance = _instanceBuilder.ToExpression(ref mi, Resolver, restrictedArgs, usageMarkers);
                    Debug.Assert(instance != null, "Can't skip instance expression");
                }

                if (CompilerHelpers.IsVisible(mi)) {
                    call = AstUtils.SimpleCallHelper(instance, mi, callArgs);
                } else {
                    call = Expression.Call(
                        typeof(BinderOps).GetMethod("InvokeMethod"),
                        AstUtils.Constant(mi),
                        instance != null ? AstUtils.Convert(instance, typeof(object)) : AstUtils.Constant(null),
                        AstUtils.NewArrayHelper(typeof(object), callArgs)
                    );
                }
            } else {
                ConstructorInfo ci = (ConstructorInfo)mb;
                if (CompilerHelpers.IsVisible(ci)) {
                    call = AstUtils.SimpleNewHelper(ci, callArgs);
                } else {
                    call = Expression.Call(
                        typeof(BinderOps).GetMethod("InvokeConstructor"),
                        AstUtils.Constant(ci),
                        AstUtils.NewArrayHelper(typeof(object), callArgs)
                    );
                }
            }

            if (spilledArgs != null) {
                call = Expression.Block(spilledArgs.AddLast(call));
            }

            Expression ret = ReturnBuilder.ToExpression(Resolver, ArgBuilders, restrictedArgs, call);

            List<Expression> updates = null;
            for (int i = 0; i < ArgBuilders.Count; i++) {
                Expression next = ArgBuilders[i].UpdateFromReturn(Resolver, restrictedArgs);
                if (next != null) {
                    if (updates == null) {
                        updates = new List<Expression>();
                    }
                    updates.Add(next);
                }
            }

            if (updates != null) {
                if (ret.Type != typeof(void)) {
                    ParameterExpression temp = Expression.Variable(ret.Type, "$ret");
                    updates.Insert(0, Expression.Assign(temp, ret));
                    updates.Add(temp);
                    ret = Expression.Block(new[] { temp }, updates.ToArray());
                } else {
                    updates.Insert(0, ret);
                    ret = Expression.Block(typeof(void), updates.ToArray());
                }
            }

            if (Resolver.Temps != null) {
                ret = Expression.Block(Resolver.Temps, ret);
            }

            return ret;
        }

        private Expression[] GetArgumentExpressions(RestrictedArguments restrictedArgs, out bool[] usageMarkers, out Expression[] spilledArgs) {
            int minPriority = int.MaxValue;
            int maxPriority = int.MinValue;
            foreach (ArgBuilder ab in ArgBuilders) {
                minPriority = Math.Min(minPriority, ab.Priority);
                maxPriority = Math.Max(maxPriority, ab.Priority);
            }

            var args = new Expression[ArgBuilders.Count];
            Expression[] actualArgs = null;
            usageMarkers = new bool[restrictedArgs.Length];
            for (int priority = minPriority; priority <= maxPriority; priority++) {
                for (int i = 0; i < ArgBuilders.Count; i++) {
                    if (ArgBuilders[i].Priority == priority) {
                        args[i] = ArgBuilders[i].ToExpression(Resolver, restrictedArgs, usageMarkers);

                        // see if this has a temp that needs to be passed as the actual argument
                        Expression byref = ArgBuilders[i].ByRefArgument;
                        if (byref != null) {
                            if (actualArgs == null) {
                                actualArgs = new Expression[ArgBuilders.Count];
                            }
                            actualArgs[i] = byref;
                        }
                    }
                }
            }

            if (actualArgs != null) {
                for (int i = 0; i < args.Length; i++) {
                    if (args[i] != null && actualArgs[i] == null) {
                        actualArgs[i] = Resolver.GetTemporary(args[i].Type, null);
                        args[i] = Expression.Assign(actualArgs[i], args[i]);
                    }
                }

                spilledArgs = RemoveNulls(args);
                return RemoveNulls(actualArgs);
            }

            spilledArgs = null;
            return RemoveNulls(args);
        }

        private static Expression[] RemoveNulls(Expression[] args) {
            int newLength = args.Length;
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == null) {
                    newLength--;
                }
            }

            var result = new Expression[newLength];
            for (int i = 0, j = 0; i < args.Length; i++) {
                if (args[i] != null) {
                    result[j++] = args[i];
                }
            }
            return result;
        }

        #endregion

        public override string ToString() {
            return $"MethodCandidate({Overload.ReflectionInfo} on {Overload.DeclaringType.FullName})";
        }
    }
}
