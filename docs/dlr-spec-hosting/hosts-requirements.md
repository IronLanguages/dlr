# 3 Hosts Requirements

While the functional requirements are accurate here, the descriptions are a bit dated and many use old APIs (even PythonEngine APIs from pre 1.0 releases) for examples.

<h2 id="silverlight-ria">3.1 SilverLight (RIA)</h2>

We need to ensure that the DLR can plug into whatever hosting story Telesto has for integrating into web browsers. We want web pages to be able to have one or more DLR script elements on them. Scenarios for such script are the same as if the script were native JavaScript in the browser.

We will work with Telesto to build an object that can be hosted via an object element in the HTML. Anyone using DLR scripting languages will use the same object. This object will derive from Telesto’s PageLet class, and when it spins up, it will scan the page for DLR script blocks, load the DLR, and spin up script engines.

<h2 id="merlinweb-server-side">3.2 MerlinWeb (Server Side)</h2>

MerlinWeb needs to work with various script blocks of different languages in the same execution ScriptRuntime in a language-neutral way (or with minimal language-specific awareness).

<h3 id="compilation">3.2.1 Compilation</h3>

****Represent each page with an EE scope (ipy EngineScopes).****

With IPy, all ASP.NET files that are normally managed classes (e.g., aspx/ascx/master/ashx files) are represented by EngineScope objects, created using:

> engineScope = s\_engine.CreateScope(String.Empty /\*scopeName\*/,
>
> globals, false/\*publishScope\*/);

****Execute script as text in the context of an EE scope.****

Currently, the code found in a page’s code behind file or in its &lt;script runat=”server”&gt; block is fed into the page’s IPy EngineScope:

s\_engine.Execute(scriptCode, engineScope);

****Enumerate a scope for functions.****

Currently, after feeding code into an EngineScope, MerlinWeb looks for top-level functions with:

> foreach (KeyValuePair&lt;string, object&gt; pair in EngineScope.Globals) {
>
> PythonFunction f = pair.Value as PythonFunction;
>
> //...

For each PythonFunction, MerlinWeb checks a few properties such as its ArgCount (for a rough signature check) and line number where the function starts:

> f.ArgCount
>
> ((FunctionCode)f.FunctionCode).FirstLineNumber”

****Invoke functions.****

Currently, MerlinWeb invokes function via Ops.Call().

****Compile code fragments.****

Code snippets come from *&lt;% code %&gt;* and *&lt;%= expr %&gt;* elements in the HTML.

Currently, MerlinWeb uses CreateMethod and CreateLambda, respectively. MerlinWeb likes the way these work now and wants something similar in DLR.

****Reference a special directory for access to language scopes.****

MerlinWeb apps support an App\_Script directory containing script files that any code can reference. This is the logical equivalent of ASP.NET’s App\_Code directory, but for dynamic script files.

****Currently, the code does the following:****

s\_engine.Sys.path.Add(s\_scriptFolder);

****Reload scripts that are in use and have changed.****

MerlinWeb detects scripts that have changed, and it needs to direct the engine to reload those scripts, resetting global state and definitions for the scope.

Currently, MerlinWeb does something like the following:

> foreach (PythonScope scope in s\_engine.Sys.scopes.Values) {
>
> if (scope.Filename.StartsWith(s\_scriptFolder))
>
> Builtin.Reload(scope);
>
> }

MerlinWeb is also okay with throwing out the ScriptRuntime and creating a new instance if this is on the order of 1-2s vs. several seconds.

<h3 id="globals-and-member-injection">3.2.2 Globals and Member Injection</h3>

****Provide “globals” and participate in name lookup****

MerlinWeb needs to inject global as name-&gt;object mappings. Furthermore, MerlinWeb needs to be able to participate in name lookup, so that dynamically when a name’s value is fetched MerlinWeb can discover what that name should be bound to and provide an object when asked.

****Inject members into pre-defined types with per instance values****

MerlinWeb needs to be able to dynamically compute member lookup for objects/types to provide a better programming experience for script on pages for managed objects that were defined before the advent of MerlinWeb. MerlinWeb uses this mechanism to support simpler syntax for things like:

- page.TextBox1 instead of page.FindControl(“TextBox1”)

- request.Foo instead of request.QuesryString\[“Foo”\]

Currently, MerlinWeb is highly dependent on the new IAttributesInjector and the Ops.RegisterAttributesInjectorForType mechanism.

****Allow for injected members on types defined for MerlinWeb****

MerlinWeb needs to make .NET objects appear to be dynamic objects for member lookup resolution, resolving names for members of a given object when the names come from different sources. MerlinWeb uses this ability to make objects (for example, control and page objects) and associated script functions appear on a unified object where members appearing on the object come from both the variables on the page or control as well as come from globals in associated scripts.

Currently, each dynamic page (aspx/ascx/master) is made up of both a regular Page-derived object, and an associated EngineScope. MerlinWeb makes them behave as if the EngineScope adds methods to the Page (the way a partial class would). This works by having the custom Page implement ICustomAttributes. The ICustomAttributes implementation ‘combines’ everything on the EngineScope (obtained via EngineScope.Globals) with everything on the Page’s DynamicType (obtained via Ops.GetDynamicType).

<h3 id="error-handling">3.2.3 Error handling</h3>

****Syntax error handling****

MerlinWeb needs to get syntax error information for reporting and interacting with tools.

Currently, it catches PythonSyntaxErrorException’s to get line number information from them using the FileName and Line properties.

****Runtime error handling****

When a run-time error occurs, MerlinWeb needs to get an error message, file name, and line number for reporting.

Currently, the code looks like this (not pretty, should be cleaned up):

> // Though we ignore the return value, this call is needed
>
> Ops.ExtractException(e, s\_engine.Sys);
>
> Tuple t = s\_engine.Sys.exc\_info();
>
> string message = (string)t\[1\].ToString();
>
> TraceBack tb = (TraceBack)t\[2\];
>
> // Find the initial exception frame
>
> while (tb.Next != null)
>
> tb = tb.Next;
>
> // Clear the exception
>
> Ops.ClearException(s\_engine.Sys);
>
> int line = tb.Line;
>
> string path = (string)((FunctionCode)
>
> (((TraceBackFrame)(tb.ScopeScope)).Code)).Filename;

****Line pragmas for debugging support****

MerlinWeb generates code for code found in .aspx files. For debuggers and tools to present the right info to users, MerlinWeb needs to give hints. Also, code in a single scope may come from more than one source location.

Currently, this is not well supported. For example, if you have this on an .aspx page:

> &lt;%
>
> for i in range(max):
>
> %&gt;
>
> i=&lt;%=i%&gt; i\*i=&lt;%=i\*i%&gt;&lt;br /&gt;

MerlinWeb will generate code that looks like this:

> for i in range(max):
>
> \_\_param.RenderControl(0)
>
> \_\_param.Render(i)
>
> \_\_param.RenderControl(1)
>
> \_\_param.Render(i\*i)
>
> \_\_param.RenderControl(2)

Note that this code contains a mix of user code (in green) and generated code. Furthermore, there is no correlation between the line numbers of the user code in the aspx and in the generated code. By using a line pragma mechanism, we can tell the compiler exactly where each snippet of code came from, which allows the aspx file to be debugged directly.

<h2 id="base-tools-support">3.3 Base Tools Support</h2>

This section lists tools scenarios and requirements.

<h3 id="general-hosting-scriptengineserver">3.3.1 General Hosting (ScriptEngineServer)</h3>

****Inject global name bindings.**** The tool needs to provide name bindings that are global across a ScriptRuntime, that is, across one or more script engines. The tool needs to inject one or more objects to provide necessary functionality to script engines running the tool’s command implementations.

****Host script engines remotely or isolated (including language tool support).**** The tool is running one ScriptRuntime for its commands, and it wants to run one remotely for the program the developer is working on. The remote ScriptRuntime for running users programs may need to be torn down, restarted, or debugged (stopping all threads) without putting the tool’s ScriptRuntime at risk. Users should never lose work because the ScriptRuntime for tools commands gets messed up to where users cannot save files. However, if users are interactively developing for the tool’s command ScriptRuntime, they might mess up the run time enough to render the editor useless. The language tool support may not run remotely, but it is local, it would need access to its associated script engine for some operations as well as some sort of RPC to it.

****Eval code from string to get CLR object back.**** This supports hosts like future C\# language support for interacting with dynamic objects as well as hosts like MerlinWeb that use dynamic languages to perform work within the same app domain and want to point at objects that come back. The eval operation can raise an exception, and if it does, the tool wants to catch the exception and call on the script engine for its (language-specific) reporting or formatting of error to get a string to print.

****Get a string representation of eval results.**** The tool host should either be able to call ToString on an object returned from eval to get a reasonable language-specific print representation, or the result of an eval operation is a string representing the result that the tool can print. For remote hosting, the tool should NOT have to call ToString on a proxy object, roundtripping with the script engine again to print results.

****Interrupt ScriptRuntime to stop all processing in script engines.**** The ScriptRuntime and script engines server objects run on threads that still respond so that the host can interact with the ScriptRuntime. This action either throws to top level or drops into the debugger depending on what we can do (debugger is preferable, at least getting a stack trace) and what the host chooses to do. This needs to work for aborting an editor command written in a script language (just throws to the editor’s top-level input loop). It also needs to work for the interaction window where you’re likely to want to land in the debugger for you program.

****Ask for reflection information language-independently.**** Tool hosts want to get the following:

- members lists from an identifier and context (scope or finer-grain parsing context)

- doc strings for types, members, and objects

- parameter names and types (if available)

Regardless of whether objects are from static .NET languages/assemblies or any given script engine, tool hosts call general hosting methods. Hosts do not need to know how to express such queries in each language and then evaluate expressions to get this information.

****Reset scope or ScriptRuntime.**** Want to be able to reset a scope (definitions, global bindings, etc.) and reload it. Want to be able to reset a default scope with no code or file associated with it so that you can start fresh interactions. Want to be able to reset the entire ScriptRuntime (all engines), tearing down all engines, so that all the execution state you’re working with is reset for a clean program execution. Not sure if we also need to be able to reset an engine without affecting other running engines, but we think this case collapses together with the reset the whole ScriptRuntime. A scenario for resetting the whole ScriptRuntime is if the tool has a primary script buffer designated so that F5 in any window means reset the entire ScriptRuntime and start again running the primary script.

ISSUE: Unless we're forced to support it, resetting a scope does NOT address aliases to old values. For example, I might have script A.py and B.vbx, and I’ve used "from B import \*" in A's scope. If I reload B.vbx, what happens to the bindings created in A that alias B’s old definitions and values? My instinct, and the Python expectation is that A's bindings to B's defs and globals do not change with the reload. We need to think about whether there should be a requirement to support resetting those bindings on B's reload. Note, if A.py only refers to the object that is the scope for B.vbx, then reloading B and later "dotting" into its scope will reveal new values.

<h3 id="language-tool-support-iscriptengine-or-ilanguagetoolservice">3.3.2 Language Tool Support (IScriptEngine or ILanguageToolService)</h3>

****Fetch runtime banner per script engine.**** When an interactive window comes up, we want users to see a banner that is consistent with what they expect from using interpreters for their language on other community tools. We may not show the banner in some situations, but certainly if the user launches the tool as an interpreter window (vs. as an editor first), then we want to show a familiar introductory banner.

****Fetch standard language interpreter prompt.**** Again, we want to provide an interaction window experience that is similar to what a language community expects, so we want to be able to ask a script engine what its prompt looks like.

****Fetch standard language debugger prompt.**** Again, we want to provide an interaction window experience that is similar to what a language community expects, so we want to be able to ask a script engine what its prompt looks like when it is in a debugging context.

****Update the prompt.**** For languages where the prompt changes over time, or with different modes (for example, a number representing the stack frame to support frame\_goto commands), we should have something in our API for allowing the script engine to push “next prompt” text to the host. This could be as simple as all script engines have a property that can be fetched each time a prompt needs to be printed. Note, this does not mean a tool should be in a special input mode and have a multi-line input prompt like IronPython does today. We should auto-indent and just detect on enter whether input is ready for evaluation or not, allowing editing of previous lines and so on.

****Get token/coloring info for a language.****

****Tokenizer can act on “whole buffers/files” or expression fragments.**** Fragments can come from an editor file buffer or the interaction window.

****Tokenizer is restartable at a designated location in a buffer (stream?).**** Tools need to incrementally request parsing and lexical analysis information at any point in the buffer. It’s fine if the tool has to record cookies for each line so that the parser has some state to leverage to restart parsing.

****Parse for possibly complete expression.**** Can pass a string to find out tokenizing info as well as determine if it is a complete expression/statement suitable for evaluation.

****Fetch line-oriented comment start sequence.**** This is needed so that we can do comment and uncomment region.

****Accept redefinitions regardless of live instances or active stack frames.**** We should be able to redefine functions even if there are pending calls to the function on the stack. Pending calls should continue execution of the old definition, and new calls should call the new definition. We should be able to redefine types even if there are existing instances, at least during interactive development. Declarations or compiler switches could harden definitions for optimizations if languages support that. Fetching members of old instances could fault to updated dual instances from the new definition, possible raising an exception if a member is uninitialized.

<h3 id="static-analysis-post-mix-07">3.3.3 Static Analysis (Post MIX 07)</h3>

****Get language info regardless of where object comes from.**** If a language has a variable that it knows is (will be) bound to a type from another language’s script/scope, there should be a way to get member completion for that type if the other language can provide it. Maybe the other file needs to be opened, or maybe the file needs to be loaded into a script engine. The API the host calls on should be language agnostic for this support.

****Get member completion of symbol at a buffer location.**** If a language has enough declared type info or if it has enough static analysis of variable lifetimes and known result types of expressions, a tool would want to get completion, doc strings, etc., for those variables. The tool should have an API to ask for it in case some languages do have this.

<h2 id="what-languages-and-dlr-need-to-support">3.4 What Languages and DLR Need to Support</h2>

This section captures characteristics of a hostable language via DLR APIs. These characteristics are a blend of requirements and high-level "work items". The requirements are from hosts, such as application programmability hosts, that could have been listed in previous sub sections. The work items are functionality a language plugging into the DLR would need to support in its parser and binder (or perhaps in their compiler if they compiled all the way to IL on their own).

**Discoverability:** Language is discoverable from DLR hosting registration and loadable via hosting API.

**Globals:** Language can execute code that can access the runtime’s global scope (global object). The language may resolve free references to variables in the global scope, or have its imports/require/using mechanism provide access to names bound on the global scope object. The language could provide library or builtin functions for accessing the variables, but this could also be cumbersome to programmers.

**Scopes:** Language can execute code in a host supplied scope, resolving free references to variables in the scope or some other linguistic mechanism. The language could provide library or builtin functions for accessing the variables, but this could also be cumbersome to programmers.

**LanguageContext:** Language implements a LanguageContext which supports many hosting APIs (such as ScriptEngine), compiling, creating scopes, working with ScriptSource objects, etc.

**ScriptHost:** Language defers to ScriptHost for getting/resolving sources for import/require/using features.

**Interrupt Execution:** Language supports DLR mechanisms for host to abort execution cleanly so that host can interrupt runaway code. If we allow languages to avoid this work, then those langs cannot be used in the host's process, cannot support in-situ REPLs for interactive development, etc. If the language uses DLR compilation or interpretation, they should not need to do anything. This is important because if the host has to stop execution by calling Thread.Abort or other rude abort, the thread could have transitioned into an unsafe portion of the host's code. This could happen via the script code calling back on the host OM which in turn calls an internal API not coded for rude aborts.

**Limit Stack:** Language supports DLR mechanisms for host to control maximum stack depth. If we allow languages to avoid this work, then those langs cannot be used in the host process, cannot support in-situ REPLs for interactive development, etc. If the language uses DLR compilation or interpretation, they should not need to do anything. This requirement is motivated by similar concerns to the execution interrupt requirement.

**Debugging:** Language supports DLR mechanisms for cooperative debugging or interpreted debugging so that hosts can use the DLR in the same process and app domain while not running all of its managed code at the speed of debug mode. If the language uses DLR compilation or interpretation, it should not need to do anything.

**Exceptions Filter:** Language supports an exception filter call back to the host. A scenario is that hosts may provide access to components (WPF control) that could execute hosted code outside of the host's entry points (cmd invocation, host OM event handler, etc.). The host would like to be able to catch all hosted code exceptions to cleanly present the issue to the end user or cleanup the host’s OM state as appropriate. If the language uses DLR compilation or interpretation, it should not need to do anything.

**P2 Memory Limit:** Language supports a limit on memory allocation so that the host can somehow nearly drive to zero the chance that running some hosted code will use up too much memory for the host to cleanly shutdown or clean up the hosted language runtime.

**P2 Disabled Methods:** language supports a list of methods that cannot be invoked, and if the hosted language tries to do so, the host call back for exception filtering is invoked. For example, on a shared public server that runs user code, allowing code to invoke Environment.FailFast is probably a bad idea.

**Degree of Interop support:**

- Level 1 -- Hosts can consume the language (as above), and language can consume globals/locals and .NET objects. Nothing more needed. Programmers would need to cast globals to known .NET types, or language would need to provide its own reflection-based late binding support.

- Level 2 -- Hosts can consume language, and language interops with DLR languages and dynamic objects. Language then needs to compile operations to DynamicSites (or generate DLR ASTs with ActionExpr nodes). Language may not need to produce any ASTs still since the default binder may be sufficient fall back when DynSites encounter IDynObjs that do not produce a rule for the operation at the site. If the default binder’s semantics aren’t good enough, the language would have to produce ASTs for tests and targets when asked for a rule in the DynSite.

- Level 3 -- Language is built on DLR or can produce full ASTs for interpreted mode, complete integration, etc. NOT SURE THIS IS A LEVEL, but could impact whether language is debuggable and can interact with host while debugging (depends on debugging story).

**Aspects of tool support (optionally supporting each):**

- Colorization -- lang produces TokenCategorizer service from LanguageContext

- Completion, Param info -- if we have static analysis service for tool support, then lang produces service. Probably nothing to do here if completion is all about live objects (other than support get doc and get sigs members/ops on LanguageContext).

- REPLs -- lang parser can return code properties at end of parse and support interactive execution (for example, is this snippet complete enough to execute or recognizing special REPL syntax or variables)

- Debugging -- story still under development here, may be nothing language needs to do, may be about AST

- NOT saying anything here about VSTA, VSA, VBA, etc., style hosting (project model, code model events, UI, host embedded UI, extension distribution, project persistence and discovery, etc.)

<h2 id="configuration-notes">3.5 Configuration Notes</h2>

These are somewhat raw notes from various conversations on hosting configurations requirements, goals, and scenarios.

<h3 id="requirements">3.5.1 Requirements</h3>

Hosts need to have full control of config. (what langs are available, what versions, how code executes, etc.).

Config and options representation has to be very pliable and extensible between host, DLR, and languages. (likely means Dict&lt;str,obj&gt;).

There needs to be two levels of config (one for engine IDs, names, and LCs, and then one for per engine opts).

We want as simple a model as possible without rolling our own mechanisms.

Assuming two language versions work on same CLR version, should be able to use both.

Conflicting langIDs (simple names) are host’s job to rectify (or to provide affordances for end-user to rectify).

LangIDs map to an assembly-qualified type that is the LanguageCtx that the hosting API implementations need to instantiate and hook up.

LangIDs are compared case-insensitively.

Languages need to be able to merge options from host that come through DLR (they know their defaults, but hosts shouldn’t have to supply a full set of options to change a few).

There is some affordance for file exts mapping to engines, but this could break down, leaving hosts to use langIDs.

There is a display name property that we identify for how we pass into from host to DLR, but we don’t have to standardize on how hosts get that or languages provide it.

GAC’ing is NOT required (that is, can be avoided in all end-to-end scenarios).

<h3 id="goals">3.5.2 Goals</h3>

These are characteristics we'd like to satisfy, but we may not satisfy them all. In fact, some are mutually exclusive.

- Hosts should be able to discover what languages are available for the default DLR usage on the machine.

- Xcopy installation should be possible.

- An application using the DLR should be able to run from a USB drive.

<h3 id="scenarios">3.5.3 Scenarios</h3>

End user uses one or more applications that utilize DLR scripting solution with no special actions on behalf of the end-user, he can use VB and C\# as hosted languages (once they're hostable).

End-user uses one or more applications that support end-user options for scripting language choice.

- End-user installs IronPython and IronRuby on his machine.

- End-user may have to update each app (which could have different user options models) to identify appropriate DLLs, .NET types for language, and default options.

Barring language implementer’s decisions, end-users use v1 and v2 of same language (that is, the DLR is not the gating factor), even in the same ScriptRuntime.

Host wants to tag sources with test/python type (or a web page with "language=python"), then just grab that string to map to an engine where it processes the page. It uses langIDs that map to an engine's assembly-qualified type name to avoid having to update pages all over a web site (and for more readable code, simple name vs. AQTN).

Internal test frameworks and code submission tools work for testing DLR and configurations.

Hosts may need to associate scripts with specific engine implementations

- It is okay (and consistent with other guidance) that they have to manage that association and config data themselves, then reconstitute it.

- Languages must do the work to be able to be SxS, if there’s anything they’d have to do.

<h2 id="remote-construction-design-notes">3.6 Remote Construction Design Notes</h2>

These are rough notes on making remote construction easier for consumers.

We finally decided to go with constructors over factory methods for a couple of reasons. The first is that constructors are more natural when factories aren't needed. The second is that having factories only made the remote construction more tedious and hard to get right.

We kept the static CreateRemote factory for discoverability, but it was much simpler to implement with the constructors. Going to constructors from factory methods would allow you to write these two "lines" instead of the helper class et al needed with only factory methods.

> AppDomain.CreateInstanceAndUnwrap
>
> (typeof(ScriptRuntime).Assembly.FullName,
>
> typeof(ScriptRuntime).FullName);

And if you want to pass ScriptRuntimeSetup (as value of setup variable below):

> AppDomain.CreateInstanceAndUnwrap
>
> (typeof(ScriptRuntime).Assembly.FullName,
>
> typeof(ScriptRuntime).FullName, false,
>
> BindingFlags.Default, null, new object\[\] { setup },
>
> null, null, null);

If we only had factory methods, and we did not provide this factory for users:

runtime = ScriptRuntime

.Create(AppDomain.CreateDomain("foo"), setup);

Then they would have to write:

runtime = RemoteRuntimeFactory

.CreateRuntime(AppDomain.CreateDomain("foo"), setup);

private sealed class RemoteRuntimeFactory : MarshalByRefObject {

public readonly ScriptRuntime Runtime;

public RemoteRuntimeFactory(ScriptRuntimeSetup setup) {

Runtime = ScriptRuntime.Create(setup);

}

public static ScriptRuntime CreateRuntime

(AppDomain domain,

ScriptRuntimeSetup setup) {

RemoteRuntimeFactory rd

= (RemoteRuntimeFactory)domain

.CreateInstanceAndUnwrap

(typeof(RemoteRuntimeFactory)

.Assembly.FullName,

typeof(RemoteRuntimeFactory).FullName,

false, BindingFlags.Default, null,

new object\[\] { setup }, null, null, null);

return rd.Runtime;

}

}
