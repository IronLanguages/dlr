// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Provides language specific facilities which are typically called by the runtime.
    /// </summary>
    public abstract class LanguageContext {
        private DynamicOperations _operations;

        protected LanguageContext(ScriptDomainManager domainManager) {
            ContractUtils.RequiresNotNull(domainManager, nameof(domainManager));

            DomainManager = domainManager;
            ContextId = domainManager.GenerateContextId();
        }
       
        /// <summary>
        /// Provides the ContextId which includes members that should only be shown for this LanguageContext.
        /// 
        /// ContextId's are used for filtering by Scope's.
        /// </summary>
        public ContextId ContextId { get; }

        /// <summary>
        /// Gets the ScriptDomainManager that this LanguageContext is running within.
        /// </summary>
        public ScriptDomainManager DomainManager { get; }

        /// <summary>
        /// Whether the language can parse code and create source units.
        /// </summary>
        public virtual bool CanCreateSourceCode => true;

        #region Scope

        /// <summary>
        /// Gets the scope associated with a file, or null if none was available
        /// </summary>
        public virtual Scope GetScope(string path) {
            return null;
        }

        /// <summary>
        /// Creates a new ScriptScope using the default storage container
        /// </summary>
        public virtual Scope CreateScope() {
            return new Scope();
        }
                
        /// <summary>
        /// Creates a new ScriptScope whose storage contains the provided dictionary of objects
        /// 
        /// Accesses to the ScriptScope will turn into get,set, and delete members against this dictionary
        /// </summary>
        public virtual Scope CreateScope(IDictionary<string, object> dictionary) {
            return new Scope(dictionary);
        }
        
        /// <summary>
        /// Creates a new ScriptScope whose storage is an arbitrary object.
        /// 
        /// Accesses to the ScriptScope will turn into get, set, and delete members against the object.
        /// </summary>
        public virtual Scope CreateScope(IDynamicMetaObjectProvider storage) {
            return new Scope(storage);
        }

        // TODO: remove
        public ScopeExtension EnsureScopeExtension(Scope scope) {
            ContractUtils.RequiresNotNull(scope, nameof(scope));
            ScopeExtension extension = scope.GetExtension(ContextId);

            if (extension != null)
                return extension;

            extension = CreateScopeExtension(scope);
            if (extension == null) {
                throw Error.MustReturnScopeExtension();
            }

            return scope.SetExtension(ContextId, extension);
        }

        // TODO: remove
        public virtual ScopeExtension CreateScopeExtension(Scope scope) {
            return new ScopeExtension(scope);
        }

        /// <summary>
        /// Provides access to setting variables in scopes.  
        /// 
        /// By default this goes through ObjectOperations which can be rather slow.  
        /// Languages can override this to provide fast customized access which avoids 
        /// ObjectOperations.  Languages can provide fast access to commonly used scope 
        /// types for that language.  Typically this includes ScopeStorage and any other 
        /// classes which the language themselves uses for backing of a Scope.
        /// </summary>
        public virtual void ScopeSetVariable(Scope scope, string name, object value) {
            Operations.SetMember(scope, name, value);
        }

        /// <summary>
        /// Provides access to try getting variables in scopes.  
        /// 
        /// By default this goes through ObjectOperations which can be rather slow.  
        /// Languages can override this to provide fast customized access which avoids 
        /// ObjectOperations.  Languages can provide fast access to commonly used scope 
        /// types for that language.  Typically this includes ScopeStorage and any other 
        /// classes which the language themselves uses for backing of a Scope.
        /// </summary>
        public virtual bool ScopeTryGetVariable(Scope scope, string name, out dynamic value) {
            return Operations.TryGetMember(scope, name, out value);
        }

        /// <summary>
        /// Provides access to getting variables in scopes and converting the result.
        /// 
        /// By default this goes through ObjectOperations which can be rather slow.  
        /// Languages can override this to provide fast customized access which avoids 
        /// ObjectOperations.  Languages can provide fast access to commonly used scope 
        /// types for that language.  Typically this includes ScopeStorage and any other 
        /// classes which the language themselves uses for backing of a Scope.
        /// </summary>
        public virtual T ScopeGetVariable<T>(Scope scope, string name) {
            return Operations.GetMember<T>(scope, name);
        }

        /// <summary>
        /// Provides access to getting variables in scopes.
        /// 
        /// By default this goes through ObjectOperations which can be rather slow.  
        /// Languages can override this to provide fast customized access which avoids 
        /// ObjectOperations.  Languages can provide fast access to commonly used scope 
        /// types for that language.  Typically this includes ScopeStorage and any other 
        /// classes which the language themselves uses for backing of a Scope.
        /// </summary>
        public virtual dynamic ScopeGetVariable(Scope scope, string name) {
            return Operations.GetMember(scope, name);
        }

        #endregion

        #region Source Code Parsing & Compilation

        /// <summary>
        /// Provides a text reader for source code that is to be read from a given stream.
        /// </summary>
        /// <param name="stream">The stream open for reading. The stream must also allow seeking.</param>
        /// <param name="defaultEncoding">An encoding that should be used if the stream doesn't have Unicode or language specific preamble.</param>
        /// <param name="path">the path of the source unit if available</param>
        /// <returns>The reader.</returns>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public virtual SourceCodeReader GetSourceReader(Stream stream, Encoding defaultEncoding, string path) {
            ContractUtils.RequiresNotNull(stream, nameof(stream));
            ContractUtils.RequiresNotNull(defaultEncoding, nameof(defaultEncoding));
            ContractUtils.Requires(stream.CanRead && stream.CanSeek, nameof(stream), "The stream must support reading and seeking");

            var result = new StreamReader(stream, defaultEncoding, true);
            result.Peek();
            return new SourceCodeReader(result, result.CurrentEncoding);
        }

        /// <summary>
        /// Creates the language specific CompilerOptions object for compilation of code not bound to any particular scope.
        /// The language should flow any relevant options from LanguageContext to the newly created options instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual CompilerOptions GetCompilerOptions() {
            return new CompilerOptions();
        }

        /// <summary>
        /// Creates the language specific CompilerOptions object for compilation of code bound to a given scope.
        /// </summary>
        public virtual CompilerOptions GetCompilerOptions(Scope scope) {
            return GetCompilerOptions();
        }

        /// <summary>
        /// Parses the source code within a specified compiler context. 
        /// The source unit to parse is held on by the context.
        /// </summary>
        /// <returns><b>null</b> on failure.</returns>
        /// <remarks>Could also set the code properties and line/file mappings on the source unit.</remarks>
        public abstract ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink);

        public virtual ScriptCode LoadCompiledCode(Delegate method, string path, string customData) {
            throw new NotSupportedException();
        }

        public virtual int ExecuteProgram(SourceUnit program) {
            ContractUtils.RequiresNotNull(program, nameof(program));

            object returnValue = program.Execute();

            if (returnValue == null) {
                return 0;
            }

            CallSite<Func<CallSite, object, int>> site =
                CallSite<Func<CallSite, object, int>>.Create(CreateConvertBinder(typeof(int), true));

            return site.Target(site, returnValue);
        }

        #endregion

        #region ScriptEngine API

        public virtual Version LanguageVersion => new Version(0, 0);

        public virtual void SetSearchPaths(ICollection<string> paths) {
            throw new NotSupportedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual ICollection<string> GetSearchPaths() {
            return Options.SearchPaths;
        }

#if FEATURE_CODEDOM
        // Convert a CodeDom to source code, and output the generated code and the line number mappings (if any)
        public virtual SourceUnit GenerateSourceCode(System.CodeDom.CodeObject codeDom, string path, SourceCodeKind kind) {
            throw new NotImplementedException();
        }
#endif

        public virtual TService GetService<TService>(params object[] args) where TService : class {
            return null;
        }

        public virtual Guid LanguageGuid => Guid.Empty;

        public virtual Guid VendorGuid => Guid.Empty;

        public virtual void Shutdown() {
        }

        public virtual string FormatException(Exception exception) {
            return exception.ToString();
        }

        public virtual IList<DynamicStackFrame> GetStackFrames(Exception exception) {
            return new DynamicStackFrame[0];
        }
        
        public virtual LanguageOptions Options => new LanguageOptions();

        #region Source Units

        public SourceUnit CreateSnippet(string code, SourceCodeKind kind) {
            return CreateSnippet(code, null, kind);
        }

        public SourceUnit CreateSnippet(string code, string id, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(code, nameof(code));

            return CreateSourceUnit(new SourceStringContentProvider(code), id, kind);
        }

        public SourceUnit CreateFileUnit(string path) {
            return CreateFileUnit(path, StringUtils.DefaultEncoding);
        }

        public SourceUnit CreateFileUnit(string path, Encoding encoding) {
            return CreateFileUnit(path, encoding, SourceCodeKind.File);
        }

        public SourceUnit CreateFileUnit(string path, Encoding encoding, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(path, nameof(path));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));

            TextContentProvider provider = new LanguageBoundTextContentProvider(this, new FileStreamContentProvider(DomainManager.Platform, path), encoding, path);
            return CreateSourceUnit(provider, path, kind);
        }

        public SourceUnit CreateFileUnit(string path, string content) {
            ContractUtils.RequiresNotNull(path, nameof(path));
            ContractUtils.RequiresNotNull(content, nameof(content));

            TextContentProvider provider = new SourceStringContentProvider(content);
            return CreateSourceUnit(provider, path, SourceCodeKind.File);
        }

        public SourceUnit CreateSourceUnit(StreamContentProvider contentProvider, string path, Encoding encoding, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(contentProvider, nameof(contentProvider));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));
            ContractUtils.Requires(kind.IsValid(), nameof(kind));
            ContractUtils.Requires(CanCreateSourceCode);

            return new SourceUnit(this, new LanguageBoundTextContentProvider(this, contentProvider, encoding, path), path, kind);
        }

        public SourceUnit CreateSourceUnit(TextContentProvider contentProvider, string path, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(contentProvider, nameof(contentProvider));
            ContractUtils.Requires(kind.IsValid(), nameof(kind));
            ContractUtils.Requires(CanCreateSourceCode);

            return new SourceUnit(this, contentProvider, path, kind);
        }
        
        #endregion

        
        #endregion
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual ErrorSink GetCompilerErrorSink() {
            return ErrorSink.Null;
        }

        
        #region Object Operations Support

        internal static DynamicMetaObject ErrorMetaObject(Type resultType, DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) {
            return errorSuggestion ?? new DynamicMetaObject(
                Expression.Throw(Expression.New(typeof(NotImplementedException)), resultType),
                target.Restrictions.Merge(BindingRestrictions.Combine(args))
            );
        }

        public virtual UnaryOperationBinder CreateUnaryOperationBinder(ExpressionType operation) {
            return new DefaultUnaryOperationBinder(operation);
        }

        private sealed class DefaultUnaryOperationBinder : UnaryOperationBinder {
            internal DefaultUnaryOperationBinder(ExpressionType operation)
                : base(operation) {
            }

            public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion) {
                return ErrorMetaObject(ReturnType, target, new[] { target }, errorSuggestion);
            }
        }

        public virtual BinaryOperationBinder CreateBinaryOperationBinder(ExpressionType operation) {
            return new DefaultBinaryOperationBinder(operation);
        }

        private sealed class DefaultBinaryOperationBinder : BinaryOperationBinder {
            internal DefaultBinaryOperationBinder(ExpressionType operation)
                : base(operation) {
            }

            public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion) {
                return ErrorMetaObject(ReturnType, target, new[] { target, arg }, errorSuggestion);
            }
        }

        private class DefaultConvertAction : ConvertBinder {
            internal DefaultConvertAction(Type type, bool @explicit)
                : base(type, @explicit) {
            }

            public override DynamicMetaObject FallbackConvert(DynamicMetaObject self, DynamicMetaObject errorSuggestion) {
                if (Type.IsAssignableFrom(self.LimitType)) {
                    return new DynamicMetaObject(
                        Expression.Convert(self.Expression, Type),
                        BindingRestrictions.GetTypeRestriction(self.Expression, self.LimitType)
                    );
                }

                if (errorSuggestion != null) {
                    return errorSuggestion;
                }

                return new DynamicMetaObject(
                    Expression.Throw(
                        Expression.Constant(
                            new ArgumentTypeException($"Expected {Type.FullName}, got {self.LimitType.FullName}")
                        ),
                        ReturnType
                    ),
                    BindingRestrictions.GetTypeRestriction(self.Expression, self.LimitType)
                );
            }
        }

        /// <summary>
        /// Creates a conversion binder.
        /// 
        /// If explicitCast is true then the binder should do explicit conversions.
        /// If explicitCast is false then the binder should do implicit conversions.
        /// 
        /// If explicitCast is null it is up to the language to select the conversions
        /// which closest match their normal behavior.
        /// </summary>
        public virtual ConvertBinder CreateConvertBinder(Type toType, bool? explicitCast) {
            return new DefaultConvertAction(toType, explicitCast ?? false);
        }

        private class DefaultGetMemberAction : GetMemberBinder {
            internal DefaultGetMemberAction(string name, bool ignoreCase)
                : base(name, ignoreCase) {
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject self, DynamicMetaObject errorSuggestion) {
                return errorSuggestion ?? new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(
                            typeof(MissingMemberException).GetConstructor(new[] { typeof(string) }),
                            Expression.Constant($"unknown member: {Name}")
                        ),
                        typeof(object)
                    ),
                    self.Value == null ?
                        BindingRestrictions.GetExpressionRestriction(Expression.Equal(self.Expression, Expression.Constant(null))) :
                        BindingRestrictions.GetTypeRestriction(self.Expression, self.Value.GetType())
                );
            }
        }

        public virtual GetMemberBinder CreateGetMemberBinder(string name, bool ignoreCase) {
            return new DefaultGetMemberAction(name, ignoreCase);
        }

        private class DefaultSetMemberAction : SetMemberBinder {
            internal DefaultSetMemberAction(string name, bool ignoreCase)
                : base(name, ignoreCase) {
            }

            public override DynamicMetaObject FallbackSetMember(DynamicMetaObject self, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
                return ErrorMetaObject(ReturnType, self, new DynamicMetaObject[] { value }, errorSuggestion);
            }
        }

        public virtual SetMemberBinder CreateSetMemberBinder(string name, bool ignoreCase) {
            return new DefaultSetMemberAction(name, ignoreCase);
        }

        private class DefaultDeleteMemberAction : DeleteMemberBinder {
            internal DefaultDeleteMemberAction(string name, bool ignoreCase)
                : base(name, ignoreCase) {
            }

            public override DynamicMetaObject FallbackDeleteMember(DynamicMetaObject self, DynamicMetaObject errorSuggestion) {
                return ErrorMetaObject(ReturnType, self, DynamicMetaObject.EmptyMetaObjects, errorSuggestion);
            }
        }

        public virtual DeleteMemberBinder CreateDeleteMemberBinder(string name, bool ignoreCase) {
            return new DefaultDeleteMemberAction(name, ignoreCase);
        }

        private class DefaultCallAction : InvokeMemberBinder {
            private readonly LanguageContext _context;

            internal DefaultCallAction(LanguageContext context, string name, bool ignoreCase, CallInfo callInfo)
                : base(name, ignoreCase, callInfo) {
                _context = context;
            }

            public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) {
                return ErrorMetaObject(ReturnType, target, args.AddFirst(target), errorSuggestion);
            }

            private static Expression[] GetArgs(DynamicMetaObject target, DynamicMetaObject[] args) {
                Expression[] res = new Expression[args.Length + 1];
                res[0] = target.Expression;
                for (int i = 0; i < args.Length; i++) {
                    res[1 + i] = args[i].Expression;
                }

                return res;
            }

            public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) {
                return new DynamicMetaObject(
                    DynamicExpression.Dynamic(
                        _context.CreateInvokeBinder(CallInfo),
                        typeof(object),
                        GetArgs(target, args)
                    ),
                    target.Restrictions.Merge(BindingRestrictions.Combine(args))
                );
            }
        }

        public virtual InvokeMemberBinder CreateCallBinder(string name, bool ignoreCase, CallInfo callInfo) {
            return new DefaultCallAction(this, name, ignoreCase, callInfo);
        }

        private class DefaultInvokeAction : InvokeBinder {
            internal DefaultInvokeAction(CallInfo callInfo)
                : base(callInfo) {
            }

            public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) {
                return ErrorMetaObject(ReturnType, target, args, errorSuggestion);
            }
        }

        public virtual InvokeBinder CreateInvokeBinder(CallInfo callInfo) {
            return new DefaultInvokeAction(callInfo);
        }

        private class DefaultCreateAction : CreateInstanceBinder {
            internal DefaultCreateAction(CallInfo callInfo)
                : base(callInfo) {
            }

            public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) {
                return ErrorMetaObject(ReturnType, target, args, errorSuggestion);
            }
        }

        public virtual CreateInstanceBinder CreateCreateBinder(CallInfo callInfo) {
            return new DefaultCreateAction(callInfo);
        }

        public DynamicOperations Operations {
            get {
                if (_operations == null) {
                    Interlocked.CompareExchange(ref _operations, new DynamicOperations(this), null);
                }

                return _operations;
            }
        }
        #endregion

        #region Object Introspection Support

        /// <summary>
        /// Gets the member names associated with the object
        /// By default, only returns IDO names
        /// </summary>
        public virtual IList<string> GetMemberNames(object obj) {
            if (obj is IDynamicMetaObjectProvider ido) {
                var mo = ido.GetMetaObject(Expression.Parameter(typeof(object), null));
                return mo.GetDynamicMemberNames().ToReadOnly();
            }
            return EmptyArray<string>.Instance;
        }

        public virtual string GetDocumentation(object obj) {
            return String.Empty;
        }

        public virtual IList<string> GetCallSignatures(object obj) {
            return new string[0];
        }

        public virtual bool IsCallable(object obj) => obj is Delegate;

        #endregion

        #region Object formatting

        /// <summary>
        /// Returns a string representation of the object in a language specific object display format.
        /// </summary>
        /// <param name="operations">Dynamic sites container that could be used for any dynamic dispatches necessary for formatting.</param>
        /// <param name="obj">Object to format.</param>
        /// <returns>A string representation of object.</returns>
        public virtual string FormatObject(DynamicOperations operations, object obj) {
            return obj == null ? "null" : obj.ToString();
        }

        public virtual void GetExceptionMessage(Exception exception, out string message, out string errorTypeName) {
            message = exception.Message;
            errorTypeName = exception.GetType().Name;
        }

        #endregion
    }
}
