# 4 API References

Need to dope in what exceptions are thrown where I say we throw one, and need to add more details about argument checking and exceptions we throw.

<h2 id="scriptruntime-class">4.1 ScriptRuntime Class</h2>

This class is the starting point for hosting. For Level One scenarios, you just create ScriptScopes, use Globals, and use ExecuteFile. For Level Two scenarios, you can get to ScriptEngines and so on.

ScriptRuntime represents global script state. This includes referenced assemblies, a "global object" (ScriptRuntime.Globals), "published" scopes (scopes bound to a name on Globals), available language engines, etc.

ScriptRuntime has a single constructor and two convenience factory methods. You can create multiple instances of a ScriptRuntime in a single AppDomain. For information on configuring a ScriptRuntime for what languages it allows, global settings, language settings, etc., see ScriptRuntimeSetup.

<h3 id="class-summary">4.1.1 Class Summary</h3>

public class ScriptRuntime : MarshalByRefObject

public ScriptRuntime(ScriptRuntimeSetup setup)

public static ScriptRuntime CreateFromConfiguration()

public static ScriptRuntime

CreateRemote(AppDomain domain, ScriptRuntimeSetup setup)

public ScriptScope ExecuteFile(string path)

public ScriptScope Globals { get; set;}

public ScriptScope CreateScope()

public ScriptScope CreateScope(IDynamicMetaObjectProvider storage)

public ScriptEngine GetEngine(string languageId)

public ScriptEngine GetEngineByFileExtension(string extension)

public string\[\] GetRegisteredFileExtensions()

public string\[\] GetRegisteredLanguageIdentifiers()

public void LoadAssembly(Assembly assm)

~~public ObjectOperations Operations { get; }~~

~~public ObjectOperations CreateOperations()~~

public ScriptRuntimeSetup Setup { get; }

public ScriptHost Host { get; }

public ScriptIO IO { get; }

public void Shutdown()

<h3 id="constructor">4.1.2 Constructor</h3>

The constructor requires a ScriptRuntimeSetup, which gives the host full control of the languages allowed in the ScriptRuntime, their options, and the global runtime options.

This method ensures the list of languages in the setup object has no duplicate elements based on the LanguageSetup.TypeName property (just comparing them as strings at this point). Later, when engines fault in, the DLR also ensures that none of the assembly-qualified types actually identify the same type.

This method ensures the list of languages in the setup have no conflicting LangaugeSetup.Names elements or Language.FileExtensions elements.

After calling this method, modifying the ScriptRuntimeSetup object throws an exception.

Signature:

public ScriptRuntime(ScriptRuntimeSetup setup)

<h3 id="create-methods">4.1.3 Create\* Methods</h3>

These factory methods construct and return ScriptRuntimes. They primarily are for convenience and discoverability via editors that complete members on types.

CreateFromConfiguration is just a convenience for:

new ScriptRuntime(ScriptRuntimeSetup.ReadConfiguration())

CreateRemote creates the ScriptRuntime in the specified domain, instantiates the ScriptRuntimeSetup.HostType in that domain, and returns the ScriptRuntime. Any arguments specified in ScriptRuntimeSetup.HostArguments must derive from MBRO or serialize across app domain boundaries. The same holds for any values in ScriptRuntimeSetup.Options and any LanguageSetup.Options.

Signatures:

public static ScriptRuntime CreateFromConfiguration()

public static ScriptRuntime

CreateRemote(AppDomain domain, ScriptRuntimeSetup setup)

<h3 id="executefile-method">4.1.4 ExecuteFile Method</h3>

This method executes the source identified in the path argument and returns the new ScriptScope in which the source executed. This method calls on the ScriptRuntime.Host to get the PlatformAdaptationLayer and then calls on it to resolve and open the path. ExecuteFile determines the language engine to use from the path's extension and the ScriptRuntime's configuration, comparing extensions case-insensitively.

This convenience method exists for Level 1 hosting scenarios where the path is likely an absolute pathname or a filename that naturally resolves with standard .NET BCL file open calls.

Signature:

public ScriptScope ExecuteFile(string path)

Each time this method is called it create a fresh ScriptScope in which to run the source. Calling Engine.GetScope returns the last ScriptScope created for repeated invocations of ExecuteFile on the same path.

This method adds variable name bindings within the ScriptRuntime.Globals object as appropriate for the language. Dynamic language code can then access and drill into objects bound to those names. For example, the IronPython loader adds the base file name to the ScriptRuntime.Globals as a Python module, and when IronPython is importing names, it looks in ScriptRuntime.Globals to find names to import. IronRuby's loader adds constants and modules to the ScriptRuntime.Globals object. DLR JScript adds all globals there.

In Globals, each language decides its own semantics for name conflicts, but the expected model is last-writer-wins. Languages do have the ability to add names to Globals so that only code executing in that language can see the global names. In this case, other languages would not have the ability to clobber the name bindings. For example, Python might do this for its special built-in modules. However, most names should be added so that all languages can see the bindings and interoperate with the objects bound to the names.

<h3 id="usefile-method">4.1.5 UseFile Method</h3>

This method executes the source identified in the path argument and returns the new ScriptScope in which the source executed. If the identified file was already executed, this method does NOT execute the file again, but instead the method just returns the ScriptScope. The path must have a language file extension registered with the ScriptRuntime. UseFile affects ScriptRuntime.Globals the same way ExecuteFile does.

This method is like ExecuteFile except in two ways. ExecuteFile always executes the file each time you call it, but UseFile executes the file at most once. UseFile resolves the path argument against the language engines search paths to find the file, but ExecuteFile just calls .NET open functions to find the file.

Essentially, this method finds the engine for the file extension and calls GetScope on the results of joining the path argument with each of the items in the engine's search paths. If it finds no scope, then it calls ExecuteFile on the first existing file found by joining the argument path with the items in the engine's search paths.

This convenience method exists for Level 1 hosting scenarios where the host wants to load a file of script code in the same manner a language would (for example, Python's import statement or Ruby's require function).

Signature:

public ScriptScope UseFile(string path)

<h3 id="globals-property">4.1.6 Globals Property</h3>

This property returns the "global object" or name bindings of the ScriptRuntime as a ScriptScope. You can set the globals scope, which you might do if you created a ScriptScope with an IDynamicMetaObjectProvider so that your host could late bind names. The easiest way to provide an IDynamicMetaObjectProvider is to use ExpandObject or derive from DynamicObject.

Signature:

public ScriptScope Globals { get; set; }

<h3 id="createscope-method">4.1.7 CreateScope Method</h3>

This method returns a new ScriptScope.

Signatures:

public ScriptScope CreateScope()

public ScriptScope CreateScope(IDynamicMetaObjectProvider storage)

The storage parameter lets you supply the dictionary of the scope so that you can provide late bound values for some name lookups. If storage is null, this method throws an ArgumentNullException.

<h3 id="getengine-method">4.1.8 GetEngine Method</h3>

This method returns the one engine associated with this ScriptRuntime that matches the languageId argument, compared case-insensitively. This loads the engine and initializes it if needed.

Signature:

public ScriptEngine GetEngine(string languageId)

If languageId is null, or it does not map to an engine in the ScriptRuntime's configuration, then this method throws an exception.

<h3 id="getenginebyfileextension-method">4.1.9 GetEngineByFileExtension Method</h3>

This method takes a file extension and returns the one engine associated with this ScriptRuntime that matches the extension argument. This strips one leading period if extension starts with a period.

This loads the engine and initializes it if needed. The file extension associations are determined by the ScriptRuntime configuration (see configuration section above). This method compares extensions case-insensitively.

Signature:

public ScriptEngine GetEngineByFileExtension(string extension)

If extension is null, or it does not map to an engine in the ScriptRuntime's configuration, then this method throws an exception.

<h3 id="getenginebymimetype-method">4.1.10 ~~GetEngineByMimeType Method~~</h3>

~~This method takes a MIME type and returns the one engine associated with this ScriptRuntime that matches the argument.~~

~~This loads the engine and initializes it if needed. The MIME type associations are determined by the ScriptRuntime configuration (see configuration section above).~~

~~Signature:~~

~~public ScriptEngine GetEngineByMimeType(string mimetype);~~

~~If mimetype is null, or it does not map to an engine in the ScriptRuntime's configuration, then this method throws an exception.~~

<h3 id="getregisteredfileextensions-method">4.1.11 GetRegisteredFileExtensions Method</h3>

This method returns an array of strings (without periods) where each element is a registered file extension for this ScriptRuntime. Each file extension maps to a language engine based on the ScriptRuntime configuration (see configuration section above). If there are none, this returns an empty array.

Signature:

public string\[\] GetRegisteredFileExtensions()

<h3 id="getregisteredlanguageidentifiers-method">4.1.12 GetRegisteredLanguageIdentifiers Method</h3>

This method returns an array of strings where each element is a registered language identifier for this ScriptRuntime. Each language identifier maps to a language engine based on the ScriptRuntime configuration (see configuration section above). Typically all registered file extensions are also language identifiers. If there are no language identifiers, this returns an empty array.

Signature:

public string\[\] GetRegisteredLanguageIdentifiers()

<h3 id="loadassembly-method">4.1.13 LoadAssembly Method</h3>

This method calls on language engines to inform them of DLLs whose namespaces and types should be available to code the engines execute. Language engines can make the names available however they see fit. They may resolved free identifiers first to ScriptRuntime.Globals and then to names provided by loaded DLLs. They may make the names available to 'using', 'import', or 'require' expressions. They may add names to ScriptRuntime.Globals that are bound to dynamic objects for accessing sub namespaces and types by drilling in from root namespaces stored in Globals.

By default, the DLR seeds the ScriptRuntime with Mscorlib and System assemblies. You can avoid this by setting the ScriptRuntimeSetup option "NoDefaultReferences" to true. When new language engines load, the ScriptRuntime passes the list of loaded assemblies.

Signature:

public void LoadAssembly(Assembly assm)

*The following is the old behavior that was cut in lieu of the more flexible, langauge-specific support above (kept here should it come back before adding these APIs to .NET):*

~~walks the assembly's namespaces and adds name bindings in ScriptRuntime.Globals to represent namespaces available in the assembly. Each top-level namespace name becomes a name in Globals, bound to a dynamic object representing the namespace. Within each top-level namespace object, the DLR binds names to dynamic objects representing each sub namespace or type.~~

~~There is a bug today that instead of dynamic objects representing namespaces and types, the DLR stores NamespaceTrackers and TypeTrackers, which only implement IAttributesCollection. We are considering cutting these types, perhaps even leaving all reflection with dynamic objects to the langauge. See the Sympl language example for what it does. Production-quality languages may want something as sophisticated as the tracker objects, which IronPython will continue to use from an IronPython DLL.~~

~~By default, the DLR seeds the ScriptRuntime with Mscorlib and System assemblies. You can avoid this by setting the ScriptRuntimeSetup option "NoDefaultReferences" to true.~~

~~When this method encounters the same fully namespace-qualified type name, it merges names together objects representing the namespaces. If you called LoadAssembly on two different assemblies, each contributing to System.Foo.Bar namespace, then all names within System.Foo.Bar from both assemblies will be present in the resulting object representing Bar.~~

<h3 id="operations-property">4.1.14 Operations Property</h3>

Likely cutting this from the hosting APIs soon. We've shifted our thinking away from having a language-invariant static helper since it can't do very much without baking in language choices, like implicit conversions and whatnot just to invoke objects.

This property returns a default, language-neutral ObjectOperations. ObjectOperations lets you perform various operations on objects. When the objects do not provide their own behaviors for performing the operations, this ObjectOperations uses general .NET semantics. Because there are many situations when general .NET semantics are insufficient due to dynamic objects often not using straight .NET BCL types, this ObjectOperations will throw exceptions when one produced by a ScriptEngine would succeed.

Because an ObjectOperations object caches rules for the types of objects and operations it processes, using the default ObjectOperations for many objects could degrade the caching benefits. Eventually the cache for some operations could degrade to a point where ObjectOperations stops caching and does a full search for an implementation of the requested operation for the given objects. For simple hosting situations, this is sufficient behavior.

See CreateOperations for alternatives.

Signature:

public ObjectOperations Operations { get; }

<h3 id="createoperations-methods">4.1.15 CreateOperations Methods</h3>

Likely cutting this from the hosting APIs soon. We've shifted our thinking away from having a language-invariant static helper since it can't do very much without baking in language choices, like implicit conversions and whatnot just to invoke objects.

These methods return a new ObjectOperations object. See the Operations property for why you might want to call this and for limitations of ObjectOperations provided by a ScriptRuntime instead of one obtained from a ScriptEngine.

There currently is little guidance on how to choose when to create new ObjectOperations objects. However, there is a simple heuristic. If you were to perform some operations over and over on the same few types of objects, it would be advantageous to create an ObjectOperations just for use with those few cases. If you perform different operations with many types of objects just once or twice, you can use the default instance provided by the ObjectOperations property.

Signature:

public ObjectOperations CreateOperations()

<h3 id="setup-property">4.1.16 Setup Property</h3>

This property returns a read-only ScriptRuntimeSetup object describing the configuration information used to create the ScriptRuntime.

Signature:

public ScriptRuntimeSetup Setup { get; }

<h3 id="host-property">4.1.17 Host Property</h3>

This property returns the ScriptHost associated with the ScriptRuntime. This is not settable because the ScriptRuntime must create the host from a supplied type to support remote ScriptRuntime creation. Setting it would also be bizarre because it would be similar to changing the owner of the ScriptRuntime.

Signature:

public ScriptHost Host { get; }

<h3 id="io-property">4.1.18 IO Property</h3>

This property returns the ScriptIO associated with the ScriptRuntime. The ScriptIO lets you control the standard input and output streams for code executing in the ScriptRuntime.

Signature:

public ScriptIO IO { get; }

<h3 id="shutdown-method">4.1.19 Shutdown Method</h3>

This method announces to the language engines that are loaded that the host is done using the ScriptRuntime. Languages that have a shutdown hook or mechanism for code to release system resources on shutdown will invoke their shutdown protocols.

There are no other guarantees from this method. For example, It is undefined when code executing (possibly on other threads) will stop running. Also, any calls on the ScriptRuntime, hosting API objects associated with the runtime, or dynamic objects extracted from the runtime have undefined behavior.

Signature:

public void Shutdown()

<h2 id="scriptscope-class">4.2 ScriptScope Class</h2>

This class represents a namespace essentially. Hosts can bind variable names in ScriptScopes, fetch variable values, etc. Hosts can execute code within scopes for distinct name bindings.

ScriptScopes also have some convenience members and an optional language affinity. Scopes use the language to look up names and convert values. If the ScriptScope has no default language, then these convenience methods throw an exception, and the Engine property returns null.

Hosts can store ScriptScopes as the values of names on ScriptRuntime.Globals or in other scopes. When dynamic language code encounters a ScriptScope as an object, the DLR manifests the scope as a dynamic object. This means that normal object member access sees the variables stored in the ScriptScope first. Languages executing code that is doing the object member access get a chance to find members if the members are not variables in the ScriptScope. The language might bind to the .NET static type members documented here. They might detect the .NET type is ScriptScope and throw a missing member exception, use meta-programming hooks to give dynamic language code a chance to produce the member, or return sentinel objects according to the language's semantics.

Hosts can use ScriptScopes (regardless of whether they have a language affinity) to execute any kind of code within their namespace context. ScriptEngine methods that execute code take a ScriptScope argument. There are parallel methods on engines for getting and setting variables so that hosts can request a name lookup with any specific language's semantics in any ScriptScope.

You create instances of ScriptScopes using the CreateScope and ExecuteFile methods on ScriptRuntimes or CreateScope on ScriptEngine.

Note, members that take or return ObjectHandles are not present on Silverlight.

<h3 id="class-summary-1">4.2.1 Class Summary</h3>

public class ScriptScope : MarshalByRefObject {

public object GetVariable(string name)

public ObjectHandle GetVariableHandle(string name)

public bool RemoveVariable(string name)

public void SetVariable(string name, object value)

public void SetVariable(string name, ObjectHandle handle)

public bool TryGetVariable(string name, out object value)

public bool TryGetVariableHandle(string name,

out ObjectHandle handle)

public T GetVariable&lt;T&gt;(string name)

public bool TryGetVariable&lt;T&gt;(string name, out T value)

public bool ContainsVariable(string name)

public IEnumerable&lt;string&gt; GetVariableNames()

public IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; GetItems()

public ScriptEngine Engine { get;}

<h3 id="getvariable-methods">4.2.2 GetVariable\* Methods</h3>

These methods fetch the value of a variable stored in the scope.

If there is no engine associated with the scope (see ScriptRuntime.CreateScope), then the name lookup is a case-sensitive, literal lookup of the name in the scope's dictionary. If there is a default engine, then the name lookup uses that language's semantics.

Signatures:

public object GetVariable(string name)

public ObjectHandle GetVariableHandle(string name)

public T GetVariable&lt;T&gt;(string name)

GetVariableHandle is useful when the ScriptScope is remote so that you get back an ObjectHandle referring to the value.

GetVariable&lt;T&gt; uses language-specific (based on the default language in the Engine property) conversions. These may be implicit only, or include explicit conversions too. This method throws a NotSupportedException if the engine cannot perform the requested type conversion. If there is no associated engine, this method essentially just casts to T, which could throw an ArgumentException.

If you need an explicit conversion to T, you can use scope.Engine.Operations.ExplicitConvertTo&lt;T&gt;.

<h3 id="setvariable-methods">4.2.3 SetVariable Methods</h3>

These methods assign a value to a variable in the scope, overwriting any previous value.

If there is no engine associated with the scope (see ScriptRuntime.CreateScope), then the name mapping is a case-sensitive, literal mapping of the name in the scope's dictionary. If there is a default engine, then the name lookup uses that language's semantics.

Signatures:

public void SetVariable(string name, object value)

public void SetVariable(string name, ObjectHandle handle)

<h3 id="trygetvariable-methods">4.2.4 TryGetVariable\* Methods</h3>

These methods fetch the value of a variable stored in the scope and return a Boolean indicating success of the lookup. When the method's result is false, then it assigns null to value.

If there is no engine associated with the scope (see ScriptRuntime.CreateScope), then the name lookup is a case-sensitive, literal lookup of the name in the scope's dictionary. If there is a default engine, then the name lookup uses that language's semantics.

Signatures:

public bool TryGetVariable(string name, out object value)

public bool TryGetVariableHandle(string name,

out ObjectHandle handle)

public bool TryGetVariable&lt;T&gt;(string name, out T value)

TryGetVariableHandle is useful when the ScriptScope is remote so that you get back an ObjectHandle referring to the value.

TryGetVariable&lt;T&gt; uses language-specific (based on the default language in the Engine property) conversions. These may be implicit only, or include explicit conversions too. It throws a NotSupportedException if the engine cannot perform the requested type conversion. If there is no associated engine, this method uses standard .NET conversion, which could throw an ArgumentException.

If you need an explicit conversion to T, you can use scope.Engine.Operations.TryExplicitConvertTo&lt;T&gt;.

<h3 id="containsvariable-method">4.2.5 ContainsVariable Method</h3>

This method returns whether the variable is exists in this scope and has a value.

If there is no engine associated with the scope (see ScriptRuntime.CreateScope), then the name lookup is a literal lookup of the name in the scope's dictionary. Therefore, it is case-sensitive for example. If there is a default engine, then the name lookup uses that language's semantics.

Signature:

public bool ContainsVariable(string name)

<h3 id="getvariablenames-method">4.2.6 GetVariableNames Method</h3>

This method returns an enumeration of strings, one string for each variable name in this scope. If there are no names, then it returns an empty array. Modifying the array has no impact on the ScriptScope. This method returns a new instance for the result of each call.

Signature:

public IEnumerable&lt;string&gt; GetVariableNames()

<h3 id="getitems-method">4.2.7 GetItems Method</h3>

This method returns an IEnumerable of variable name/value pairs, one for each variable name in this scope. If there are no names, then the enumeration is empty. Modifying the array has no impact on the ScriptScope. This method returns a new instance for the result of each call, and modifying the scope while using the enumeration has undefined behavior.

Signature:

public IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; GetItems ()

<h3 id="removevariable-method">4.2.8 RemoveVariable Method</h3>

This method removes the variable name and returns whether the variable existed and had a value in the scope when you called this method.

If there is no engine associated with the scope (see ScriptRuntime.CreateScope), then the name lookup is a literal lookup of the name in the scope's dictionary. Therefore, it is case-sensitive for example. If there is a default engine, then the name lookup uses that language's semantics.

Some languages may refuse to remove some variables. If the scope has an associated language that has variables that cannot be removed, and name identifies such a variable, it is undefined what happens. Languages vary on whether this is a no-op or exceptional.

Signature:

public bool RemoveVariable(string name)

<h3 id="engine-property">4.2.9 Engine Property</h3>

This property returns the engine associated with this scope. If the scope was created without a language affinity, then this property returns null.

Signature:

public ScriptEngine Engine { get;}

<h2 id="scriptengine-class">4.3 ScriptEngine Class</h2>

ScriptEngines represent a language implementation in the DLR, and they are the work horse for intermediate and advanced hosting scenarios. ScriptEngines offer various ways to execute code and create ScriptScopes and ScriptSources. ScriptSources offer methods for executing code in various ways from different kinds of sources. ScriptEngines offer the more common or convenience methods for executing code.

There is only one instance of a ScriptEngine for a given language in a given ScriptRuntime. You get to engines with ScriptRuntime's methods or the Engine property of ScriptScope.

Note, members that take or return ObjectHandles are not present on Silverlight.

<h3 id="class-summary-2">4.3.1 Class Summary</h3>

public class ScriptEngine : MarshalByRefObject {

internal ScriptEngine()

public ScriptRuntime Runtime { get; }

public string LanguageDisplayName { get; }

public string\[\] GetRegisteredIdentifiers()

public string\[\] GetRegisteredExtensions()

public object Execute(string expression)

public object Execute(string expression, ScriptScope scope)

public T Execute&lt;T&gt;(string code)

public T Execute&lt;T&gt;(string expression, ScriptScope scope)

public ObjectHandle ExecuteAndWrap(string expression)

public ObjectHandle ExecuteAndWrap(string expression,

ScriptScope scope)

public ScriptScope ExecuteFile(string path)

public ScriptScope ExecuteFile(string path, ScriptScope scope)

public ScriptScope GetScope(string path)

public ObjectOperations Operations { get; }

public ObjectOperations CreateOperations()

public ObjectOperations CreateOperations(ScriptScope Scope)

public ScriptSource CreateScriptSourceFromString

(string expression)

public ScriptSource CreateScriptSourceFromString

(string expression, string path)

public ScriptSource CreateScriptSourceFromString

(string code, SourceCodeKind kind)

public ScriptSource CreateScriptSourceFromString

(string code, string path, SourceCodeKind kind)

public ScriptSource CreateScriptSourceFromFile(string path)

public ScriptSource CreateScriptSourceFromFile

(string path, System.Text.Encoding encoding)

public ScriptSource CreateScriptSourceFromFile

(string path, System.Text.Encoding encoding,

SourceCodeKind kind)

public ScriptSource CreateScriptSource

(StreamContentProvider contentProvider, string path)

public ScriptSource CreateScriptSource

(StreamContentProvider contentProvider, string path,

System.Text.Encoding encoding)

public ScriptSource CreateScriptSource

(StreamContentProvider contentProvider, string path,

System.Text.Encoding encoding, SourceCodeKind kind)

public ScriptSource CreateScriptSource

(TextContentProvider contentProvider, string path,

SourceCodeKind kind)

public ScriptSource CreateScriptSource(CodeObject content)

public ScriptSource CreateScriptSource(CodeObject content,

string path)

public ScriptSource CreateScriptSource(CodeObject content,

SourceCodeKind kind)

public ScriptSource CreateScriptSource

(System.CodeDom.CodeObject code,

string path, SourceCodeKind kind)

public ScriptScope CreateScope()

public ScriptScope CreateScope(IDynamicMetaObjectProvider globals)

public ServiceType GetService&lt;ServiceType&gt;(params object\[\] args)

where ServiceType : class

public LanguageSetup Setup { get; }

public CompilerOptions GetCompilerOptions()

public CompilerOptions GetCompilerOptions(ScriptScope scope)

    public ICollection&lt;string&gt; GetSourceSearchPaths()

public void SetSearchPaths (ICollection&lt;string&gt; paths)

public System.Version LanguageVersion { get; }

<h3 id="runtime-property">4.3.2 Runtime Property</h3>

This property returns the ScriptRuntime for the context in which this engine executes.

Signature:

public ScriptRuntime Runtime { get; }

<h3 id="languagedisplayname-property">4.3.3 LanguageDisplayName Property</h3>

This property returns a display name for the engine or language that is suitable for UI.

Signature:

public string LanguageDisplayName { get; }

<h3 id="getregistered-methods">4.3.4 GetRegistered\* Methods</h3>

These methods return unique identifiers for this engine and file extensions that map to this engine and its language. This information comes from configuration data passed to ScriptRuntime.Create.

Modifying the results of these methods has no effect on configuration of this engine.

Signatures:

public string\[\] GetRegisteredIdentifiers()

public string\[\] GetRegisteredExtensions()

<h3 id="execute-methods">4.3.5 Execute\* Methods</h3>

These methods execute the strings as expressions and return a result in various ways. There are complementary overloads that take a ScriptScope. The overloads that do not take scopes create a new scope for each execution. These methods throw the scope away and use it for side effects only, returning the result in the same way the complementary overload does.

Execute&lt;T&gt; returns the result as the specified type, using the engine's Operations.ConvertTo&lt;T&gt; method. If this method cannot convert to the specified type, then it throws a NotSupportedException.

ExecuteAndWrap returns an ObjectHandle for use when the engine and/or scope are remote.

Signatures:

public object Execute(string expression)

public object Execute(string expression, ScriptScope scope)

public T Execute&lt;T&gt;(string expression)

public T Execute&lt;T&gt;(string expression, ScriptScope scope)

public ObjectHandle ExecuteAndWrap(string expression)

public ObjectHandle ExecuteAndWrap(string expression,

ScriptScope scope)

<h3 id="executefile-methods">4.3.6 ExecuteFile Methods</h3>

These methods execute the strings the contents of files and return the scope in which the string executed. The overload that does not take a ScriptScope creates a new one each time it is called.

Signatures:

public ScriptScope ExecuteFile(string path)

public ScriptScope ExecuteFile(string path, ScriptScope scope)

<h3 id="getscope-method">4.3.7 GetScope Method</h3>

This method returns the ScriptScope in which the specified path/source executed. This method works in conjunction with LoadFile and language implementer APIs for loading dynamic language libraries (see LoadFile's side note). The path argument needs to match a ScriptSource's Path property because it is the key to finding the ScriptScope. Hosts need to make sure they create ScriptSources (see ScriptHost as well as methods on ScriptEngine) with their Path properties set appropriately (for example, resolving relative paths to canonical full pathnames, FileInfo.FullPath for standard .NET resolved paths).

GetScope is primarily useful for tools that need to map files to their execution scopes when the tool did not create the scope. For example, an editor and interpreter tool might execute a file, Foo, that imports or requires a file, Bar. The editor end user might later open the Bar and want to execute expressions in its context. The tool would need to find Bar's ScriptScope for setting the appropriate context in its interpreter window. This method helps with this scenario.

Languages may return null. For example, Ruby's require expression executes a file's contents in the calling scope. Since Ruby does not have a distinct scope in which the file executed in this case, they return null for such files.

Signature:

public ScriptScope GetScope(string path)

<h3 id="operations-property-1">4.3.8 Operations Property</h3>

This property returns a default ObjectOperations for the engine. ObjectOperations lets you perform various operations on objects. Because an ObjectOperations object caches rules for the types of objects and operations it processes, using the default ObjectOperations for many objects could degrade the caching benefits. Eventually the cache for some operations could degrade to a point where ObjectOperations stops caching and does a full search for an implementation of the requested operation for the given objects. For simple hosting situations, this is sufficient behavior.

See CreateOperations for alternatives.

Signature:

public ObjectOperations Operations { get; }

<h3 id="createoperations-methods-1">4.3.9 CreateOperations Methods</h3>

These methods return a new ObjectOperations object. See the Operations property for why you might want to call this.

There currently is little guidance on how to choose when to create new ObjectOperations objects. However, there is a simple heuristic. If you were to perform some operations over and over on the same few types of objects, it would be advantageous to create an ObjectOperations just for use with those few cases. If you perform different operations with many types of objects just once or twice, you can use the default instance provided by the ObjectOperations property.

Signature:

public ObjectOperations CreateOperations()

public ObjectOperations CreateOperations(ScriptScope Scope)

The overload that takes a ScriptScope supports pretty advanced or subtle scenarios. It allows you to get an ObjectOperations that uses the execution context built up in a ScriptScope. For example, the engine affiliated with the scope could be IronPython, and you could execute code that did an "import clr" or "from \_\_future\_\_ import true\_division". These change execution behaviors within that ScriptScope. If you obtained objects from that scope or executing expressions in that scope, you may want to operate on those objects with the same execution behaviors; however, you generally do not need to worry about these subtleties for typical object interactions.

<h3 id="createscriptsourcefromstring-methods">4.3.10 CreateScriptSourceFromString Methods</h3>

These methods return ScriptSource objects from string contents. These are factory methods for creating ScriptSources with this language binding.

The default SourceCodeKind is AutoDetect.

The ScriptSource's Path property defaults to null. When path is non-null, if executing the resulting ScriptSource would create a ScriptScope, then path should map to the ScriptScope via GetScope.

Signatures:

public ScriptSource CreateScriptSourceFromString

(string expression)

public ScriptSource CreateScriptSourceFromString

(string expression, string path)

public ScriptSource CreateScriptSourceFromString

(string code, SourceCodeKind kind)

public ScriptSource CreateScriptSourceFromString

(string code, string path, SourceCodeKind kind)

<h3 id="createscriptsourcefromfile-methods">4.3.11 CreateScriptSourceFromFile Methods</h3>

These methods return ScriptSource objects from file contents. These are factory methods for creating ScriptSources with this language binding. The path's extension does NOT have to be registered or valid for the engine. This method does NOT go through the PlatformAdaptationLayer to open the file; it goes directly to the file system via .NET.

The default SourceCodeKind is File.

The ScriptSource's Path property will be the path argument, which needs to be in some canonical form according to the host if the host is using GetScope to find the source's execution context later.

Creating the ScriptSource does not open the file. Any exceptions that will be thrown on opening or reading the file happen when you use the ScriptSource to execute or compile the source.

The encoding defaults to the platform encoding.

Signatures:

public ScriptSource CreateScriptSourceFromFile(string path)

public ScriptSource CreateScriptSourceFromFile

(string path, System.Text.Encoding encoding)

public ScriptSource CreateScriptSourceFromFile

(string path, System.Text.Encoding encoding,

SourceCodeKind kind)

<h3 id="createscriptsource-methods">4.3.12 CreateScriptSource Methods</h3>

These methods returns a ScriptSource based on a CodeDom object or content providers. This is a factory method for creating a ScriptSources with this language binding.

public ScriptSource CreateScriptSource

(StreamContentProvider contentProvider, string path)

public ScriptSource CreateScriptSource

(StreamContentProvider contentProvider, string path,

System.Text.Encoding encoding)

public ScriptSource CreateScriptSource

(StreamContentProvider contentProvider, string path,

System.Text.Encoding encoding, SourceCodeKind kind)

public ScriptSource CreateScriptSource

(TextContentProvider contentProvider, string path,

SourceCodeKind kind)

public ScriptSource CreateScriptSource

(System.CodeDom.CodeObject code,

string path, SourceCodeKind kind)

public ScriptSource CreateScriptSource(CodeObject content)

public ScriptSource CreateScriptSource(CodeObject content,

string path)

public ScriptSource CreateScriptSource(CodeObject content,

SourceCodeKind kind)

public ScriptSource CreateScriptSource

(System.CodeDom.CodeObject code,

string path, SourceCodeKind kind)

The method taking a TextContentProvider lets you supply input from Unicode strings or stream readers. This could be useful for implementing a TextReader over internal host data structures, such as an editor's text representation.

The method taking a StreamContentProvider lets you supply binary (sequence of bytes) stream input. This is useful when opening files that may contain language-specific encodings that are marked in the first few bytes of the file's contents. There is a default StreamContentProvider used internally if you call CreateScriptSourceFromFile. The encoding defaults to the platform encoding if the language doesn't recognize some other encoding (for example, one marked in the file's first few bytes).

The method taking a System.CodeDom.CodeObject, and the expected CodeDom support is extremely minimal for syntax-independent expression of semantics. Languages may do more, but hosts should only expect CodeMemberMethod support, and only sub nodes consisting of the following:

- CodeSnippetStatement

- CodeSnippetExpression

- CodePrimitiveExpression

- CodeMethodInvokeExpression

- CodeExpressionStatement (for holding MethodInvoke)

This support exists primarily for ASP.NET pages that contain snippets of DLR languages, and these requirements were very limited. When the CodeObject argument does not match this specification, you will get a type cast error, but if the language supports more options, you could get different errors per engine.

The path argument in all cases is a unique ID that the host may use to retrieve the scope in which the source executes via Engine.GetScope.

<h3 id="createscope-method-1">4.3.13 CreateScope Method</h3>

This method returns a new ScriptScope with this engine as the default language for the scope.

Signatures:

public ScriptScope CreateScope()

public ScriptScope CreateScope(IDynamicMetaObjectProvider globals)

The globals parameter lets you supply the dictionary of the scope so that you can provide late bound values for some name lookups. The easiest way to supply your own dictionary is to use ExpandoObject or derive from DynamicObject .

<h3 id="getservice-method">4.3.14 GetService Method</h3>

This method returns a language-specific service. It provides a point of extensibility for a language implementation to offer more functionality than the standard engine members discussed here. If the specified service is not available, this returns null.

Signature:

public ServiceType GetService&lt;ServiceType&gt;(params object\[\] args)

where ServiceType : class

The following are services expected to be supported:

<table>
<thead>
<tr class="header">
<th>ExceptionOperations</th>
<th>This duplicates some members of Exception and can return a string in the style of this engine's language to describe the exception argument.</th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td>TokenCategorizer</td>
<td><p>This is for building tools that want to scan languages and get token info, such as colorization categories.</p>
<p><em>This type will change and be spec'ed external to this document eventually, see the section below for this type.</em></p></td>
</tr>
<tr class="even">
<td>OptionsParser</td>
<td><p>This can parse a command shell (cmd.exe) style command line string. Hosts that are trying to be an interactive console or incorporate standard command line switches of a language's console can get the engine's command line parser.</p>
<p><em>This is a place holder for DLR v2. Its design will definitely change. We have a big open issue to redesign language and DLR support for building interactive UI, interpreters, tools, etc., with some common support around command lines and consoles.</em></p></td>
</tr>
<tr class="odd">
<td>CommandLine</td>
<td><p>is a helper object for parsing and processing interactive console input, maintaining a history of input, etc.</p>
<p><em>This is a place holder for DLR v2. Its design will definitely change. We have a big open issue to redesign language and DLR support for building interactive UI, interpreters, tools, etc., with some common support around command lines and consoles.</em></p></td>
</tr>
<tr class="even">
<td>ScriptConsole</td>
<td><p>This is a helper object for the UI of an interpreter, how output is displayed and how we get input. If the language does not implement a ScriptConsole, there is a default Console object they can return.</p>
<p><em>This is a place holder for DLR v2. Its design will definitely change. We have a big open issue to redesign language and DLR support for building interactive UI, interpreters, tools, etc., with some common support around command lines and consoles. Need to distinguish this and CommandLine.</em></p></td>
</tr>
</tbody>
</table>

<h3 id="setup-property-1">4.3.15 Setup Property</h3>

This property returns a read-only LanguageSetup describing the configuration used to instantiate this engine.

Signature:

public LanguageSetup Setup { get; }

<h3 id="getcompileroptions-method">4.3.16 GetCompilerOptions Method</h3>

This method returns the compiler options object for the engine's language. The overload that takes a ScriptScope returns options that represent any accrued imperative options state from the scope (for example, "from futures import truedivision" in python). To operate on the options before passing them to ScriptSource.Compile, for example, you may need to cast the result to the documented subtype of CompilerOptions for the language you're manipulating.

If scope is null, this throws an ArgumentNullException.

Signatures:

public CompilerOptions GetCompilerOptions()

public CompilerOptions GetCompilerOptions(ScriptScope scope)

CompilerOptions type will likely change by the time the DLR Hosting APIs move into the .NET libraries, possibly becoming Dictionary&lt;str,obj&gt;.

<h3 id="getsearchpaths-method">4.3.17 GetSearchPaths Method</h3>

This method returns the search paths used by the engine for loading files when a script wants to import or require another file of code. These are also the paths used by ScriptRuntime.UseFile.

These paths do not affect ScriptRuntime.ExecuteFile. The ScriptHost's PlatformAdaptationLayer (or the default's direct use of .NET file APIs) controls partial file name resolution for ExecuteFile.

Signature:

public ICollection&lt;string&gt; GetSearchPaths ()

<h3 id="setsearchpaths-method">4.3.18 SetSearchPaths Method</h3>

This method sets the search paths used by the engine for loading files when a script wants to import or require another file of code. Setting these paths affects ScriptRuntime.UseFile.

These paths do not affect ScriptRuntime.ExecuteFile. The ScriptHost's PlatformAdaptationLayer (or the default's direct use of .NET file APIs) controls partial file name resolution for ExecuteFile.

Signature:

public void SetSearchPaths (ICollection&lt;string&gt; paths)

<h3 id="languageversion-property">4.3.19 LanguageVersion Property</h3>

This property returns the language's version.

Signature:

public System.Version LanguageVersion { get; }

<h2 id="scriptsource-class">4.4 ScriptSource Class</h2>

ScriptSource represents source code and offer a variety of ways to execute or compile the source. You can get ScriptSources from factory methods on ScriptEngine, and ScriptSources are tied to the engine that created them. The associated engine provides the execution and compilation semantics for the source.

ScriptSources have properties that direct the parsing of and report aspects of the source. For example, the source could be marked as being an expression or a statement, for language that need to distinguish expressions and statements semantically for how to parse them. The code could be marked as being interactive, which means the language's parser should handle standard interpreter affordances the language might support (for example, Python's "\_" variable or VB's "?" syntax).

ScriptSources also have a Path property. This is mostly useful for those marked as being a file. The Path is the key for engines recognizing ScriptSources they have seen before so that they do not repeatedly load files when load-once semantics should apply (see ScriptEngine.LoadFile). The Path also helps the engine find the ScriptScope the file executed in (see ScriptEngine.GetScope), which is useful for some tool host scenarios. The host defines what a canonical representation of a path is. The host needs to set the path to the same string when it intends for ScriptSources to match for the purposes of the above functions on ScriptEngine.

You can create ScriptSource objects with factory methods on ScriptEngine.

Note, members that take or return ObjectHandles are not present on Silverlight.

<h3 id="class-summary-3">4.4.1 Class Summary</h3>

public sealed class ScriptSource : MarshalByRefObject {

internal ScriptSource()

public string Path { get; }

public SourceCodeKind Kind { get;}

public ScriptCodeParseResult GetCodeProperties ()

public ScriptCodeParseResult GetCodeProperties

(CompilerOptions options)

public ScriptEngine Engine { get; }

public CompiledCode Compile()

  public CompiledCode Compile(ErrorListener sink)

public CompiledCode Compile(CompilerOptions options)

  public CompiledCode Compile(CompilerOptions options,

ErrorListener sink)

public object Execute()

public object Execute(ScriptScope scope)

public ObjectHandle ExecuteAndWrap ()

public ObjectHandle ExecuteAndWrap (ScriptScope scope)

public T Execute&lt;T&gt;()

public T Execute&lt;T&gt;(ScriptScope scope)

public int ExecuteProgram()

public ScriptCodeReader GetReader()

public Encoding DetectEncoding()

// line/file mapping:

public string GetCode()

public string GetCodeLine(int line)

public string\[\] GetCodeLines(int start, int count)

public SourceSpan MapLine(SourceSpan span)

public SourceLocation MapLine(SourceLocation loc)

public int MapLine(int line)

public string MapLineToFile(int line)

<h3 id="path-property">4.4.2 Path Property</h3>

This property returns the identifier for this script source. In many cases the Path doesn't matter. It is mostly useful for file ScriptSources. The Path is the key for engines to recognize ScriptSources they have seen before so that they do not repeatedly load files when load-once semantics should apply. The Path also helps the engine find the ScriptScope the file executed in, which is useful for some tool host scenarios (see ScriptEngine.GetScope).

The Path is null if not set explicitly on construction. The path has the value the ScriptSource was created with. In the case of relative file paths, for example, the DLR does not convert them to absolute or canonical representations.

Signature:

public string Path { get; }

<h3 id="kind-property">4.4.3 Kind Property</h3>

This property returns the kind of source this ScriptSource represents. This property is a hint to the ScriptEngine how to parse the code ScriptSource (as an expression, statement, whole file, etc.).

If you're unsure, File can be used to direct the language to generally parse the code. For languages that are expression-based, they should interpret Statement as Expression.

Signature:

public SourceCodeKind Kind { get;}

<h3 id="getcodeproperties-methods">4.4.4 GetCodeProperties Methods</h3>

This method returns the properties of the code to support tools. The values indicate the state of parsing the source relative to completeness, or whether the source is complete enough to execute.

Signature:

public ScriptCodeParseResult GetCodeProperties ()

public ScriptCodeParseResult GetCodeProperties

(CompilerOptions options)

CompilerOptions type will likely change by the time the DLR Hosting APIs move into the .NET libraries, possibly becoming Dictionary&lt;str,obj&gt;.

<h3 id="engine-property-1">4.4.5 Engine Property</h3>

This property returns the language engine associated with this ScriptSource. There is always a language tied to the source for convenience. Also, we do not think it is useful to support having a piece of code that could perhaps be parsed by multiple languages.

Signature:

public ScriptEngine Engine { get; }

<h3 id="compile-methods">4.4.6 Compile Methods</h3>

These methods compile the source and return a CompileCode object that can be executed repeatedly in its default scope or in other scopes without having to recompile the code.

Each call to Compile returns a new CompiledCode object. Each call to Compile always calls on its content provider to get sources, and the default file content provider always re-opens the file and reads its contents.

Signatures

public CompiledCode Compile()

    public CompiledCode Compile(ErrorListener sink)

public CompiledCode Compile(CompilerOptions options)

    public CompiledCode Compile(CompilerOptions options,

ErrorListener sink)

If any arguments are null, these throw ArgumentNullExceptions.

If you supply an error listener, and there were errors, these methods return null. Otherwise, it leaves any raised exceptions unhandled.

These methods do not take a ScriptScope to compile against. That would prevent compilation from choosing optimized scope implementations. You can always execute compiled code against any scope (see Execute\* methods).

CompilerOptions type will likely change by the time the DLR Hosting APIs move into the .NET libraries, possibly becoming Dictionary&lt;str,obj&gt;.

<h3 id="execute-methods-1">4.4.7 Execute\* Methods</h3>

These methods execute the source code and return a result in various ways. There are complementary overloads that take a ScriptScope and those that do not. The overloads with no arguments create a new scope for each execution. These methods throw the scope away and use it for side effects only, returning the result in the same way the complementary overload does.

These methods always execute the ScriptSource. Even when the source is a file, and the associated engine's language has an execute-at-most-once mechanism, these methods always execute the source contents.

Each call to Execute always calls on its content provider to get sources, and the default file content provider always re-opens the file and reads its contents.

Signatures:

public object Execute()

public object Execute(ScriptScope scope)

public ObjectHandle ExecuteAndWrap ()

public ObjectHandle ExecuteAndWrap (ScriptScope scope)

public T Execute&lt;T&gt;()

public T Execute&lt;T&gt;(ScriptScope scope)

public int ExecuteProgram()

Execute returns an object that is the resulting value of running the code. When the ScriptSource is a file or statement, the language decides what is an appropriate value to return. Some languages return the value produced by the last expression or statement, but languages that are not expression based may return null.

ExecuteAndWrap returns an ObjectHandle for use when the engine and/or scope are remote.

Execute&lt;T&gt; returns the result as the specified type, using the associated engine's Operations.ConvertTo&lt;T&gt; method. If this method cannot convert to the specified type, then it throws an exception.

ExecuteProgram runs the source as though it were launched from an OS command shell and returns a process exit code indicating the success or error condition of executing the code. Each time this method is called it creates a fresh ScriptScope in which to run the source, and if you were to use ScriptEngine.GetScope, you'd get whatever last ScriptScope the engine created for the source.

<h3 id="getreader-method">4.4.8 GetReader Method</h3>

This method returns a derived type of TextReader that is bound to this ScriptSource. Every time you call this method you get a new ScriptCodeReader reset to beginning parsing state, and no two instances interfere with each other.

Signature:

public ScriptCodeReader GetReader()

<h3 id="detectencoding-method">4.4.9 DetectEncoding Method</h3>

This method returns the encoding for the source. The language associated with the source has the chance to read the beginning of the file if it has any special handling for encodings based on the first few bytes of the file. This method could return an encoding different than what the source was created with.

Signature:

public Encoding DetectEncoding()

<h3 id="getcode-method">4.4.10 GetCode Method</h3>

This method returns all the source code contents as a string. The result may share storage with the string passed to create the ScriptSource.

Each call to GetCode always calls on its content provider to get sources, and the default file content provider always re-opens the file and reads its contents.

Signature:

public string GetCode()

<h3 id="getcodeline-methods">4.4.11 GetCodeLine\* Methods</h3>

These methods return a string (or strings) for the line (or lines) indexed. Count is one-based. The count argument can be greater than the number of lines. The start argument cannot be zero or negative.

The line and count arguments can cause indexing to go beyond the end of the source. GetCodeLine returns null in that case. GetCodeLines returns strings only for existing lines and does not throw an exception or include nulls in the array. If start is beyond the end, the result is an empty array.

Signatures:

public string GetCodeLine(int line)

public string\[\] GetCodeLines(int start, int count)

<h3 id="mapline-methods">4.4.12 MapLine Methods</h3>

These methods map physical line numbers to virtual line numbers for reporting errors or other information to users. These are useful for languages that support line number directives for their parsers and error reporting.

Signatures:

public SourceSpan MapLine(SourceSpan span)

public SourceLocation MapLine(SourceLocation loc)

public int MapLine(int line)

<h3 id="maplinetofile-method">4.4.13 MapLineToFile Method</h3>

This method maps a physical line number to a .NET CLR pdb or file with symbol information in it. The result is an absolute path or relative path that resolves in a standard .NET way to the appropriate file.

Signature:

public string MapLineToFile(int line)

<h2 id="compiledcode-class">4.5 CompiledCode Class</h2>

CompiledCode represents code that has been compiled to execute repeatedly without having to compile it each time, and it represents the default ScriptScope the code runs in. The default scope may have optimized variable storage and lookup for the code. You can always execute the code in any ScriptScope if you need it to execute in a clean scope each time, or you want to accumulate side effects from the code in another scope.

You can get CompiledCode from Compile methods on ScriptSource. CompiledCode objects have an internal reference to the engine that produced them. Because they have a default scope in which to execute, and the use for CompiledCode objects is to execute them, they have several execute methods.

Note, members that take or return ObjectHandles are not present on Silverlight.

<h3 id="class-summary-4">4.5.1 Class Summary</h3>

public class CompiledCode : MarshalByRefObject {

internal CompiledCode()

public ScriptScope DefaultScope { get; }

public ScriptEngine Engine { get; }

public object Execute()

public object Execute(ScriptScope scope)

public ObjectHandle ExecuteAndWrap()

public ObjectHandle ExecuteAndWrap(ScriptScope scope)

public T Execute&lt;T&gt;()

public T Execute&lt;T&gt;(ScriptScope scope)

<h3 id="defaultscope-property">4.5.2 DefaultScope Property</h3>

This property returns the default ScriptScope in which the code executes. This allows you to extract variable values after executing the code or insert variable bindings before executing the code.

Signature:

public ScriptScope DefaultScope { get; }

<h3 id="engine-property-2">4.5.3 Engine Property</h3>

This property returns the engine that produced the compiled code.

Signature:

public ScriptEngine Engine { get; }

<h3 id="execute-methods-2">4.5.4 Execute\* Methods</h3>

These methods execute the compiled code in a variety of ways. Half of the overloads do the same thing as their complement, one executes in the default scope while the other takes a ScriptScope in which to execute the code. If invoked on null, this throws an ArgumentNullException.

Signatures:

public object Execute()

public object Execute(ScriptScope scope)

public ObjectHandle ExecuteAndWrap()

public ObjectHandle ExecuteAndWrap(ScriptScope scope)

public T Execute&lt;T&gt;()

public T Execute&lt;T&gt;(ScriptScope scope)

ExecuteAndWrap returns an ObjectHandle for use when the engine and/or scope are remote.

Execute&lt;T&gt; returns the result as the specified type, using the engine's Operations.ConvertTo&lt;T&gt; method. If this method cannot convert to the specified type, then it throws an exception.

<h2 id="objectoperations-class">4.6 ObjectOperations Class</h2>

This utility class provides operations on objects. The operations work on objects emanating from a ScriptRuntime or straight up .NET static objects. The behaviors of this class are language-specific, depending on which language owns the instance you're using.

You get ObjectOperations objects from ScriptEngines. The operations have a language-specific behavior determined by the engine from which you got the ObjectOperations object. For example, calling GetMember on most objects to get the "\_\_dict\_\_" member using an ObjectOperations obtained from an IronPython ScriptEngine will return the object's dictionary of members. However, using an ObjectOperations obtained from an IronRuby engine, would raise a member missing exception.

The reason ObjectOperations is a utility class that is not static is that the instances provide a context of caching for performing the operations. If you were to perform several operations over and over on the same few objects, it would be advantageous to create a special ObjectOperations just for use with those few objects. If you perform different operations with many objects just once or twice, you can use the default instance provided by the ScriptEngine.

Half of the methods do the same thing as their complement, one works with objects of type Object while the other works with ObjectHandles. We need the overloads for clear method selection and to allow for an ObjectHandle to be treated as Object should that be interesting.

You obtain ObjectOperation objects from ScriptEngines' Operations property and CreateOperations method.

Note, members that take or return ObjectHandles are not present on Silverlight.

<h3 id="class-summary-5">4.6.1 Class Summary</h3>

public sealed class ObjectOperations : MarshalByRefObject {

public ScriptEngine Engine { get; }

public ObjectHandle Add(ObjectHandle self, ObjectHandle other)

public Object Add(Object self, Object other)

public Object BitwiseAnd(Object self, Object other)

public ObjectHandle BitwiseAnd(ObjectHandle self,

ObjectHandle other)

public ObjectHandle BitwiseOr(ObjectHandle self,

ObjectHandle other)

public Object BitwiseOr(Object self, Object other)

public Boolean ContainsMember(ObjectHandle obj, String name)

public Boolean ContainsMember(Object obj, String name,

Boolean ignoreCase)

public Boolean ContainsMember(Object obj, String name)

public T ConvertTo&lt;T&gt;(Object obj)

public ObjectHandle ConvertTo&lt;T&gt;(ObjectHandle obj)

public Object ConvertTo(Object obj, Type type)

public ObjectHandle ConvertTo(ObjectHandle obj, Type type)

public ObjectHandle CreateInstance(ObjectHandle obj,

params ObjectHandle\[\] parameters)

public ObjectHandle CreateInstance(ObjectHandle obj,

params Object\[\] parameters)

public Object CreateInstance(Object obj,

params Object\[\] parameters)

public Object Divide(Object self, Object other)

public ObjectHandle Divide(ObjectHandle self,

ObjectHandle other)

public Object DoOperation(ExpressionType operation,

Object target)

public TResult DoOperation&lt;TTarget, TResult&gt;

(ExpressionType operation, TTarget target)

public Object DoOperation

(ExpressionType operation, Object target, Object other)

public TResult DoOperation&lt;TTarget, TOther, TResult&gt;

(ExpressionType operation, TTarget target, TOther other)

public Object DoOperation(ExpressionType op,

ObjectHandle target)

public ObjectHandle DoOperation

(ExpressionType op, ObjectHandle target, ObjectHandle other)

public Boolean Equal(Object self, Object other)

public Boolean Equal(ObjectHandle self, ObjectHandle other)

public Object ExclusiveOr(Object self, Object other)

public ObjectHandle ExclusiveOr(ObjectHandle self,

ObjectHandle other)

public T ExplicitConvertTo&lt;T&gt;(Object obj)

public Object ExplicitConvertTo(Object obj, Type type)

public ObjectHandle ExplicitConvertTo(ObjectHandle obj, Type type)

public ObjectHandle ExplicitConvertTo&lt;T&gt;(ObjectHandle obj)

public T ImplicitConvertTo&lt;T&gt;(Object obj)

public Object ImplicitConvertTo(Object obj, Type type)

public ObjectHandle ImplicitConvertTo(ObjectHandle obj, Type type)

public ObjectHandle ImplicitConvertTo&lt;T&gt;(ObjectHandle obj)

public String Format(Object obj)

public String Format(ObjectHandle obj)

public IList&lt;System.String&gt; GetCallSignatures(ObjectHandle obj)

public IList&lt;System.String&gt; GetCallSignatures(Object obj)

public String GetDocumentation(Object obj)

public String GetDocumentation(ObjectHandle obj)

public T GetMember&lt;T&gt;(ObjectHandle obj, String name)

public T GetMember&lt;T&gt;(Object obj, String name, Boolean ignoreCase)

public Object GetMember(Object obj, String name)

public Object GetMember(Object obj, String name,

Boolean ignoreCase)

public ObjectHandle GetMember(ObjectHandle obj, String name)

public T GetMember&lt;T&gt;(Object obj, String name)

public IList&lt;System.String&gt; GetMemberNames(ObjectHandle obj)

public IList&lt;System.String&gt; GetMemberNames(Object obj)

public Boolean GreaterThan(Object self, Object other)

public Boolean GreaterThan(ObjectHandle self, ObjectHandle other)

public Boolean GreaterThanOrEqual(Object self, Object other)

public Boolean GreaterThanOrEqual(ObjectHandle self,

ObjectHandle other)

public ObjectHandle Invoke(ObjectHandle obj,

params ObjectHandle\[\] parameters)

public ObjectHandle Invoke(ObjectHandle obj,

params Object\[\] parameters)

public Object Invoke(Object obj, params Object\[\] parameters)

public Object InvokeMember(Object obj, String memberName,

params Object\[\] parameters)

public Boolean IsCallable(Object obj)

public Boolean IsCallable(ObjectHandle obj)

public ObjectHandle LeftShift(ObjectHandle self,

ObjectHandle other)

public Object LeftShift(Object self, Object other)

public Boolean LessThan(Object self, Object other)

public Boolean LessThan(ObjectHandle self, ObjectHandle other)

public Boolean LessThanOrEqual(ObjectHandle self,

ObjectHandle other)

public Boolean LessThanOrEqual(Object self, Object other)

public ObjectHandle Modulo(ObjectHandle self, ObjectHandle other)

public Object Modulo(Object self, Object other)

public ObjectHandle Multiply(ObjectHandle self, ObjectHandle other)

public Object Multiply(Object self, Object other)

public Boolean NotEqual(Object self, Object other)

public Boolean NotEqual(ObjectHandle self, ObjectHandle other)

public Object Power(Object self, Object other)

public ObjectHandle Power(ObjectHandle self, ObjectHandle other)

public Boolean RemoveMember(Object obj, String name)

public Boolean RemoveMember(ObjectHandle obj, String name)

public Boolean RemoveMember(Object obj, String name,

Boolean ignoreCase)

public ObjectHandle RightShift(ObjectHandle self,

ObjectHandle other)

public Object RightShift(Object self, Object other)

public void SetMember(Object obj, String name, Object value,

Boolean ignoreCase)

public void SetMember(ObjectHandle obj, String name,

ObjectHandle value)

public void SetMember&lt;T&gt;(Object obj, String name, T value,

Boolean ignoreCase)

public void SetMember&lt;T&gt;(Object obj, String name, T value)

public void SetMember&lt;T&gt;(ObjectHandle obj, String name, T value)

public void SetMember(Object obj, String name, Object value)

public ObjectHandle Subtract(ObjectHandle self, ObjectHandle other)

public Object Subtract(Object self, Object other)

public Boolean TryConvertTo&lt;T&gt;(ObjectHandle obj,

out ObjectHandle result)

public Boolean TryConvertTo(Object obj, Type type,

out Object result)

public Boolean TryConvertTo&lt;T&gt;(Object obj, out T result)

public Boolean TryConvertTo(ObjectHandle obj, Type type,

out ObjectHandle result)

public Boolean TryExplicitConvertTo&lt;T&gt;(Object obj, out T result)

public Boolean TryExplicitConvertTo&lt;T&gt;(ObjectHandle obj,

out ObjectHandle result)

public Boolean TryExplicitConvertTo(ObjectHandle obj, Type type,

out ObjectHandle result)

public Boolean TryExplicitConvertTo(Object obj, Type type,

out Object result)

public Boolean TryImplicitConvertTo&lt;T&gt;(Object obj, out T result)

public Boolean TryImplicitConvertTo&lt;T&gt;(ObjectHandle obj,

out ObjectHandle result)

public Boolean TryImplicitConvertTo(ObjectHandle obj, Type type,

out ObjectHandle result)

public Boolean TryImplicitConvertTo(Object obj, Type type,

out Object result)

public Boolean TryGetMember(Object obj, String name,

Boolean ignoreCase, out Object value)

public Boolean TryGetMember(ObjectHandle obj, String name,

out ObjectHandle value)

public Boolean TryGetMember(Object obj, String name,

out Object value)

public T Unwrap&lt;T&gt;(ObjectHandle obj)

<h3 id="engine-property-3">4.6.2 Engine Property</h3>

This property returns the engine bound to this ObjectOperations. The engine binding provides the language context or semantics applied to each requested operation.

Signature:

public ScriptEngine Engine { get; }

<h3 id="iscallable-methods">4.6.3 IsCallable Methods</h3>

These methods returns whether the object is callable. Languages should return delegates when fetching the value of variables or executing expressions that result in callable objects. However, sometimes you'll get objects that are callable, but they are not wrapped in a delegate. Note, even if this method returns true, a call may fail due to incorrect number of arguments or incorrect types of arguments.

Signatures:

public bool IsCallable(object obj)

public bool IsCallable(ObjectHandle obj)

<h3 id="invoke-methods">4.6.4 Invoke Methods</h3>

These methods invoke objects that are callable. In general you should not need to call these methods. Languages should return delegates when fetching the value of variables or executing expressions that result in callable objects. However, sometimes you'll get objects that are callable, but they are not wrapped in a delegate. If you're calling an object multiple times, you can use ConvertTo to get a strongly typed delegate that you can call more efficiently. You'll also need to use Invoke for objects that are remote.

If any obj arguments are null, then these throw an ArgumentNullException.

Signatures:

public ObjectHandle Invoke(ObjectHandle obj,

params ObjectHandle\[\] parameters)

public ObjectHandle Invoke(ObjectHandle obj,

params Object\[\] parameters)

public Object Invoke(Object obj, params Object\[\] parameters)

<h3 id="invokemember-method">4.6.5 InvokeMember Method</h3>

This method invokes callable members from objects.

If the obj argument is null, then this throws an ArgumentNullException.

Signatures:

public Object InvokeMember(Object obj, String memberName,

params Object\[\] parameters)

<h3 id="createinstance-methods">4.6.6 CreateInstance Methods</h3>

These methods create objects when the input object can be instantiated.

If any obj arguments are null, then these throw an ArgumentNullException.

Signatures:

public ObjectHandle CreateInstance(ObjectHandle obj,

params ObjectHandle\[\] parameters)

public ObjectHandle CreateInstance(ObjectHandle obj,

params Object\[\] parameters)

public Object CreateInstance(Object obj,

params Object\[\] parameters)

<h3 id="getmember-methods">4.6.7 GetMember\* Methods</h3>

These methods return a named member of an object.

The generic overloads do not modify obj to convert to the requested type. If they cannot perform the requested conversion to the concrete type, then they throw a NotSupportedException. You can use Unwrap&lt;T&gt; after ConvertTo&lt;T&gt; on ObjectHandle to get a local T for the result. The generic overloads use language-specific conversions (based on the default language in the Engine property), like ConvertTo&lt;T&gt;.

If the specified member does not exist, or if it is write-only, then these throw exceptions.

Signatures:

public T GetMember&lt;T&gt;(ObjectHandle obj, String name)

public T GetMember&lt;T&gt;(Object obj, String name, Boolean ignoreCase)

public Object GetMember(Object obj, String name)

public Object GetMember(Object obj, String name,

Boolean ignoreCase)

public ObjectHandle GetMember(ObjectHandle obj, String name)

public T GetMember&lt;T&gt;(Object obj, String name)

<h3 id="trygetmember-methods">4.6.8 TryGetMember Methods</h3>

These methods try to get a named member of an object. They return whether name was a member of obj and set the out value to name's value. If the name was not a member of obj, then this method sets value to null.

If obj or name is null, then these throw an ArgumentNullException.

Signatures:

public Boolean TryGetMember(Object obj, String name,

Boolean ignoreCase, out Object value)

public Boolean TryGetMember(ObjectHandle obj, String name,

out ObjectHandle value)

public Boolean TryGetMember(Object obj, String name,

out Object value)

<h3 id="containsmember-methods">4.6.9 ContainsMember Methods</h3>

These methods return whether the name is a member of obj.

Signatures:

public Boolean ContainsMember(ObjectHandle obj, String name)

public Boolean ContainsMember(Object obj, String name,

Boolean ignoreCase)

public Boolean ContainsMember(Object obj, String name)

<h3 id="removemember-methods">4.6.10 RemoveMember Methods</h3>

These methods remove name from obj so that it is no longer a member of obj. If the object or the language binding of this ObjectOperations allows read-only or non-removable members, and name identifies such a member, then it is undefined what happens. Languages vary on whether this is a no-op or exceptional.

If any arguments are null, then these throw an ArgumentNullException.

Signatures:

public Boolean RemoveMember(Object obj, String name)

public Boolean RemoveMember(ObjectHandle obj, String name)

public Boolean RemoveMember(Object obj, String name,

Boolean ignoreCase)

<h3 id="setmember-methods">4.6.11 SetMember Methods</h3>

These members set the value of a named member of an object. There are generic overloads that can be used to avoid boxing values and casting of strongly typed members.

If the object or the language binding of this ObjectOperations supports read-only members, and name identifies such a member, then these methods throw a NotSupportedException.

If any arguments are null, then these throw an ArgumentNullException.

Signatures:

public void SetMember(Object obj, String name, Object value,

Boolean ignoreCase)

public void SetMember(ObjectHandle obj, String name,

ObjectHandle value)

public void SetMember&lt;T&gt;(Object obj, String name, T value,

Boolean ignoreCase)

public void SetMember&lt;T&gt;(Object obj, String name, T value)

public void SetMember&lt;T&gt;(ObjectHandle obj, String name, T value)

public void SetMember(Object obj, String name, Object value)

<h3 id="convertto-methods">4.6.12 ConvertTo\* Methods</h3>

These methods convert an object to the requested type using language-specific (based on the default language in the Engine property) conversions. These may be implicit only, or include explicit conversion too. The conversions do not modify obj. Obj may be returned if it is already the requested type. You can use Unwrap&lt;T&gt; after ConvertTo&lt;T&gt; on ObjectHandle to get a local T for the result.

If any of the arguments is null, then these throw an ArgumentNullException.

If these methods cannot perform the requested conversion, then they throw a NotSupportedException.

Signatures:

public T ConvertTo&lt;T&gt;(Object obj)

public ObjectHandle ConvertTo&lt;T&gt;(ObjectHandle obj)

public Object ConvertTo(Object obj, Type type)

public ObjectHandle ConvertTo(ObjectHandle obj, Type type)

<h3 id="tryconvertto-methods">4.6.13 TryConvertTo\* Methods</h3>

These methods try to convert an object to the requested type language-specific (based on the default language in the Engine property) conversions. These may be implicit only, or include explicit conversion too. The conversions do not modify obj. They return whether they could perform the conversion and set the out result parameter. If the methods could not perform the conversion, then they set result to null.

You can use Unwrap&lt;T&gt; after calling overloads on ObjectHandle to get a local T for the result.

If they cannot perform the conversion to the requested type, then they throw a NotSupportedException.

If obj is null, then these throw an ArgumentNullException.

Signatures:

public Boolean TryConvertTo&lt;T&gt;(ObjectHandle obj,

out ObjectHandle result)

public Boolean TryConvertTo(Object obj, Type type,

out Object result)

public Boolean TryConvertTo&lt;T&gt;(Object obj, out T result)

public Boolean TryConvertTo(ObjectHandle obj, Type type,

out ObjectHandle result)

<h3 id="explicitconvertto-methods">4.6.14 ExplicitConvertTo\* Methods</h3>

These methods convert an object to the requested type using explicit conversions, which may be lossy. Otherwise these methods are the same as the ConvertTo\* methods.

public T ExplicitConvertTo&lt;T&gt;(Object obj)

public Object ExplicitConvertTo(Object obj, Type type)

public ObjectHandle ExplicitConvertTo(ObjectHandle obj, Type type)

public ObjectHandle ExplicitConvertTo&lt;T&gt;(ObjectHandle obj)

<h3 id="tryexplicitconvertto-methods">4.6.15 TryExplicitConvertTo\* Methods</h3>

These methods try to convert an object to the request type using explicit conversions, which may be lossy. Otherwise these methods are the same as TryConvertTo\* methods.

public Boolean TryExplicitConvertTo&lt;T&gt;(Object obj, out T result)

public Boolean TryExplicitConvertTo&lt;T&gt;(ObjectHandle obj,

out ObjectHandle result)

public Boolean TryExplicitConvertTo(ObjectHandle obj, Type type,

out ObjectHandle result)

public Boolean TryExplicitConvertTo(Object obj, Type type,

out Object result)

<h3 id="implicitconvertto-methods">4.6.16 ImplicitConvertTo\* Methods</h3>

These methods convert an object to the requested type using implicit conversions, which may be lossy. Otherwise these methods are the same as the ConvertTo\* methods.

public T ImplicitConvertTo&lt;T&gt;(Object obj)

public Object ImplicitConvertTo(Object obj, Type type)

public ObjectHandle ImplicitConvertTo(ObjectHandle obj, Type type)

public ObjectHandle ImplicitConvertTo&lt;T&gt;(ObjectHandle obj)

<h3 id="tryimplicitconvertto-methods">4.6.17 TryimplicitConvertTo\* Methods</h3>

These methods try to convert an object to the request type using implicit conversions, which may be lossy. Otherwise these methods are the same as TryConvertTo\* methods.

public Boolean TryImplicitConvertTo&lt;T&gt;(Object obj, out T result)

public Boolean TryImplicitConvertTo&lt;T&gt;(ObjectHandle obj,

out ObjectHandle result)

public Boolean TryImplicitConvertTo(ObjectHandle obj, Type type,

out ObjectHandle result)

public Boolean TryImplicitConvertTo(Object obj, Type type,

out Object result)

<h3 id="unwrapt-method">4.6.18 Unwrap&lt;T&gt; Method</h3>

This method unwraps the remote object reference, converting it to the specified type before returning it. If this method cannot perform the requested conversion to the concrete type, then it throws a NotSupportedException. If the requested T does not serialize back to the calling app domain, the CLR throws an exception.

Signature:

public T Unwrap&lt;T&gt;(ObjectHandle obj)

<h3 id="format-methods">4.6.19 Format Methods</h3>

These methods return a string representation of obj that is parse-able by the language. ConvertTo operations that request a string return a display string for the object that is not necessarily parse-able as input for evaluation.

Signatures:

public string Format(object obj)

public string Format(ObjectHandle obj)

<h3 id="getmembernames-methods">4.6.20 GetMemberNames Methods</h3>

These methods return an array of all the member names that obj has explicitly, determined by the language associated with this ObjectOperations. Computed or late bound member names may not be in the result.

Signatures:

public IList&lt;string&gt; GetMemberNames(object obj)

public IList&lt;string&gt; GetMemberNames(ObjectHandle obj)

<h3 id="getdocumentation-methods">4.6.21 GetDocumentation Methods</h3>

These methods return the documentation for obj. When obj is a static .NET object, this returns xml documentation comment information associated with the DLL containing obj's type. If there is no available documentation for the object, these return the empty string. Some languages do not have documentation hooks for objects, in which case they return the empty string.

Signatures:

public string GetDocumentation(object obj)

public string GetDocumentation(ObjectHandle obj)

<h3 id="getcallsignatures-methods">4.6.22 GetCallSignatures Methods</h3>

These methods return arrays of stings, each one describing a call signature that obj supports. If the object is not callable, these throw a NotSupportedException.

Signatures:

public IList&lt;string&gt; GetCallSignatures(object obj)

public IList&lt;string&gt; GetCallSignatures(ObjectHandle obj)

<h3 id="dooperation-methods">4.6.23 DoOperation\* Methods</h3>

These methods perform the specified unary and binary operations on the supplied target and other objects, returning the results. If the specified operator cannot be performed on the object or objects supplied, then these throw an exception. See the [Expression Tree spec](http://www.codeplex.com/dlr/Wiki/View.aspx?title=Docs%20and%20specs&referringTitle=Home) for information on the expected semantics of the operators.

The Hosting APIs share the ExpressionType enum with Expression Trees and the dynamic object interop protocol to specify what operation to perform. Most values overlap making a distinct enum just another concept to learn, but this enum contains values for operations used in Expression Trees that do not make sense when passed to this method (for example, Block, Try, and Throw). These methods pass the operation to the language that created the ObjectOperations object, and the language handles the ExpressionType as it sees fit. For example, IronPython only supports the following ExpressionType values:

| Add         | Subtract          | SubtractAssign     |
|-------------|-------------------|--------------------|
| And         | AddAssign         | Equal              |
| Divide      | AndAssign         | GreaterThan        |
| ExclusiveOr | DivideAssign      | GreaterThanOrEqual |
| Modulo      | ExclusiveOrAssign | LessThan           |
| Multiply    | MultiplyAssign    | LessThanOrEqual    |
| Or          | OrAssign          | NotEqual           |
| Power       | PowerAssign       |                    |
| RightShift  | RightShfitAssign  |                    |
| LeftShift   | LeftShiftAssign   |                    |

Signatures:

public Object DoOperation(ExpressionType operation,

Object target)

public TResult DoOperation&lt;TTarget, TResult&gt;

(ExpressionType operation, TTarget target)

public Object DoOperation

(ExpressionType operation, Object target, Object other)

public TResult DoOperation&lt;TTarget, TOther, TResult&gt;

(ExpressionType operation, TTarget target, TOther other)

public Object DoOperation(ExpressionType op,

ObjectHandle target)

public ObjectHandle DoOperation

(ExpressionType op, ObjectHandle target, ObjectHandle other)

<h3 id="add-methods">4.6.24 Add Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Add, self, other)

Signatures:

public object Add(object self, object other)

public ObjectHandle Add(ObjectHandle self, ObjectHandle other)

<h3 id="subtract-methods">4.6.25 Subtract Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Subtract, self, other)

Signatures:

public object Subtract(object self, object other)

public ObjectHandle Subtract(ObjectHandle self, ObjectHandle other)

<h3 id="power-methods">4.6.26 Power Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Power, self, other)

Signatures:

public object Power(object self, object other)

public ObjectHandle Power(ObjectHandle self, ObjectHandle other)

<h3 id="multiply-methods">4.6.27 Multiply Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Multiply, self, other)

Signatures:

public object Multiply(object self, object other)

public ObjectHandle Multiply(ObjectHandle self, ObjectHandle other)

<h3 id="divide-methods">4.6.28 Divide Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Divide, self, other)

Signatures:

public object Divide(object self, object other)

public ObjectHandle Divide(ObjectHandle self, ObjectHandle other)

<h3 id="modulo-methods">4.6.29 Modulo Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Modulo, self, other)

Signatures:

public ObjectHandle Modulo(ObjectHandle self, ObjectHandle other)

public Object Modulo(Object self, Object other)

<h3 id="leftshift-methods">4.6.30 LeftShift Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.LeftShift, self, other)

Signatures:

public object LeftShift(object self, object other)

public ObjectHandle LeftShift(ObjectHandle self, ObjectHandle other)

<h3 id="rightshift-methods">4.6.31 RightShift Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.RightShift, self, other)

Signatures:

public object RightShift(object self, object other) {

public ObjectHandle RightShift(ObjectHandle self,

ObjectHandle other)

<h3 id="bitwiseand-methods">4.6.32 BitwiseAnd Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.BitwiseAnd, self, other)

Signatures:

public object BitwiseAnd(object self, object other) {

public ObjectHandle BitwiseAnd(ObjectHandle self,

ObjectHandle other)

<h3 id="bitwiseor-methods">4.6.33 BitwiseOr Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.BitwiseOr, self, other)

Signatures:

public object BitwiseOr(object self, object other)

public ObjectHandle BitwiseOr(ObjectHandle self,

ObjectHandle other)

<h3 id="exclusiveor-methods">4.6.34 ExclusiveOr Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.ExclusiveOr, self, other)

Signatures:

public object ExclusiveOr(object self, object other)

public ObjectHandle ExclusiveOr(ObjectHandle self,

ObjectHandle other)

<h3 id="equal-methods">4.6.35 Equal Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Equal, self, other)

Signatures:

public bool Equal(object self, object other)

public bool Equal(ObjectHandle self, ObjectHandle other)

<h3 id="notequal-methods">4.6.36 NotEqual Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.Equal, self, other)

Signatures:

public Boolean NotEqual(Object self, Object other)

public Boolean NotEqual(ObjectHandle self, ObjectHandle other)

<h3 id="lessthan-methods">4.6.37 LessThan Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.LessThan, self, other)

Signatures:

public bool LessThan(object self, object other)

public bool LessThan(ObjectHandle self, ObjectHandle other)

<h3 id="lessthanorequal-methods">4.6.38 LessThanOrEqual Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.LessThanOrEqual, self, other)

Signatures:

public Boolean LessThanOrEqual(ObjectHandle self,

ObjectHandle other)

public Boolean LessThanOrEqual(Object self, Object other)

<h3 id="greaterthan-methods">4.6.39 GreaterThan Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.GreaterThan, self, other)

Signatures:

public bool GreaterThan(object self, object other)

public bool GreaterThan(ObjectHandle self, ObjectHandle other)

<h3 id="greaterthanorequal-methods">4.6.40 GreaterThanOrEqual Methods</h3>

These methods are convenience members that are equivalent to:

DoOperation(ExpressionType.GreaterThanOrEqual, self, other)

Signatures:

public bool GreaterThanOrEqual(object self, object other)

public bool GreaterThanOrEqual(ObjectHandle self,

ObjectHandle other)

<h2 id="sourcecodekind-enum">4.7 SourceCodeKind Enum</h2>

This enum identifies parsing hints to languages for ScriptSource objects. For example, some languages need to know if they are parsing a Statement or an Expression, or they may allow special syntax or variables for InteractiveCode.

<h3 id="type-summary">4.7.1 Type Summary</h3>

public enum SourceCodeKind {

Unspecified,

Expression,

Statements,

SingleStatement,

File,

InteractiveCode,

AutoDetect

<h3 id="members">4.7.2 Members</h3>

The Summary section shows the type as it is defined (to indicate values of members), and this section documents the intent of the members.

| Unspecified     | Should not be used.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
|-----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Expression      | Start parsing an expression.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| Statements      | Start parsing one or more statements if there's special syntax for multiple statements.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| SingleStatement | Start parsing a single statement, guaranteeing there's only one if that is significant to the language.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| File            | Start parsing at the beginning of a file.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| InteractiveCode | Start parsing at a legal input to a REPL. This kind also means the language should wrap the source to do language-specific output of the evaluation result.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| AutoDetect      | The language best determines how to parse the input. It may choose Interactive (supporting special syntax or variables), Expression, Statement, or File. This is the most liberal choice for the language to do whatever it determines is best. The goal of this kind is to help uses hosting with embedded code that has extraneous whitespace, several statements or expressions, etc. The language determines how to evaluate and what to return as the result in addition to shaping the extra whitespace to make sense if whitespace is significant in the language. The language may ignore initial lines with only whitespace if that would confuse the parsing or evaluation. |

<h2 id="scriptcodeparseresult-enum">4.8 ScriptCodeParseResult Enum</h2>

This enum identifies final parsing state for a ScriptSource objects. It helps with interactive tool support.

May need to rename to ScriptCodeParseResult since .NET naming conventions only like plural enum names for those with flag values. None should probably be Success or something more descriptive?

<h3 id="type-summary-1">4.8.1 Type Summary</h3>

public enum ScriptCodeParseResult {

Complete,

Invalid,

IncompleteToken,

IncompleteStatement,

Empty

<h3 id="members-1">4.8.2 Members</h3>

The Summary section shows the type as it is defined (to indicate values of members), and this section documents the intent of the members.

| Complete            | There is no reportable state after parsing.                       |
|---------------------|-------------------------------------------------------------------|
| Invalid             | The source is syntactically invalid and cannot be parsed.         |
| IncompleteToken     | The source ended on an incomplete token that aborted parsing.     |
| IncompleteStatement | The source ended on an incomplete statement that aborted parsing. |
| Empty               | The source is either empty, all whitespace, or all comments.      |

<h2 id="textcontentprovider-abstract-class">4.9 TextContentProvider Abstract Class</h2>

This class provides a means for hosts to provide TextReaders for a source that is already encoded as a Unicode string. A host only needs to implement one of these if it needs to provide a reader over source that is in its data structures (that is, not from a file or string). For example, an editor host could support reading from its buffer or text representation.

<h3 id="class-summary-6">4.9.1 Class Summary</h3>

\[Serializable\]

public abstract class TextContentProvider {

public abstract TextReader GetReader()

<h3 id="getreader-method-1">4.9.2 GetReader Method</h3>

This method returns a new TextReader each time you call it. The reader is positioned at the start of the input. It is undefined whether fetching the stream fetches fresh source contents.

Signature:

public abstract TextReader GetReader()

<h2 id="streamcontentprovider-abstract-class">4.10 StreamContentProvider Abstract Class</h2>

This class provides a mean for hosts to provide multiple Streams for a source of content that is binary data (has no encoding). Languages have an opportunity to decode the binary data directly if their language defines a way to supply encoding information within the source code.

<h3 id="class-summary-7">4.10.1 Class Summary</h3>

\[Serializable\]

public abstract class StreamContentProvider {

public abstract Stream GetStream()

<h3 id="getstream-method">4.10.2 GetStream Method</h3>

This method returns a new Stream each time you call it. The Stream is positioned at the start of the input. It is undefined whether fetching the stream fetches fresh source contents.

Signature:

public abstract Stream GetStream()

<h2 id="scriptcodereader-sealed-class">4.11 ScriptCodeReader Sealed Class</h2>

This class simply holds a ScriptSource and the TextReader associated with the ScriptSource. You get to these objects with ScriptSource.GetReader, which returns a new reader each time positioned at the start of the source.

Other than a property that returns the ScriptSource, this class just has overrides for TextReader and a virtual SeekLine. These changes allow the reader to interact with the language engine associated with the source for any special newline handling the language might perform.

<h3 id="class-summary-8">4.11.1 Class Summary</h3>

public sealed class ScriptCodeReader : TextReader {

public ScriptSource ScriptSource

internal ScriptCodeReader(SourceUnit sourceUnit,

TextReader textReader)

public override string ReadLine()

public virtual bool SeekLine(int line)

public override string ReadToEnd()

public override int Read(char\[\] buffer, int index, int count)

public override int Peek()

public override int Read()

<h2 id="scriptio-class">4.12 ScriptIO Class</h2>

This class let's you control input and output by default for dynamic code running via DLR hosting. You can access the instance of this class from the IO property on ScriptRuntime.

<h3 id="class-summary-9">4.12.1 Class Summary</h3>

public sealed class ScriptIO : MarshalByRefObject

internal ScriptIO(...)

/// Used for binary IO.

public Stream InputStream { get; }

public Stream OutputStream { get; }

public Stream ErrorStream { get; }

/// Used for pure unicode IO.

public TextReader InputReader { get; }

public TextWriter OutputWriter { get; }

public TextWriter ErrorWriter { get; }

/// What encoding are the unicode reader/writers using.

public Encoding InputEncoding { get; }

public Encoding OutputEncoding { get; }

public Encoding ErrorEncoding { get; }

public void SetOutput(Stream stream, Encoding encoding)

public void SetOutput(Stream stream, TextWriter writer)

public void SetErrorOutput(Stream stream, Encoding encoding)

public void SetErrorOutput(Stream stream, TextWriter writer)

public void SetInput(Stream stream, Encoding encoding)

public void SetInput(Stream stream, TextReader reader,

Encoding encoding)

public void RedirectToConsole()

<h3 id="outputstream-property">4.12.2 OutputStream Property</h3>

This property returns the standard output stream for the ScriptRuntime. This is a binary stream. All code and engines should output binary data here for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct output to a file or stream, then that language's output would go there as directed by the code.

Signature:

public Stream OutputStream { get;}

<h3 id="inputstream-property">4.12.3 InputStream Property</h3>

This property returns the standard input stream for the ScriptRuntime. This is a binary stream. All code and engines should read binary data from here for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct input from a file or stream, then that language's input would come from there as directed by the code.

Signature:

public Stream InputStream { get;}

<h3 id="errorstream-property">4.12.4 ErrorStream Property</h3>

This property returns the standard erroroutput stream for the ScriptRuntime. This is a binary stream. All code and engines should send error binary output here for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct error output to a file or stream, then that language's error output would go there as directed by the code.

Signature:

public Stream ErrorStream { get;}

<h3 id="inputreader-property">4.12.5 InputReader Property</h3>

This property returns the standard input reader for the ScriptRuntime. This is a unicode reader. All code and engines should read text from here for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct input from a file or stream, then that language's input would come from there as directed by the code.

Signature:

public TextReader InputReader { get; }

<h3 id="outputwriter-property">4.12.6 OutputWriter Property</h3>

This property returns the standard output writer for the ScriptRuntime. All code and engines should send text output here for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct output to a file or stream, then that language's output would go there as directed by the code.

Signature:

public TextWriter OutputWriter { get; }

<h3 id="errorwriter-property">4.12.7 ErrorWriter Property</h3>

This property returns the standard error output writer for the ScriptRuntime. All code and engines should send text error output here for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct error output to a file or stream, then that language's output would go there as directed by the code.

Signature:

public TextWriter ErrorWriter { get; }

<h3 id="inputencoding-property">4.12.8 InputEncoding Property</h3>

This property returns the encoding used by the TextReader returned from InputReader.

Signature:

public Encoding InputEncoding { get; }

<h3 id="outputencoding-property">4.12.9 OutputEncoding Property</h3>

This property returns the encoding used by the TextWriters returned from the OutputWriter property.

Signature:

public Encoding OutputEncoding { get; }

<h3 id="errorencoding-property">4.12.10 ErrorEncoding Property</h3>

This property returns the encoding used by the TextWriters returned from the ErrorWriter property.

Signature:

public Encoding ErrorEncoding { get; }

<h3 id="setoutput-method">4.12.11 SetOutput Method</h3>

This method sets the standard output stream for the ScriptRuntime. All code and engines should send output to the specified stream for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct output to a file or stream, then that language's output would go there as directed by the code.

Signatures:

public void SetOutput(Stream stream, Encoding encoding)

public void SetOutput(Stream stream, TextWriter writer)

The first method is useful if the host just captures binary stream output. The second method is useful if the host captures unicode text and binary output. Note, if you pass just a stream and an encoding, the this method creates a default StreamWriter, which writes a BOM on first usage. To avoid this, you'll need to pass your own TextWriter.

If any argument to these methods is null, they throw an ArgumentException.

<h3 id="seterroroutput-method">4.12.12 SetErrorOutput Method</h3>

This method sets the standard error output stream for the ScriptRuntime. All code and engines should send error output to the specified stream for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct error output to a file or stream, then that language's output would go there as directed by the code.

Signatures:

public void SetErrorOutput(Stream stream, Encoding encoding)

public void SetErrorOutput(Stream stream, TextWriter writer)

The first method is useful if the host just captures binary stream output. The second method is useful if the host captures unicode text and binary output.

If any argument to these methods is null, they throw an ArgumentException.

<h3 id="setinput-method">4.12.13 SetInput Method</h3>

This method sets the standard input stream for the ScriptRuntime. All code and engines should read input here for this ScriptRuntime. Of course, if a language has a mechanism to programmatically direct input from a file or stream, then that language's input would come from there as directed by the code.

Signature:

public void SetInput(Stream stream, Encoding encoding)

public void SetInput(Stream stream, TextReader reader,

Encoding encoding)

<h3 id="redirecttoconsole-method">4.12.14 RedirectToConsole Method</h3>

This method makes all the standard IO for the ScriptRuntime go to System.Console. Of course, if a language has a mechanism to programmatically direct output to a file or stream, then that language's output would go there as directed by the code.

Signature:

public void RedirectToConsole()

<h2 id="scriptruntimesetup-class">4.13 ScriptRuntimeSetup Class</h2>

This class gives hosts full control over how a ScriptRuntime gets configured. You can instantiate this class, fill in the setup information, and then instantiate a ScriptRuntime with the setup instance. Once you pass the setup object to create a ScriptRuntime, attempts to modify its contents throws an exception.

There is also a static method as a helper to hosts for reading .NET application configuration. Hosts that want to be able to use multiple DLR-hostable languages, allow users to change what languages are available, and not have to rebuild can use the DLR's default application configuration model. See ReadConfiguration for the XML details.

You can also get these objects from ScriptRuntime.Setup. These instances provide access to the configuration information used to create the ScriptRuntime. These instances will be read-only and throws exceptions if you attempt to modify them. Hosts may not have created a ScriptRuntimeSetup object and may not have configuration information without the Setup property.

<h3 id="class-summary-10">4.13.1 Class Summary</h3>

public sealed class ScriptRuntimeSetup {

public ScriptRuntimeSetup()

public IList&lt;LanguageSetup&gt; LanguageSetups { get; }

public bool DebugMode { get; set; }

public bool PrivateBinding { get; set; }

public Type HostType { get; set; }

public Dictionary&lt;string, object&gt; Options { get; }

public object\[\] HostArguments {get; set; }

public static ScriptRuntimeSetup ReadConfiguration()

public static ScriptRuntimeSetup

ReadConfiguration(Stream configFileStream)

<h3 id="constructor-1">4.13.2 Constructor</h3>

The constructor returns an empty ScriptRuntimeSetup object, with no languages preconfigured.

Signature:

public ScriptRuntimeSetup()

<h3 id="readconfiguration-methods">4.13.3 ReadConfiguration Methods</h3>

These methods read application configuration and return a ScriptRuntimeSetup initialized from the application configuration data. Hosts can modify the result before using the ScriptRuntimeSetup object to instantiate a ScriptRuntime.

Signatures:

public static ScriptRuntimeSetup ReadConfiguration()

public static ScriptRuntimeSetup

ReadConfiguration(Stream configFileStream)

<h4 id="configuration-structure">4.13.3.1 Configuration Structure</h4>

These lines must be included in the .config file as the first element under the &lt;configuration&gt; element for the DLR's default reader to work:

> &lt;configSections&gt;
>
>   &lt;section name="microsoft.scripting"
>
> type="Microsoft.Scripting.Hosting.Configuration.Section, Microsoft.Scripting, Version=1.0.0.5000, Culture=neutral, PublicKeyToken=31bf3856ad364e35" /&gt;
>
> &lt;/configSections&gt;

The structure of the configuration section is the following (with some notes below):

> &lt;microsoft.scripting \[debugMode="{bool}"\]?
>
> \[privateBinding="{bool}"\]?&gt;
>
>     &lt;languages&gt;
>
>       &lt;!-- BasicMap with type attribute as key. Inherits language
>
> nodes, overwrites previous nodes based on key --&gt;
>
>       &lt;language names="{semicolon-separated}"
>
> extensions="{semicolon-separated, optional-dot}"
>
> type="{assembly-qualified type name}"
>
> \[displayName="{string}"\]? /&gt;
>
>     &lt;/languages&gt;
>
>     &lt;options&gt;
>
>       &lt;!-- AddRemoveClearMap with option as key. If language
>
> Attribute is present, the key is option cross language.
>
> --&gt;
>
>       &lt;set option="{string}" value="{string}"
>
> \[language="{langauge-name}"\]? /&gt;
>
>       &lt;clear /&gt;
>
>       &lt;remove option="{string}" \[language="{langauge-name}"\]? /&gt;
>
>     &lt;/options&gt;
>
>   &lt;/microsoft.scripting&gt;

Attributes enclosed in \[…\]? are optional.

{bool} is whatever Convert.ToBoolean(string) works for (“true”, “False”, “TRUE”, “1”, “0”).

&lt;languages&gt; tag inherits content from parent .config files. You cannot remove a language in a child .config file once it is defined in a parent .config file. You can redefine a language if the value of the “type” attribute is the same as a defined in a parent .config file (last writer wins). If the displayName attribute is missing, ReadConfiguration sets it to the first name in the names attribute. If names is the empty string, then ReadConfiguration sets the display name to the type attribute. The names and extensions attributes support semi-colon and comma as separators.

&lt;options&gt; tag inherits options from parent .config files. You can set, remove, and clear options (removes them all). The key in the options dictionary is a pair of option and language attributes. Language attribute is optional. If specified, the option applies to the language whose simple name is stated; otherwise, it applies to all languages. &lt;remove option=”foo”/&gt; removes the option from common options dictionary, not from all language dictionaries. &lt;remove option=”foo” language=”rb”/&gt; removes the option from Ruby language options.

<h4 id="default-dlr-configuration">4.13.3.2 Default DLR Configuration</h4>

The default application configuration section for using the DLR languages we ship for the desktop is (of course, you need correct type names from your current version):

> &lt;?xml version="1.0" encoding="utf-8" ?&gt;
>
> &lt;configuration&gt;
>
>   &lt;configSections&gt;
>
>     &lt;section name="microsoft.scripting"
>
> type="Microsoft.Scripting.Hosting.Configuration.Section, Microsoft.Scripting, Version=1.0.0.5000, Culture=neutral, PublicKeyToken=31bf3856ad364e35" /&gt;
>
>   &lt;/configSections&gt;
>
>   &lt;microsoft.scripting&gt;
>
>     &lt;languages&gt;
>
>       &lt;language names="IronPython;Python;py" extensions=".py"
>
> displayName="IronPython v2.0"
>
> type="IronPython.Runtime.PythonContext, IronPython, Version=2.0.0.5000, Culture=neutral, PublicKeyToken=31bf3856ad364e35" /&gt;
>
>       &lt;language names="IronRuby;Ruby;rb" extensions=".rb"
>
> displayName="IronRuby v1.0"
>
> type="IronRuby.Runtime.RubyContext, IronRuby, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
>
> /&gt;
>
> &lt;!-- If for experimentation you want ToyScript ... --&gt;
>
>       &lt;language names="ToyScript;ts" extensions=".ts"
>
> type="ToyScript.ToyLanguageContext, ToyScript, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
>
> /&gt;
>
>     &lt;/languages&gt;
>
>   &lt;/microsoft.scripting&gt;
>
> &lt;/configuration&gt;

<h3 id="languagesetups-property">4.13.4 LanguageSetups Property</h3>

This property returns a list of LanguageSetup objects, each describing one language the ScriptRuntime will allow. When you instantiate the ScriptRuntime, it will ensure there is only one element in the list with a given LanguageSetup.TypeName value.

Signature:

public IList&lt;LanguageSetup&gt; LanguageSetups { get; }

<h3 id="hosttype-property">4.13.5 HostType Property</h3>

This property gets and sets the ScriptHost type that the DLR should instantiate when it creates the ScriptRuntime. The DLR instantiates the host type in the app domain where it creates the ScriptRuntime object. See ScriptHost for more information.

public Type HostType { get; set; }

<h3 id="hostarguments-property">4.13.6 HostArguments Property</h3>

This property gets and sets an array of argument values that should be passed to the HostType's constructor. The objects must be MBRO or serializable when creating a remote ScriptRuntime.

Here's an example:

> class MyHost : ScriptHost {
>
> public MyHost(string foo, int bar)
>
> }
>
> setup = new ScriptRuntimeSetup()
>
> setup.HostType = typeof(MyHost)
>
> setup.HostArguments = new object\[\] { “some foo”, 123 }
>
> ScriptRuntime.CreateRemote(otherAppDomain, setup)

Signature:

public object\[\] HostArguments {get; set; }

<h3 id="options-property">4.13.7 Options Property</h3>

This property returns a dictionary of global options for the ScriptRuntime. There are two options explicit on the ScriptRuntimeSetup type, DebugMode and PrivateBinding. The Options property is an flexibility point for adding options later. Names are case-sensitive.

There is one specially named global option, "SearchPaths". If this value is present, languages should add these paths to their default search paths. If your intent is to replace an engine's default paths, then you can use Engine.SetSearchPaths (perhaps on the ScriptHost.EngineCreated callback).

Signature:

public Dictionary&lt;string, object&gt; Options { get; }

<h3 id="debugmode-property">4.13.8 DebugMode Property</h3>

This property controls whether the ScriptRuntime instance and engines compiles code for debuggability.

Signature.

public bool DebugMode { get; set; }

<h3 id="privatebinding-property">4.13.9 PrivateBinding Property</h3>

This property controls whether the ScriptRuntime instance and engines will use reflection to access private members of types when binding object members in dynamic operations. Setting this to true only works in app domains running in full trust.

public bool PrivateBinding { get; set; }

<h2 id="languagesetup-class">4.14 LanguageSetup Class</h2>

This class represents a language configuration for use in a ScriptRuntimeSetup when instantiating a ScriptRuntime. Once you pass the setup object to create a ScriptRuntime, attempts to modify its contents throws an exception.

You can also get these objects from ScriptRuntime.Setup and ScriptEngine.Setup. These instances provide access to the configuration information used to create the ScriptRuntime. These instances will be read-only and throws exceptions if you attempt to modify them. Hosts may not have created a ScriptRuntimeSetup object and may not have language setup information without the Setup properties.

<h3 id="class-summary-11">4.14.1 Class Summary</h3>

public sealed class LanguageSetup {

public LanguageSetup(string typeName, string displayName)

public LanguageSetup(string typeName, string displayName,

IEnumerable&lt;string&gt; names,

IEnumerable&lt;string&gt; fileExtensions)

public string TypeName {get; set; }

public string DisplayName {get; set; }

public IList&lt;string&gt; Names {get; }

public IList&lt;string&gt; FileExtensions {get; }

public Dictionary&lt;string, object&gt; Options {get; }

public bool InterpretedMode {get; set; }

public bool ExceptionDetail {get; set; }

public bool PerfStats {get; set; }

public T GetOption&lt;T&gt;(string name, T defaultValue)

<h3 id="constructors">4.14.2 Constructors</h3>

The minimal construction requires an assembly-qualified type name for the language and a display name. You can set other properties after instantiating the setup object.

These ensure typeName and displayName are not null or empty. The collections can be empty but not null so that you can fill them in after instantiating this type.

Signatures:

public LanguageSetup(string typeName, string displayName)

public LanguageSetup(string typeName, string displayName,

IEnumerable&lt;string&gt; names,

IEnumerable&lt;string&gt; fileExtensions)

<h3 id="typename-property">4.14.3 TypeName Property</h3>

This property gets or sets the assembly-qualified type name of the language. This is the type the DLR loads when, for example, it needs to execute files with the specified file extensions.

Signature:

public string TypeName {get; set; }

<h3 id="displayname-property">4.14.4 DisplayName Property</h3>

This property gets or sets a suitably descriptive name for displaying in UI or for debugging. It often includes the version number in case different versions of the same language are configured.

Signature:

public string DisplayName {get; set; }

<h3 id="names-property">4.14.5 Names Property</h3>

This property returns a list of names for the language. These can be nicknames or simple names used programmatically (for example, language=python on a web page or in a user's options UI).

Signature:

public IList&lt;string&gt; Names {get; }

<h3 id="fileextensions-property">4.14.6 FileExtensions Property</h3>

This property gets the list of file extensions that map to this language in the ScriptRuntime.

Signature:

public IList&lt;string&gt; FileExtensions {get; }

<h3 id="interpretedmode-property">4.14.7 InterpretedMode Property</h3>

This property gets or sets whether the language engine interprets sources or compiles and executes them. Not all languages respond to this option.

This method pulls the value from Options in case it is set there via application .config instead of via the property setter. It defaults to false. If the host or reading .config set this option, then it will be in Options with the key "InterpretedMode".

Signature:

public bool InterpretedMode {get; set; }

<h3 id="exceptiondetail-property">4.14.8 ExceptionDetail Property</h3>

This property gets or sets whether the language engine should print exception details (for example, a call stack) when it catches exceptions. Not all languages respond to this option.

This method pulls the value from Options in case it is set there via application .config instead of via the property setter. It defaults to false. If the host or reading .config set this option, then it will be in Options with the key "ExceptionDetail".

Signature:

public bool ExceptionDetail {get; set; }

<h3 id="perfstats-property">4.14.9 PerfStats Property</h3>

This property gets or sets whether the language engine gathers performance statistics. Not all languages respond to this option. Typically the languages dump the information when the application shuts down.

This method pulls the value from Options in case it is set there via application .config instead of via the property setter. It defaults to false. If the host or reading .config set this option, then it will be in Options with the key "ExceptionDetail".

Signature:

public bool PerfStats {get; set; }

<h3 id="options-property-1">4.14.10 Options Property</h3>

This property returns the list dictionary of options for the language. Option names are case-sensitive. The list of valid options for a given language must be found in its documentation.

Signature:

public Dictionary&lt;string, object&gt; Options {get; }

<h3 id="getoption-method">4.14.11 GetOption Method</h3>

This method looks up name in the Options dictionary and returns the value associated with name, converting it to type T. If the name is not present, this method return defaultValue.

Signature:

public T GetOption&lt;T&gt;(string name, T defaultValue)

<h2 id="scripthost-class">4.15 ScriptHost Class</h2>

ScriptHost represents the host to the ScriptRuntime. Hosts can derive from this type and overload behaviors by returning a custom PlatformAdaptationLayer. Hosts can also handle callbacks for some events such as when engines get created.

The ScriptHost object lives in the same app domain as the ScriptRuntime in remote scenarios.

Derived types from ScriptHost can have arguments passed to them via ScriptRuntimeSetup's HostArguments property. For example,

> class MyHost : ScriptHost {
>
> public MyHost(string foo, int bar)
>
> }
>
> setup = new ScriptRuntimeSetup()
>
> setup.HostType = typeof(MyHost)
>
> setup.HostArguments = new object\[\] { “some foo”, 123 }
>
> ScriptRuntime.CreateRemote(otherAppDomain, setup)

The DLR instantiates the ScriptHost when the DLR initializes a ScriptRuntime. The host can get at the instance with ScriptRuntime.Host.

<h3 id="class-summary-12">4.15.1 Class Summary</h3>

public class ScriptHost : MarshalByRefObject {

public ScriptHost()

public ScriptRuntime Runtime { get; }

public virtual PlatformAdaptationLayer

PlatformAdaptationLayer {get; }

protected virtual void RuntimeAttached()

internal protected virtual void

EngineCreated(ScriptEngine engine)

<h3 id="runtime-property-1">4.15.2 Runtime Property</h3>

This property returns the ScriptRuntime to which this ScriptHost is attached.

Signature:

public ScriptRuntime Runtime { get; }

<h3 id="platformadaptationlayer-property">4.15.3 PlatformAdaptationLayer Property</h3>

This property returns the PlatformAdaptationLayer associated with the ScriptRuntime. This object adapts the runtime to the system by implementing various file operations, for example. The Silverlight DLR host and PAL might go to the server for some operations or throw an exception for others, depending on the behavior of the operation.

Signature:

public virtual PlatformAdaptationLayer

PlatformAdaptationLayer {get; }

<h3 id="runtimeattached-method">4.15.4 RuntimeAttached Method</h3>

This method gets called when initializing a ScriptRuntime is finished. The host can override this method to do additional initialization such as calling ScriptRuntime.LoadAssembly.

Signature:

protected virtual void RuntimeAttached()

<h3 id="enginecreated-method">4.15.5 EngineCreated Method</h3>

This method is a call back from the ScriptRuntime whenever it causes a language engine to be loaded and initialized. Hosts can derive from ScriptHost to override this method, which by default does nothing. An example usage would be for a host to load some standard scripts per language or to load per language init files for end users.

Signature:

internal protected virtual void

EngineCreated(ScriptEngine engine)

<h2 id="scriptruntimeconfig-class">4.16 ~~ScriptRuntimeConfig Class~~</h2>

~~This class provides access to ScriptRuntime configuration information provided when it was constructed. The host may not have created a ScriptRuntimeSetup object and may not have this information available otherwise. This object does not report on all options the runtime or language may have; it reports only those supplied via ScriptRuntimeSetup (or the app .config file).~~

~~See ScriptRuntimeSetup for more info on the properties. This type is different only in that it is read-only.~~

<h3 id="class-summary-13">4.16.1 ~~Class Summary~~</h3>

~~public sealed class ScriptRuntimeConfig {~~

~~public IList&lt;LanguageConfig&gt; Languages { get { } }~~

~~public bool DebugMode { get { } }~~

~~public bool PrivateBinding { get { } }~~

~~public IDictionary&lt;string, object&gt; Options { get { } }~~

<h2 id="languageconfig-class">4.17 ~~LanguageConfig Class~~</h2>

~~This class provides access to language configuration information provided when creating the ScriptRuntime. The host may not have created a ScriptRuntimeSetup object and may not have this information available otherwise. This object does not report on all options the language may have; it reports only those supplied via ScriptRuntimeSetup (or the app .config file).~~

~~See LanguageSetup for more info on the properties. This type is different only in that it is read-only.~~

<h3 id="class-summary-14">4.17.1 ~~Class Summary~~</h3>

~~public sealed class LanguageConfig {~~

~~public string TypeName {get { } }~~

~~public string DisplayName {get { } }~~

~~public IList&lt;string&gt; Names {get { } }~~

~~public IList&lt;string&gt; FileExtensions {get { } }~~

<h2 id="platformadaptationlayer-class">4.18 PlatformAdaptationLayer Class</h2>

This class abstracts system operations used by the DLR that could possibly be platform specific. Hosts can derive from this class and implement operations, such as opening a file. For example, the Silverlight PAL could go to the server to fetch a file.

To use a custom PAL, you derive from this type and implement the members important to you. You also need to derive a custom ScriptHost that returns the custom PAL instance. Then when you create your ScriptRuntime, you explicitly create a ScriptRuntimeSetup and set the HostType property to your custome ScriptHost.

<h3 id="class-summary-15">4.18.1 Class Summary</h3>

public class PlatformAdaptationLayer {

public static readonly PlatformAdaptationLayer Default

public virtual Assembly LoadAssembly(string name)

public virtual Assembly LoadAssemblyFromPath(string path)

public virtual void TerminateScriptExecution(int exitCode)

public StringComparer PathComparer { get;}

public virtual bool FileExists(string path)

public virtual bool DirectoryExists(string path)

public virtual Stream

OpenInputFileStream(string path, FileMode mode,

FileAccess access, FileShare share)

public virtual Stream

OpenInputFileStream(string path, FileMode mode,

FileAccess access, FileShare share,

int bufferSize)

public virtual Stream OpenInputFileStream(string path)

public virtual Stream OpenOutputFileStream(string path)

public virtual string\[\] GetFiles(string path,

string searchPattern)

public virtual string GetFullPath(string path)

public virtual string CurrentDirectory {get;}

public virtual string\[\]

GetDirectories(string path, string searchPattern)

public virtual bool IsAbsolutePath(string path)

<h2 id="syntaxerrorexception-class">4.19 SyntaxErrorException Class</h2>

<h2 id="scriptexecutionexception-class">4.20 ScriptExecutionException Class</h2>

This class and its subtypes represent errors that occurred while executing code within the hosting API. The hosting API wraps any error that occurs while dynamic language code executes in a ScriptExecutionException object and rethrows that. The DLR does NOT wrap parsing errors since we have other exceptions for those. The DLR does not wrap other API errors in this exception either.

<h2 id="errorlistener-class">4.21 ErrorListener Class</h2>

This is an abstract class that hosts can implement and supply to ScriptSource.Compile methods. Instead of raising exceptions for compilation errors, the compile methods report errors by calling on the ErrorListener.

<h3 id="class-summary-16">4.21.1 Class Summary</h3>

public abstract class ErrorListener : MarshalByRefObject

protected ErrorListener()

public abstract void ErrorReported

(ScriptSource source, string message, SourceSpan span,

int errorCode, Severity severity)

<h2 id="severity-enum">4.22 Severity Enum</h2>

This enum identifies compiler error kinds when calling ErrorListener.ErrorReported.

<h3 id="type-summary-2">4.22.1 Type Summary</h3>

public enum Severity {

Ignore,

Warning,

Error,

FatalError

<h2 id="sourcelocation-struct">4.23 SourceLocation Struct</h2>

<h2 id="sourcespan-struct">4.24 SourceSpan Struct</h2>

<h2 id="exceptionoperations-class">4.25 ExceptionOperations Class</h2>

This class provides language-specific utilities for working with exceptions coming from executing code. You access instances of this type from Engine.GetService.

<h3 id="class-summary-17">4.25.1 Class Summary</h3>

public sealed class ExceptionOperations : MarshalByRefObject {

public string FormatException(Exception exception)

public void GetExceptionMessage

(Exception exception, out string message,

out string errorTypeName)

<h2 id="documentoperations-class">4.26 DocumentOperations Class</h2>

This class provides language-specific utilities for getting documentation and call signature information for objects coming from executing code. You access instances of this type from Engine.GetService.

<h3 id="class-summary-18">4.26.1 Class Summary</h3>

``` csharp
public sealed class DocumentationOperations : MarshalByRefObject {
    public ICollection<MemberDoc> GetMembers(object value)
    public ICollection<OverloadDoc> GetOverloads(object value)
    public ICollection<MemberDoc> GetMembers(ObjectHandle value)
    public ICollection<OverloadDoc> GetOverloads(ObjectHandle value)
```

<h3 id="getmembers-method">4.26.2 GetMembers Method</h3>

This method returns the collection of MemberDocs which in turn represent the name and kind of member for each. If there are no members, the collection is empty.

Signatures:

``` csharp
public ICollection<MemberDoc> GetMembers(object value)
public ICollection<MemberDoc> GetMembers(ObjectHandle value)
```

<h3 id="getoverloads">4.26.3 GetOverloads</h3>

This method returns the collection of OverloadDocs which in turn provide signature info. If the object is not invocable, then the collection is empty.

Signature:

``` csharp
public ICollection<OverloadDoc> GetOverloads(object value)
public ICollection<OverloadDoc> GetOverloads(ObjectHandle value)
```

<h2 id="memberdoc-class">4.27 MemberDoc Class</h2>

This class provides language-specific, basic information about members of an object. You access instances of this type from DocumentationOperations objects.

<h3 id="class-summary-19">4.27.1 Class Summary</h3>

``` csharp
public class MemberDoc {
    public MemberDoc(string name, MemberKind kind) 
    public string Name { get {} }
    public MemberKind Kind { get {} }
```

<h3 id="name-property">4.27.2 Name Property</h3>

<h3 id="kind-property-1">4.27.3 Kind Property</h3>

<h2 id="memberkind-enum">4.28 MemberKind Enum</h2>

<h3 id="type-summary-3">4.28.1 Type Summary</h3>

``` csharp
public enum MemberKind {
    None,
    Class,
    Delegate,
    Enum,
    Event,
    Field,
    Function,
    Module,
    Property,
    Constant,
    EnumMember,
    Instance,
    Method,
    Namespace
```

<h3 id="members-2">4.28.2 Members</h3>

The Summary section shows the type as it is defined (to indicate values of members), and this section documents the intent of the members.

| None       | Unsure of kind. |
|------------|-----------------|
| Class      |                 |
| Delegate   |                 |
| Enum       |                 |
| Event      |                 |
| Field      |                 |
| Function   |                 |
| Module     |                 |
| Property   |                 |
| Constant   |                 |
| EnumMember |                 |
| Instance   |                 |
| Method     |                 |
| Namespace  |                 |

<h2 id="overloaddoc-class">4.29 OverloadDoc Class</h2>

This class provides language-specific information about all the overloads when an object is invocable. You access instances of this type from DocumentationOperations objects.

<h3 id="class-summary-20">4.29.1 Class Summary</h3>

``` csharp
public class OverloadDoc {
        public OverloadDoc(string name, string documentation, 
                           ICollection<ParameterDoc> parameters)
        public OverloadDoc(string name, string documentation, 
                           ICollection<ParameterDoc> parameters,
                           ParameterDoc returnParameter) 
        public string Name { get {} }
        public string Documentation { get {} }
        public ICollection<ParameterDoc> Parameters { get {} }
        public ParameterDoc ReturnParameter { get {} }
```

<h3 id="name-property-1">4.29.2 Name Property</h3>

<h3 id="documenation-property">4.29.3 Documenation Property</h3>

This property returns any doc comments or documentation strings the language allows programmers to embed in code.

Signature:

``` csharp
public string Documentation { get {} }
```

<h3 id="parameters-property">4.29.4 Parameters Property</h3>

This property returns a collection of ParameterDocs representing information such as name, type, doc comments, etc., associated with the parameter. If there are no parameters, the collection is empty.

Signature:

``` csharp
public ICollection<ParameterDoc> Parameters { get {} }
```

<h3 id="returnparameter">4.29.5 ReturnParameter</h3>

This property returns information about the return value as a ParameterDoc object.

Signature:

``` csharp
public ParameterDoc ReturnParameter { get {} }
```

<h2 id="parameterdoc-class">4.30 ParameterDoc Class</h2>

<h3 id="class-summary-21">4.30.1 Class Summary</h3>

``` csharp
public class ParameterDoc {
    public ParameterDoc(string name)
    public ParameterDoc(string name, ParameterFlags paramFlags)
    public ParameterDoc(string name, string typeName)
    public ParameterDoc(string name, string typeName, string documentation)
    public ParameterDoc(string name, string typeName, string documentation,
                        ParameterFlags paramFlags) 
    public string Name { get {} }
    public string TypeName { get {} }
    public ParameterFlags Flags { get {} }
    public string Documentation { get {} }
```

<h3 id="name-property-2">4.30.2 Name Property</h3>

<h3 id="typename-property-1">4.30.3 TypeName Property</h3>

This property returns the type of the parameter as a name. If the type has a fully qualified form, this only returns the last token of the name. The purpose of this information is for tool presentation, not crossing over to the .NET reflection model of types.

Signature:

``` csharp
public ParameterFlags Flags { get {} }
```

<h3 id="documentation-property">4.30.4 Documentation Property</h3>

This property returns any doc comments or strings associated with the parameter. If there is no such documentation, this property returns null.

Signature:

``` csharp
public string Documentation { get {} }
```

<h2 id="parameterflags-enum">4.31 ParameterFlags Enum</h2>

This enum identifies extra information about a parameter, such as whether it is caught as a rest argument or dictionary argument.

<h3 id="type-summary-4">4.31.1 Type Summary</h3>

public enum ParameterFlags {

None,

ParamsArray,

ParamsDict

<h3 id="members-3">4.31.2 Members</h3>

The Summary section shows the type as it is defined (to indicate values of members), and this section documents the intent of the members.

| None        | Just a positional parameter.                                                                                                                                                         |
|-------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ParamsArray | Indicates the parameter is a rest parameter or collection of all the arguments after any supplied positional arguments.                                                              |
| ParamsDict  | Indicated the parameter is a parameter that maps names of parameter to their values. It is like a rest parameter, but the rest of the arguments must be supplied as named arguments. |

<h2 id="post-clr-4.0----tokencategorizer-abstract-class">4.32 POST CLR 4.0 -- TokenCategorizer Abstract Class</h2>

Will spec colorization support after reconsidering best API for simple tooling and common support for VS plugins. This needs to be in another spec when fleshed out, but it is a placeholder for consideration.

<h3 id="class-summary-22">4.32.1 Class Summary</h3>

public abstract class TokenCategorizer: MarshalByRefObject {

void Initialize(object state, ScriptCodeReader sourceReader,

SourceLocation initialLocation)

public abstract bool IsRestartable { get; }

public abstract TokenInfo ReadToken()

public abstract bool SkipToken()

public abstract IEnumerable&lt;TokenInfo&gt; ReadTokens

(int countOfChars)

public abstract bool SkipTokens(int countOfChars)

public abstract SourceLocation CurrentPosition { get; }

public abstract object CurrentState { get; }

public abstract ErrorListener ErrorListener { get; set; }

<h2 id="post-clr-4.0----tokencategory-enum">4.33 POST CLR 4.0 -- TokenCategory Enum</h2>

Will spec colorization support after reconsidering best API for simple tooling and common support for VS plugins. This needs to be in another spec when fleshed out, but it is a placeholder for consideration.

<h2 id="post-clr-4.0----tokeninfo-struct">4.34 POST CLR 4.0 -- TokenInfo Struct</h2>

Will spec colorization support after reconsidering best API for simple tooling and common support for VS plugins. This needs to be in another spec when fleshed out, but it is a placeholder for consideration.

<h2 id="post-clr-4.0----tokentriggers-enum">4.35 POST CLR 4.0 -- TokenTriggers Enum</h2>

Will spec colorization support after reconsidering best API for simple tooling and common support for VS plugins. This needs to be in another spec when fleshed out, but it is a placeholder for consideration.

<h2 id="cut----consolehost-abstract-class">4.36 CUT -- ConsoleHost Abstract Class ???</h2>

<h2 id="cut----consolehostoptions-class">4.37 CUT -- ConsoleHostOptions Class</h2>

<h2 id="cut----consolehostoptionsparser-class">4.38 CUT -- ConsoleHostOptionsParser Class</h2>
