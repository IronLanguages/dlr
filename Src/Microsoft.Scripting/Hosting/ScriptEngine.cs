// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Threading;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting {

    /// <summary>
    /// Represents a language in Hosting API. 
    /// Hosting API counterpart for <see cref="LanguageContext"/>.
    /// </summary>
    [DebuggerDisplay("{Setup.DisplayName}")]
    public sealed class ScriptEngine : MarshalByRefObject {
        private LanguageSetup _config;
        private ObjectOperations _operations;

        internal ScriptEngine(ScriptRuntime runtime, LanguageContext context) {
            Debug.Assert(runtime != null);
            Debug.Assert(context != null);

            Runtime = runtime;
            LanguageContext = context;
        }

        #region Object Operations

        /// <summary>
        /// Returns a default ObjectOperations for the engine.  
        /// 
        /// Because an ObjectOperations object caches rules for the types of 
        /// objects and operations it processes, using the default ObjectOperations for 
        /// many objects could degrade the caching benefits.  Eventually the cache for 
        /// some operations could degrade to a point where ObjectOperations stops caching and 
        /// does a full search for an implementation of the requested operation for the given objects.  
        /// 
        /// Another reason to create a new ObjectOperations instance is to have it bound
        /// to the specific view of a ScriptScope.  Languages may attach per-language
        /// behavior to a ScriptScope which would alter how the operations are performed.
        /// 
        /// For simple hosting situations, this is sufficient behavior.
        /// 
        /// 
        /// </summary>
        public ObjectOperations Operations {
            get {
                if (_operations == null) {
                    Interlocked.CompareExchange(ref _operations, CreateOperations(), null);
                }

                return _operations;
            }
        }

        /// <summary>
        /// Returns a new ObjectOperations object.  See the Operations property for why you might want to call this.
        /// </summary>
        public ObjectOperations CreateOperations() {
            return new ObjectOperations(new DynamicOperations(LanguageContext), this);
        }

        /// <summary>
        /// Returns a new ObjectOperations object that inherits any semantics particular to the provided ScriptScope.  
        /// 
        /// See the Operations property for why you might want to call this.
        /// </summary>
        public ObjectOperations CreateOperations(ScriptScope scope) {
            ContractUtils.RequiresNotNull(scope, nameof(scope));

            return new ObjectOperations(LanguageContext.Operations, this);
        }

        #endregion

        #region Code Execution (for convenience)

        /// <summary>
        /// Executes an expression. The execution is not bound to any particular scope.
        /// </summary>
        /// <exception cref="NotSupportedException">The engine doesn't support code execution.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is a <c>null</c> reference.</exception>
        public dynamic Execute(string expression) {
            // The host doesn't need the scope so do not create it here. 
            // The language can treat the code as not bound to a DLR scope and change global lookup semantics accordingly.
            return CreateScriptSourceFromString(expression).Execute(); 
        }

        /// <summary>
        /// Executes an expression within the specified scope.
        /// </summary>
        /// <exception cref="NotSupportedException">The engine doesn't support code execution.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is a <c>null</c> reference.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="scope"/> is a <c>null</c> reference.</exception>
        public dynamic Execute(string expression, ScriptScope scope) {
            return CreateScriptSourceFromString(expression).Execute(scope);
        }

        /// <summary>
        /// Executes an expression within a new scope and converts result to the given type.
        /// </summary>
        /// <exception cref="NotSupportedException">The engine doesn't support code execution.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is a <c>null</c> reference.</exception>
        public T Execute<T>(string expression) {
            return Operations.ConvertTo<T>((object)Execute(expression));
        }
        
        /// <summary>
        /// Executes an expression within the specified scope and converts result to the given type.
        /// </summary>
        /// <exception cref="NotSupportedException">The engine doesn't support code execution.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is a <c>null</c> reference.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="scope"/> is a <c>null</c> reference.</exception>
        public T Execute<T>(string expression, ScriptScope scope) {
            return Operations.ConvertTo<T>((object)Execute(expression, scope));
        }

        /// <summary>
        /// Executes content of the specified file in a new scope and returns that scope.
        /// </summary>
        /// <exception cref="NotSupportedException">The engine doesn't support code execution.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is a <c>null</c> reference.</exception>
        public ScriptScope ExecuteFile(string path) {
            return ExecuteFile(path, CreateScope());
        }

        /// <summary>
        /// Executes content of the specified file against the given scope.
        /// </summary>
        /// <returns>The <paramref name="scope"/>.</returns>
        /// <exception cref="NotSupportedException">The engine doesn't support code execution.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is a <c>null</c> reference.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="scope"/> is a <c>null</c> reference.</exception>
        public ScriptScope ExecuteFile(string path, ScriptScope scope) {
            CreateScriptSourceFromFile(path).Execute(scope);
            return scope;
        }

#if FEATURE_REMOTING
        /// <summary>
        /// Executes the expression in the specified scope and return a result.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// </summary>
        public ObjectHandle ExecuteAndWrap(string expression, ScriptScope scope) {
            return new ObjectHandle((object)Execute(expression, scope));
        }

        /// <summary>
        /// Executes the code in an empty scope.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// </summary>
        public ObjectHandle ExecuteAndWrap(string expression) {
            return new ObjectHandle((object)Execute(expression));
        }

        /// <summary>
        /// Executes the expression in the specified scope and return a result.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// 
        /// If an exception is thrown the exception is caught and an ObjectHandle to
        /// the exception is provided.
        /// </summary>
        /// <remarks>
        /// Use this API in case the exception is not serializable (for example, due to security restrictions) or its serialization 
        /// loses information that you need to access.
        /// </remarks>
        [Obsolete("Use ScriptSource.ExecuteAndWrap instead")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ObjectHandle ExecuteAndWrap(string expression, ScriptScope scope, out ObjectHandle exception) {
            exception = null;
            try {
                return new ObjectHandle((object)Execute(expression, scope));
            } catch (Exception e) {
                exception = new ObjectHandle(e);
                return null;
            }
        }

        /// <summary>
        /// Executes the code in an empty scope.
        /// Returns an ObjectHandle wrapping the resulting value of running the code.  
        /// 
        /// If an exception is thrown the exception is caught and an ObjectHandle to
        /// the exception is provided.
        /// </summary>
        /// <remarks>
        /// Use this API in case the exception is not serializable (for example, due to security restrictions) or its serialization 
        /// loses information that you need to access.
        /// </remarks>
        [Obsolete("Use ScriptSource.ExecuteAndWrap instead")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ObjectHandle ExecuteAndWrap(string expression, out ObjectHandle exception) {
            exception = null;
            try {
                return new ObjectHandle((object)Execute(expression));
            } catch (Exception e) {
                exception = new ObjectHandle(e);
                return null;
            }
        }
#endif
        
        #endregion

        #region Scopes

        /// <summary>
        /// Creates a new ScriptScope using the default storage container
        /// </summary>
        public ScriptScope CreateScope() {
            return new ScriptScope(this, LanguageContext.CreateScope());
        }

        /// <summary>
        /// Creates a new ScriptScope whose storage contains the provided dictionary of objects
        /// 
        /// Accesses to the ScriptScope will turn into get,set, and delete members against this dictionary
        /// </summary>
        public ScriptScope CreateScope(IDictionary<string, object> dictionary)
        {
            ContractUtils.RequiresNotNull(dictionary, nameof(dictionary));
            return new ScriptScope(this, LanguageContext.CreateScope(dictionary));
        }

        /// <summary>
        /// Creates a new ScriptScope whose storage is an arbitrary object.
        /// 
        /// Accesses to the ScriptScope will turn into get, set, and delete members against the object.
        /// </summary>
        public ScriptScope CreateScope(IDynamicMetaObjectProvider storage) {
            ContractUtils.RequiresNotNull(storage, nameof(storage));

            return new ScriptScope(this, LanguageContext.CreateScope(storage));
        }

        /// <summary>
        /// This method returns the ScriptScope in which a ScriptSource of given path was executed.  
        /// 
        /// The ScriptSource.Path property is the key to finding the ScriptScope.  Hosts need 
        /// to make sure they create a ScriptSource and set its Path property appropriately.
        /// 
        /// GetScope is primarily useful for tools that need to map files to their execution scopes. For example, 
        /// an editor and interpreter tool might run a file Foo that imports or requires a file Bar.  
        /// 
        /// The editor's user might later open the file Bar and want to execute expressions in its context.  
        /// The tool would need to find Bar's ScriptScope for setting the appropriate context in its interpreter window. 
        /// This method helps with this scenario.
        /// </summary>
        public ScriptScope GetScope(string path) {
            ContractUtils.RequiresNotNull(path, nameof(path));
            Scope scope = LanguageContext.GetScope(path);
            return (scope != null) ? new ScriptScope(this, scope) : null;
        }

        #endregion

        #region Source Unit Creation

        /// <summary>
        /// Return a ScriptSource object from string contents with the current engine as the language binding.
        /// 
        /// The default SourceCodeKind is AutoDetect.
        /// 
        /// The ScriptSource's Path property defaults to <c>null</c>.
        /// </summary>
        public ScriptSource CreateScriptSourceFromString(string expression) {
            ContractUtils.RequiresNotNull(expression, nameof(expression));

            return CreateScriptSource(new SourceStringContentProvider(expression), null, SourceCodeKind.AutoDetect);
        }

        /// <summary>
        /// Return a ScriptSource object from string contents with the current engine as the language binding.
        /// 
        /// The ScriptSource's Path property defaults to <c>null</c>.
        /// </summary>
        public ScriptSource CreateScriptSourceFromString(string code, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(code, nameof(code));
            ContractUtils.Requires(kind.IsValid(), nameof(kind));

            return CreateScriptSource(new SourceStringContentProvider(code), null, kind);
        }

        /// <summary>
        /// Return a ScriptSource object from string contents with the current engine as the language binding.
        /// 
        /// The default SourceCodeKind is AutoDetect.
        /// </summary>
        public ScriptSource CreateScriptSourceFromString(string expression, string path) {
            ContractUtils.RequiresNotNull(expression, nameof(expression));

            return CreateScriptSource(new SourceStringContentProvider(expression), path, SourceCodeKind.AutoDetect);
        }

        /// <summary>
        /// Return a ScriptSource object from string contents.  These are helpers for creating ScriptSources' with the right language binding.
        /// </summary>
        public ScriptSource CreateScriptSourceFromString(string code, string path, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(code, nameof(code));
            ContractUtils.Requires(kind.IsValid(), nameof(kind));

            return CreateScriptSource(new SourceStringContentProvider(code), path, kind);
        }

        /// <summary>
        /// Return a ScriptSource object from file contents with the current engine as the language binding.  
        /// 
        /// The path's extension does NOT have to be in ScriptRuntime.GetRegisteredFileExtensions 
        /// or map to this language engine with ScriptRuntime.GetEngineByFileExtension.
        /// 
        /// The default SourceCodeKind is File.
        /// 
        /// The ScriptSource's Path property will be the path argument.
        /// 
        /// The encoding defaults to System.Text.Encoding.Default.
        /// </summary>
        public ScriptSource CreateScriptSourceFromFile(string path) {
            return CreateScriptSourceFromFile(path, StringUtils.DefaultEncoding, SourceCodeKind.File);
        }

        /// <summary>
        /// Return a ScriptSource object from file contents with the current engine as the language binding.  
        /// 
        /// The path's extension does NOT have to be in ScriptRuntime.GetRegisteredFileExtensions 
        /// or map to this language engine with ScriptRuntime.GetEngineByFileExtension.
        /// 
        /// The default SourceCodeKind is File.
        /// 
        /// The ScriptSource's Path property will be the path argument.
        /// </summary>
        public ScriptSource CreateScriptSourceFromFile(string path, Encoding encoding) {
            return CreateScriptSourceFromFile(path, encoding, SourceCodeKind.File);
        }

        /// <summary>
        /// Return a ScriptSource object from file contents with the current engine as the language binding.  
        /// 
        /// The path's extension does NOT have to be in ScriptRuntime.GetRegisteredFileExtensions 
        /// or map to this language engine with ScriptRuntime.GetEngineByFileExtension.
        /// 
        /// The ScriptSource's Path property will be the path argument.
        /// </summary>
        public ScriptSource CreateScriptSourceFromFile(string path, Encoding encoding, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(path, nameof(path));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));
            ContractUtils.Requires(kind.IsValid(), nameof(kind));
            if (!LanguageContext.CanCreateSourceCode) throw new NotSupportedException("Invariant engine cannot create scripts");

            return new ScriptSource(this, LanguageContext.CreateFileUnit(path, encoding, kind));
        }

#if FEATURE_CODEDOM
        /// <summary>
        /// This method returns a ScriptSource object from a System.CodeDom.CodeObject.  
        /// This is a factory method for creating a ScriptSources with this language binding.
        /// 
        /// The expected CodeDom support is extremely minimal for syntax-independent expression of semantics.  
        /// 
        /// Languages may do more, but hosts should only expect CodeMemberMethod support, 
        /// and only sub nodes consisting of the following:
        ///     CodeSnippetStatement
        ///     CodeSnippetExpression
        ///     CodePrimitiveExpression
        ///     CodeMethodInvokeExpression
        ///     CodeExpressionStatement (for holding MethodInvoke)
        /// </summary>
        public ScriptSource CreateScriptSource(CodeObject content) {
            return CreateScriptSource(content, null, SourceCodeKind.File);
        }

        /// <summary>
        /// This method returns a ScriptSource object from a System.CodeDom.CodeObject.  
        /// This is a factory method for creating a ScriptSources with this language binding.
        /// 
        /// The expected CodeDom support is extremely minimal for syntax-independent expression of semantics.  
        /// 
        /// Languages may do more, but hosts should only expect CodeMemberMethod support, 
        /// and only sub nodes consisting of the following:
        ///     CodeSnippetStatement
        ///     CodeSnippetExpression
        ///     CodePrimitiveExpression
        ///     CodeMethodInvokeExpression
        ///     CodeExpressionStatement (for holding MethodInvoke)
        /// </summary>
        public ScriptSource CreateScriptSource(CodeObject content, string path) {
            return CreateScriptSource(content, path, SourceCodeKind.File);
        }

        /// <summary>
        /// This method returns a ScriptSource object from a System.CodeDom.CodeObject.  
        /// This is a factory method for creating a ScriptSources with this language binding.
        /// 
        /// The expected CodeDom support is extremely minimal for syntax-independent expression of semantics.  
        /// 
        /// Languages may do more, but hosts should only expect CodeMemberMethod support, 
        /// and only sub nodes consisting of the following:
        ///     CodeSnippetStatement
        ///     CodeSnippetExpression
        ///     CodePrimitiveExpression
        ///     CodeMethodInvokeExpression
        ///     CodeExpressionStatement (for holding MethodInvoke)
        /// </summary>
        public ScriptSource CreateScriptSource(CodeObject content, SourceCodeKind kind) {
            return CreateScriptSource(content, null, kind);
        }

        /// <summary>
        /// This method returns a ScriptSource object from a System.CodeDom.CodeObject.  
        /// This is a factory method for creating a ScriptSources with this language binding.
        /// 
        /// The expected CodeDom support is extremely minimal for syntax-independent expression of semantics.  
        /// 
        /// Languages may do more, but hosts should only expect CodeMemberMethod support, 
        /// and only sub nodes consisting of the following:
        ///     CodeSnippetStatement
        ///     CodeSnippetExpression
        ///     CodePrimitiveExpression
        ///     CodeMethodInvokeExpression
        ///     CodeExpressionStatement (for holding MethodInvoke)
        /// </summary>
        public ScriptSource CreateScriptSource(CodeObject content, string path, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(content, nameof(content));
            if (!LanguageContext.CanCreateSourceCode) throw new NotSupportedException("Invariant engine cannot create scripts");

            return new ScriptSource(this, LanguageContext.GenerateSourceCode(content, path, kind));
        }
#endif

        /// <summary>
        /// These methods return ScriptSource objects from stream contents with the current engine as the language binding.  
        /// 
        /// The default SourceCodeKind is File.
        /// 
        /// The encoding defaults to Encoding.Default.
        /// </summary>
        public ScriptSource CreateScriptSource(StreamContentProvider content, string path) {
            ContractUtils.RequiresNotNull(content, nameof(content));

            return CreateScriptSource(content, path, StringUtils.DefaultEncoding, SourceCodeKind.File);
        }

        /// <summary>
        /// These methods return ScriptSource objects from stream contents with the current engine as the language binding.  
        /// 
        /// The default SourceCodeKind is File.
        /// </summary>
        public ScriptSource CreateScriptSource(StreamContentProvider content, string path, Encoding encoding) {
            ContractUtils.RequiresNotNull(content, nameof(content));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));

            return CreateScriptSource(content, path, encoding, SourceCodeKind.File);
        }

        /// <summary>
        /// These methods return ScriptSource objects from stream contents with the current engine as the language binding.  
        /// 
        /// The encoding defaults to Encoding.Default.
        /// </summary>
        public ScriptSource CreateScriptSource(StreamContentProvider content, string path, Encoding encoding, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(content, nameof(content));
            ContractUtils.RequiresNotNull(encoding, nameof(encoding));
            ContractUtils.Requires(kind.IsValid(), nameof(kind));

            return CreateScriptSource(new LanguageBoundTextContentProvider(LanguageContext, content, encoding, path), path, kind);
        }

        /// <summary>
        /// This method returns a ScriptSource with the content provider supplied with the current engine as the language binding.
        /// 
        /// This helper lets you own the content provider so that you can implement a stream over internal host data structures, such as an editor's text representation.
        /// </summary>
        public ScriptSource CreateScriptSource(TextContentProvider contentProvider, string path, SourceCodeKind kind) {
            ContractUtils.RequiresNotNull(contentProvider, nameof(contentProvider));
            ContractUtils.Requires(kind.IsValid(), nameof(kind));
            if (!LanguageContext.CanCreateSourceCode) throw new NotSupportedException("Invariant engine cannot create scripts");

            return new ScriptSource(this, LanguageContext.CreateSourceUnit(contentProvider, path, kind));
        }

        #endregion

        #region Additional Services

        /// <summary>
        /// This method returns a language-specific service.  
        /// 
        /// It provides a point of extensibility for a language implementation 
        /// to offer more functionality than the standard engine members discussed here.
        /// 
        /// Commonly available services include:
        ///     TokenCategorizer
        ///         Provides standardized tokenization of source code
        ///     ExceptionOperations
        ///         Provides formatting of exception objects.
        ///     DocumentationProvidera
        ///         Provides documentation for live object.
        /// </summary>
        public TService GetService<TService>(params object[] args) where TService : class {
            if (typeof(TService) == typeof(TokenCategorizer)) {
                TokenizerService service = LanguageContext.GetService<TokenizerService>(ArrayUtils.Insert((object)LanguageContext, args));
                return (service != null) ? (TService)(object)new TokenCategorizer(service) : null;
            }

            if (typeof(TService) == typeof(ExceptionOperations)) {
                ExceptionOperations service = LanguageContext.GetService<ExceptionOperations>();
                return (service != null) ? (TService)(object)service : (TService)(object)new ExceptionOperations(LanguageContext);
            }

            if (typeof(TService) == typeof(DocumentationOperations)) {
                DocumentationProvider service = LanguageContext.GetService<DocumentationProvider>(args);
                return (service != null) ? (TService)(object)new DocumentationOperations(service) : null;
            }

            return LanguageContext.GetService<TService>(args);
        }

        #endregion

        #region Misc. engine information

        /// <summary>
        /// This property returns readon-only LanguageOptions this engine is using.
        /// </summary>
        /// <remarks>
        /// The values are determined during runtime initialization and read-only afterwards. 
        /// You can change the settings via a configuration file or explicitly using ScriptRuntimeSetup class.
        /// </remarks>
        public LanguageSetup Setup {
            get {
                if (_config == null) {
                    // The user shouldn't be able to get a hold of the invariant engine
                    Debug.Assert(!(LanguageContext is InvariantContext));

                    // Find the matching language configuration
                    LanguageConfiguration config = Runtime.Manager.Configuration.GetLanguageConfig(LanguageContext);
                    Debug.Assert(config != null);

                    foreach (var language in Runtime.Setup.LanguageSetups) {
                        if (config.ProviderName == new AssemblyQualifiedTypeName(language.TypeName)) {
                            return _config = language;
                        }
                    }
                }
                return _config;
            }
        }

        /// <summary>
        /// This property returns the ScriptRuntime for the context in which this engine executes.
        /// </summary>
        public ScriptRuntime Runtime { get; }

        /// <summary>
        /// This property returns the engine's version as a string.  The format is language-dependent.
        /// </summary>
        public Version LanguageVersion => LanguageContext.LanguageVersion;

        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public CompilerOptions GetCompilerOptions() {
            return LanguageContext.GetCompilerOptions();
        }

        public CompilerOptions GetCompilerOptions(ScriptScope scope) {
            return LanguageContext.GetCompilerOptions(scope.Scope);
        }

        /// <summary>
        /// Sets the search paths used by the engine for loading files when a script wants 
        /// to import or require another file of code.  
        /// </summary>
        /// <exception cref="NotSupportedException">The language doesn't allow to set search paths.</exception>
        public void SetSearchPaths(ICollection<string> paths) {
            ContractUtils.RequiresNotNull(paths, nameof(paths));
            ContractUtils.RequiresNotNullItems(paths, nameof(paths));

            LanguageContext.SetSearchPaths(paths);
        }

        /// <summary>
        /// Gets the search paths used by the engine for loading files when a script wants 
        /// to import or require another file of code.  
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public ICollection<string> GetSearchPaths() {
            return LanguageContext.GetSearchPaths();
        }

        #region Internal API Surface

        internal LanguageContext LanguageContext { get; }

        internal TRet Call<T, TRet>(Func<LanguageContext, T, TRet> f, T arg) {
            return f(LanguageContext, arg);
        }

        #endregion

        #region Remote API

#if FEATURE_REMOTING
        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif

        #endregion
    }
}
