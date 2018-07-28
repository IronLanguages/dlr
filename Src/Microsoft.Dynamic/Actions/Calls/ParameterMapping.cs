// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    public sealed class ParameterMapping {
        private readonly OverloadResolver _resolver;
        private readonly IList<string> _argNames;

        private readonly List<ParameterWrapper> _parameters;
        private readonly List<ArgBuilder> _arguments;
        private List<int> _returnArgs;
        private InstanceBuilder _instanceBuilder;
        private ReturnBuilder _returnBuilder;

        private readonly List<ArgBuilder> _defaultArguments;
        private bool _hasByRef;
        private bool _hasDefaults;
        private ParameterWrapper _paramsDict;

        public OverloadInfo Overload { get; }

        /// <summary>
        /// Gets the next argument to consume.
        /// </summary>
        public int ArgIndex { get; private set; }

        [Obsolete("Use Overload.ReflectionInfo instead")]
        public MethodBase Method => Overload.ReflectionInfo;

        [Obsolete("Use Overload.Parameters instead")]
        public ParameterInfo[] ParameterInfos => ArrayUtils.MakeArray(Overload.Parameters);

        internal ParameterMapping(OverloadResolver resolver, OverloadInfo method, IList<string> argNames) {
            Assert.NotNull(resolver, method);
            _resolver = resolver;
            Overload = method;
            _argNames = argNames;
            _parameters = new List<ParameterWrapper>();
            _arguments = new List<ArgBuilder>(method.ParameterCount);
            _defaultArguments = new List<ArgBuilder>();
	    }

        internal void MapParameters(bool reduceByRef) {
            if (reduceByRef) {
                _returnArgs = new List<int>();
                if (Overload.ReturnType != typeof(void)) {
                    _returnArgs.Add(-1);
                }
            }

            BitArray specialParameters = _resolver.MapSpecialParameters(this);

            if (_instanceBuilder == null) {
                _instanceBuilder = new InstanceBuilder(-1);
            }

            foreach (var parameter in Overload.Parameters) {
                if (!IsSpecialParameter(specialParameters, parameter.Position)) {
                    if (reduceByRef) {
                        MapParameterReduceByRef(parameter);
                    } else {
                        MapParameter(parameter);
                    }
                }
            }

            _returnBuilder = MakeReturnBuilder(specialParameters);
        }

        private bool IsSpecialParameter(BitArray specialParameters, int infoIndex) {
            return specialParameters != null && infoIndex < specialParameters.Length && specialParameters[infoIndex];
        }

        public void AddInstanceBuilder(InstanceBuilder builder) {
            ContractUtils.Requires(_instanceBuilder == null);
            ContractUtils.Requires(builder.HasValue);
            _instanceBuilder = builder;
            ArgIndex += builder.ConsumedArgumentCount;
        }

        // TODO: We might want to add bitmap of all consumed arguments and allow to consume an arbitrary argument, not just the next one.
        public void AddBuilder(ArgBuilder builder) {
            ContractUtils.Requires(builder.ConsumedArgumentCount != ArgBuilder.AllArguments);

            _arguments.Add(builder);
            ArgIndex += builder.ConsumedArgumentCount;
        }

        public void AddParameter(ParameterWrapper parameter) {
            _parameters.Add(parameter);
        }

        public void MapParameter(ParameterInfo pi) {
            int indexForArgBuilder;
            int nameIndex = _argNames.IndexOf(pi.Name);
            if (nameIndex == -1) {
                // positional argument, we simply consume the next argument
                indexForArgBuilder = ArgIndex++;
            } else {
                // keyword argument, we just tell the simple arg builder to consume arg 0.
                // KeywordArgBuilder will then pass in the correct single argument based 
                // upon the actual argument number provided by the user.
                indexForArgBuilder = 0;
            }

            // if the parameter is default we need to build a default arg builder and then
            // build a reduced method at the end.  
            if (!pi.IsMandatory()) {
                // We need to build the default builder even if we have a parameter for it already to
                // get good consistency of our error messages.  But consider a method like 
                // def foo(a=1, b=2) and the user calls it as foo(b=3). Then adding the default
                // value breaks an otherwise valid call.  This is because we only generate MethodCandidates
                // filling in the defaults from right to left (so the method - 1 arg requires a,
                // and the method minus 2 args requires b).  So we only add the default if it's 
                // a positional arg or we don't already have a default value.
                if (nameIndex == -1 || !_hasDefaults) {
                    _defaultArguments.Add(new DefaultArgBuilder(pi));
                    _hasDefaults = true;
                } else {
                    _defaultArguments.Add(null);
                }
            } else if (_defaultArguments.Count > 0) {
                // non-contigious default parameter
                _defaultArguments.Add(null);
            }

            ArgBuilder ab;
            if (pi.ParameterType.IsByRef) {
                _hasByRef = true;
                Type elementType = pi.ParameterType.GetElementType();
                Type refType = typeof(StrongBox<>).MakeGenericType(elementType);
                _parameters.Add(new ParameterWrapper(pi, refType, pi.Name, ParameterBindingFlags.ProhibitNull));
                ab = new ReferenceArgBuilder(pi, elementType, refType, indexForArgBuilder);
            } else if (pi.Position == 0 && Overload.IsExtension) {
                _parameters.Add(new ParameterWrapper(pi, pi.ParameterType, pi.Name, ParameterBindingFlags.IsHidden));
                ab = new SimpleArgBuilder(pi, pi.ParameterType, indexForArgBuilder, false, false);
            } else {
                ab = AddSimpleParameterMapping(pi, indexForArgBuilder);
            }

            if (nameIndex == -1) {
                _arguments.Add(ab);
            } else {
                Debug.Assert(KeywordArgBuilder.BuilderExpectsSingleParameter(ab));
                _arguments.Add(new KeywordArgBuilder(ab, _argNames.Count, nameIndex));
            }
        }

        /// <summary>
        /// Maps out parameters to return args and ref parameters to ones that don't accept StrongBox.
        /// </summary>
        private void MapParameterReduceByRef(ParameterInfo pi) {
            Debug.Assert(_returnArgs != null);

            // TODO:
            // Is this reduction necessary? What if 
            // 1) we had an implicit conversion StrongBox<T> -> T& and 
            // 2) all out parameters were treated as optional StrongBox<T> parameters? (if not present we return the result in a return value)
            
            int indexForArgBuilder = 0;

            int nameIndex = -1;
            if (!pi.IsOutParameter()) {
                nameIndex = _argNames.IndexOf(pi.Name);
                if (nameIndex == -1) {
                    indexForArgBuilder = ArgIndex++;
                }
            }

            ArgBuilder ab;
            if (pi.IsOutParameter()) {
                _returnArgs.Add(_arguments.Count);
                ab = new OutArgBuilder(pi);
            } else if (pi.ParameterType.IsByRef) {
                // if the parameter is marked as [In] it is not returned.
                if ((pi.Attributes & (ParameterAttributes.In | ParameterAttributes.Out)) != ParameterAttributes.In) {
                    _returnArgs.Add(_arguments.Count);
                }
                _parameters.Add(new ParameterWrapper(pi, pi.ParameterType.GetElementType(), pi.Name, ParameterBindingFlags.None));
                ab = new ReturnReferenceArgBuilder(pi, indexForArgBuilder);
            } else {
                ab = AddSimpleParameterMapping(pi, indexForArgBuilder);
            }

            if (nameIndex == -1) {
                _arguments.Add(ab);
            } else {
                Debug.Assert(KeywordArgBuilder.BuilderExpectsSingleParameter(ab));
                _arguments.Add(new KeywordArgBuilder(ab, _argNames.Count, nameIndex));
            }
        }

        private ParameterWrapper CreateParameterWrapper(ParameterInfo info) {
            bool isParamArray = Overload.IsParamArray(info.Position);
            bool isParamDict = !isParamArray && Overload.IsParamDictionary(info.Position);
            bool prohibitsNullItems = (isParamArray || isParamDict) && Overload.ProhibitsNullItems(info.Position);

            return new ParameterWrapper(
                info,
                info.ParameterType,
                info.Name,
                (Overload.ProhibitsNull(info.Position) ? ParameterBindingFlags.ProhibitNull : 0) |
                (prohibitsNullItems ? ParameterBindingFlags.ProhibitNullItems : 0) |
                (isParamArray ? ParameterBindingFlags.IsParamArray : 0) |
                (isParamDict ? ParameterBindingFlags.IsParamDictionary : 0)
            );
        }

        private SimpleArgBuilder AddSimpleParameterMapping(ParameterInfo info, int index) {
            var param = CreateParameterWrapper(info);
            if (param.IsParamsDict) {
                _paramsDict = param;
            } else {
                _parameters.Add(param);
            }

            return new SimpleArgBuilder(info, info.ParameterType, index, param.IsParamsArray, param.IsParamsDict);
        }

        internal MethodCandidate CreateCandidate() {
            return new MethodCandidate(_resolver, Overload, _parameters, _paramsDict, _returnBuilder, _instanceBuilder, _arguments, null);
        }

        internal MethodCandidate CreateByRefReducedCandidate() {
            if (!_hasByRef) {
                return null;
            }

            var reducedMapping = new ParameterMapping(_resolver, Overload, _argNames);
            reducedMapping.MapParameters(true);
            return reducedMapping.CreateCandidate();
        }

        #region Candidates with Default Parameters

        internal IEnumerable<MethodCandidate> CreateDefaultCandidates() {
            if (!_hasDefaults) {
                yield break;
            }

            for (int defaultsUsed = 1; defaultsUsed < _defaultArguments.Count + 1; defaultsUsed++) {
                // if the left most default we'll use is not present then don't add a default.  This happens in cases such as:
                // a(a=1, b=2, c=3) and then call with a(a=5, c=3).  We'll come through once for c (no default, skip),
                // once for b (default present, emit) and then a (no default, skip again).  W/o skipping we'd generate the same
                // method multiple times.  This also happens w/ non-contigious default values, e.g. foo(a, b=3, c) where we don't want
                // to generate a default candidate for just c which matches the normal method.
                if (_defaultArguments[_defaultArguments.Count - defaultsUsed] != null) {
                    yield return CreateDefaultCandidate(defaultsUsed);
                }
            }
        }

        private MethodCandidate CreateDefaultCandidate(int defaultsUsed) {
            List<ArgBuilder> defaultArgBuilders = new List<ArgBuilder>(_arguments);
            List<ParameterWrapper> necessaryParams = _parameters.GetRange(0, _parameters.Count - defaultsUsed);

            for (int curDefault = 0; curDefault < defaultsUsed; curDefault++) {
                int readIndex = _defaultArguments.Count - defaultsUsed + curDefault;
                int writeIndex = defaultArgBuilders.Count - defaultsUsed + curDefault;

                if (_defaultArguments[readIndex] != null) {
                    defaultArgBuilders[writeIndex] = _defaultArguments[readIndex];
                } else {
                    necessaryParams.Add(_parameters[_parameters.Count - defaultsUsed + curDefault]);
                }
            }

            // shift any arguments forward that need to be...
            int curArg = Overload.IsStatic ? 0 : 1;
            for (int i = 0; i < defaultArgBuilders.Count; i++) {
                if (defaultArgBuilders[i] is SimpleArgBuilder sab) {
                    defaultArgBuilders[i] = sab.MakeCopy(curArg++);
                }
            }

            return new MethodCandidate(_resolver, Overload, necessaryParams, _paramsDict, _returnBuilder, _instanceBuilder, defaultArgBuilders, null);
        }

        #endregion

        #region ReturnBuilder, Member Assigned Arguments

        private ReturnBuilder MakeReturnBuilder(BitArray specialParameters) {
            ReturnBuilder returnBuilder = (_returnArgs != null) ?
                new ByRefReturnBuilder(_returnArgs) :
                new ReturnBuilder(Overload.ReturnType);
            
            if (_argNames.Count > 0 && _resolver.AllowMemberInitialization(Overload)) {
                List<string> unusedNames = GetUnusedArgNames(specialParameters);
                List<MemberInfo> bindableMembers = GetBindableMembers(returnBuilder.ReturnType, unusedNames);
                if (unusedNames.Count == bindableMembers.Count) {
                    List<int> nameIndices = new List<int>();

                    foreach (MemberInfo mi in bindableMembers) {
                        PropertyInfo pi = mi as PropertyInfo;
                        var type = (pi != null) ? pi.PropertyType : ((FieldInfo)mi).FieldType;
                        
                        _parameters.Add(new ParameterWrapper(null, type, mi.Name, ParameterBindingFlags.None));
                        nameIndices.Add(_argNames.IndexOf(mi.Name));
                    }

                    return new KeywordConstructorReturnBuilder(
                        returnBuilder,
                        _argNames.Count,
                        nameIndices.ToArray(),
                        bindableMembers.ToArray(),
                        _resolver.Binder.PrivateBinding
                    );
                }

            }
            return returnBuilder;
        }

        private static List<MemberInfo> GetBindableMembers(Type returnType, List<string> unusedNames) {
            List<MemberInfo> bindableMembers = new List<MemberInfo>();

            foreach (string name in unusedNames) {
                Type curType = returnType;
                MemberInfo[] mis = curType.GetInheritedMembers(name).ToArray();
                while (mis.Length != 1 && curType != null) {
                    // see if we have a single member defined as the closest level
                    mis = curType.GetDeclaredMembers(name).WithBindingFlags(BindingFlags.Public | BindingFlags.Instance).ToArray();

                    if (mis.Length > 1) {
                        break;
                    }

                    curType = curType.GetBaseType();
                }

                if (mis.Length == 1) {
                    if (mis[0] is PropertyInfo || mis[0] is FieldInfo) {
                        bindableMembers.Add(mis[0]);
                    }
                }
            }
            return bindableMembers;
        }

        private List<string> GetUnusedArgNames(BitArray specialParameters) {
            List<string> unusedNames = new List<string>();
            foreach (string name in _argNames) {
                bool found = false;
                foreach (ParameterInfo pi in Overload.Parameters) {
                    if (!IsSpecialParameter(specialParameters, pi.Position) && pi.Name == name) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    unusedNames.Add(name);
                }
            }
            return unusedNames;
        }

        #endregion
    }
}
