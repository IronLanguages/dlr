# 5 Current Issues

**Host aborts.** Need to provide a way for hosts to signal the DLR to abort execution so that we do not do a rude thread abort while the host is potentially executing deep in its code. The scenario is that the host has provided an OM layer that it protects all over and understands dyn code could abort, but the code internal (lower down) that the OM layer calls is not coded for rude aborts. The host executes dyn code, the dyn code calls back into the hosts OM, and the host then calls internal functions that shouldn't have to be protected everywhere against rude thread aborts. These internal functions do not worry about thread aborts because if that's happening, then the app is going down. The host would like to be able to support user actions to abort dyn code executions (for example, spinning cassette in VSMacros or ctrl-c in Nessie) by setting a ScriptRuntime.AbortExecutions flag. The DLR code periodically checks this, does whatever internal throwing it wants, but then just returns from whatever functions. Is the model that the host has to clear the flag, or can the DLR know to stop all code on all threads, then clear the flag?

**Stack depth control.** Need to add support for the host to provide a delegate or some control on handling stack overflows. It could set a limit, and when we detect it, we call the host's delegate. It could set the abort flag, kill the thread, perhaps return a value to abort, or whatever.

**Engine recycling.** Need to design model for engines that can recycle themselves and how to shutdown or reset ScriptRuntimes for use in asp.net like server situations.

**Doc exceptions.** Doc for each member what exceptions can be thrown, should we define catcher exceptions for lower-level calls (e.g., wrapper for loadassm to hide its nine exceptions).

**Global value src loc.** Consider host inspecting global value and asking for src line where the value came from for good error msgs to users.

**MWeb and error sinking.** Error sink'ing and MWeb requirements need to be factored into hosting API.

**DLL loading filters.** Hosts can control dynamic language file resolution, but they don't get called on for DLL resolution. This means they can't redirect or limit which DLLs can load. Not sure this is meaningful since the code could just use .NET calls from standard BCL libs to load assemblies (eh?).

**MBRO/remoting lifetime lease timeout**. Currently if you do not use a host for 15 min, it dies, and you lose your data. We've added InitializeLifetimeService on objects to cause them to stay alive forever, but we need to think through the design and the host controls here.

**Remote AppDomain DLR exceptions need to remote.** An exception may be thrown in the remote AppDomain, and the host application needs to be able to catch this error. Currently there are some DLR exceptions that don't serialize across the AppDomain boundary or contain data that doesn't serialize across the AppDomain boundary. Does it wrap all local exceptions with a remotable exception that refers back to the local exception? Or are all exceptions required to be serializable across AppDomain boundaries.

**VB hosting and static info for compiling**. One team asked us to support hosts that want to pass static info in for globals or scope variables. We think this makes sense so that when the DLR supports optional explicit typing, inferencing, etc., then hosts could supply optional explicit type info.

The rough idea for how to do this (given current VB and C\# namespace and scope chains) is to take a chain of SourceMetadata objects with names and types declared in them. Then, when the host executes the code, it would reproduce the shape of the chain with ScriptScope objects. When hosts construct these ScriptScope chains, they probably should be able to both use SetVar to set add members, or create a ScriptScope on a static .NET object (where the members of the object match the SourceMetadata names and types).

Add a SourceMetadata type with:

- Names -&gt; Dictionary&lt;string, Type&gt; (ok to use Type since it would have to be loaded in both app domains if doing remote hosting)

- ReferenceDlls -&gt; string\[\]

- ImportedNamespaces -&gt; string\[\]

- Parent -&gt; SourceMetadata

Add to ScriptSource:

- .Compile(CompilerOptions, ErrorSink, SourceMetadata) where first two can be null for defaults.

Add to CompiledCode (if we follow through on supporting this, we might still add this only to a lower-level API like LangCtx):

- .GetExpressionTree(), but I suspect we also need ...

Add to ScriptScope

- .Parent -&gt; ScriptScope (returns null by default, must be set in constructor)

**GetMembersVerbose.** This would return something like IList&lt;Tuple&lt;string,flags&gt;&gt;, but it would support poor man tooling for objects or languages that wanted to report value namespaces or categorize names somehow.

**Compiler options between REPL inputs.** Ipy.exe compiles the input "from future import truedivision", and then on the next line compile the input "3/2". How does the scope/compiler context flow from the first to the second? Maybe we can unify the VB SrcMetadata request with this to capture compiler options and let them be updated across compilation.

**Need to revisit ScriptIO.SetInput**. Do we need encoding arg?

public void SetInput(Stream stream, TextReader reader,

Encoding encoding)

**Consider expanding LoadAssembly and simplifying name.** Probably need to expand to type libs for COM interop (check with BizApps folks):

> ScriptRuntime.LoadTypeLibrary(TypeLibrary library)
>
> ScriptRuntime.LoadTypeLibrary(TypeLibraryName libraryName)

TypeLibrary and TypeLibraryName types don’t exist yet. Also consider consolidating:

> ScriptRuntime.Load(Assembly assembly)
>
> ScriptRuntime.Load(AssemblyName assembly)
>
> ScriptRuntime.Load(TypeLibrary library)
>
> ScriptRuntime.Load(TypeLibraryName libraryName)

**Add a convention for a .NET exception representing failed name look up** that is common across all languages. Otherwise, hosts have to handle N different exceptions from all the languages. Can we use the python exception folding trick here?

**ObjOps.Equal and Operators.Equals should match in name.**

**Spec HostingHelpers and think about how it fits the general model**.

**ScriptScope doesn't offer a case insensitive variable lookup even if languages support it**. Since we never fully build the case-insensitive symbol design, and since we intend to go to dict&lt;str,obj&gt; over IAttrColl, we have no model for langs that want to do case-insensitive lookups without O(n) searching the table.
