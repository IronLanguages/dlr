# 23 Appendixes

These sections show using DLR APIs that are available on codeplex.com only for now. Some will move into CLR versions beyond 4.0. These APIs let you provide more features for your language more easily, such as generators (functions with yield expressions) or richer .NET binding logic by using the DefaultBinder. These APIs sometimes improve performance over the base Sympl implementation, such as using the namespace/type trackers that IronPython uses.

The source is in the ...\\languages\\sympl\\csharp-cponly directory, separated from the version that only depends on CLR 4.0 APIs, for a cleaner code sample. This will be more useful when this version supports more codeplex-only APIs. While the changes for each appendix may not need to be big, it should be cleaner to have sources that are not riddled with "\#if cponly". Two sets of sources support using 'diff' tools to see changes more readily.

<h2 id="supporting-the-dlr-hosting-apis">23.1 Supporting the DLR Hosting APIs</h2>

This section shows a minimal LanguageContext implementation so that applications can host Sympl using the common hosting model provided by the DLR. This means Sympl could seamlessly be added to any host using these APIs for multi-language scripting support. The Main function in program.cs shows executing Python and Ruby code in the same scope or module that Sympl uses, cross-language interoperability, and accessing host supplied globals.

The changes comprise one small new file and a few tweaks to a couple of other files. The changes show using the hosting model's Globals table where hosts inject global bindings for script code to access the hosts' object models. The changes show how to represent your language as a DLR ScriptEngine and how to run code in the DLR hosting model. They also show how Sympl can expose extension services to the host.

A LanguageContext can participate in hosting by providing more functionality than what is shown here. For example, your language can support a tokenizing/colorizing service, error formatting service, ObjectOperations, execute program semantics, configuration settings such as search paths, and so on (see the dlr-spec-hosting.doc on codeplex.com/dlr). The Sympl sample shows how to get code to run in a host that supports common DLR Hosting APIs and how to provide extension services.

<h3 id="main-and-example-host-consumer">23.1.1 Main and Example Host Consumer</h3>

Let's start top-down and look at how program.cs changed in Main. This is a portion of the code:

static void Main(string\[\] args)

{

var setup = new ScriptRuntimeSetup();

string qualifiedname =

typeof(SymplSample.Hosting.SymplLangContext)

.AssemblyQualifiedName;

setup.LanguageSetups.Add(new LanguageSetup(

qualifiedname, "Sympl", new\[\] { "sympl" },

new\[\] { ".sympl" }));

setup.LanguageSetups.Add(

IronPython.Hosting.Python.CreateLanguageSetup(null));

setup.LanguageSetups.Add(IronRuby.Ruby.CreateRubySetup());

var dlrRuntime = new ScriptRuntime(setup);

var engine = dlrRuntime.GetEngine("sympl");

string filename = @"..\\..\\Languages\\sympl\\examples\\test.sympl";

var feo = engine.ExecuteFile(filename);

Console.WriteLine("ExecuteExpr ... ");

engine.Execute("(print 5)", feo);

// Get Python and Ruby engines

var pyeng = dlrRuntime.GetEngine("Python");

var rbeng = dlrRuntime.GetEngine("Ruby");

// Run some Python and Ruby code in our shared Sympl module.

pyeng.Execute("def pyfoo(): return 1", feo);

rbeng.Execute("def rbbar; 2; end", feo);

// Call those objects from Sympl.

Console.WriteLine("pyfoo returns " +

(engine.Execute("(pyfoo)", feo)).ToString());

Console.WriteLine("rbbar returns " +

(engine.Execute("(rbbar)", feo)).ToString());

// Consume host supplied globals via DLR Hosting.

dlrRuntime.Globals.SetVariable("DlrGlobal", new int\[\] { 3, 7 });

engine.Execute("(import dlrglobal)", feo);

engine.Execute("(print (elt dlrglobal 1))", feo);

// Drop into the REPL ...

... deleted code ...

var s = engine.GetService&lt;Sympl&gt;();

while (true) {

... deleted code ...

try {

object res = engine.Execute(exprstr, feo);

exprstr = "";

prompt = "&gt;&gt;&gt; ";

if (res == s.MakeSymbol("exit")) return;

Console.WriteLine(res);

... deleted code ...

This code creates a ScriptRuntimeSetup and fills it in with specific LanguageSetups for Sympl, IronPython, and Ironruby. An application may offer script engines to its users language-independently and work with newly added engines without changing its code. Application can do this by using an app.config file. See the dlr-spec-hosting.doc on codeplex.com/dlr. This example uses the convenience LanguageSetup factory functions from IronPython and IronRuby to get default setups for just those languages. If you use an app.config file, ScriptRuntime has a factory method that reads the app.config file and returns a configured ScriptRuntime.

Next the Main function runs the same Sympl test file as the other version of Sympl, followed by running IronPython and IronRuby code in the same file module or scope. Then Main executes Sympl expressions that use the IronPython and IronRuby objects. The example could have also loaded IronPython and IronRuby files and added the ScriptScopes they returned to the feo module's globals. Calling across modules and "dotting" into members to get at IronPython and IronRuby objects would also work.

Lastly, Main installs host globals using the DLR's common hosting APIs. It binds the name dlrglobal to a .NET array. Then it executes Sympl code to import the name into the file's module, and then executes code to use the values imported. This is the IronPython model of accessing DLR host globals. IronRuby simply does global lookups by chaining up to the DLR's globals table automatically.

The last bit worth noting is that the Sympl support for DLR Hosting APIs supports fetching a service from the Sympl engine. The Sympl engine lets the host fetch the inner Sympl hosting object, which happens to be the hosting object first defined in the other version of Sympl. The Main function uses this to make Sympl symbol objects so that the REPL can continue testing for 'exit to stop the session.

<h3 id="runtime.cs-changes">23.1.2 Runtime.cs Changes</h3>

The changes to runtime.cs are few and really simple. The first is that Sympl no longer uses ExpandoObjects for its file modules. It must use the language-implementation Scope type. Sympl must wrap this to make it into an IDynamicMetaObjectProvider because its file modules are just dynamic objects and can be passed around as such. The only change in runtime.cs for this support is renaming ExpandoObject to IDynamicMetaObjectProvider.

The second change is in the SymplImport runtime helper function. When importing a single name (that is, not a dotted name sequence), SymplImport now looks in Sympl.DlrGlobals if the name is not in Sympl.Globals. The branch where there are multiple names to look up, successively fetching a member of the previous resulting object, should change too. It should handle this same check for DlrGlobals for the first name. It also should change in all versions of Sympl to not assume the objects are dynamic objects. It could explicitly create a CallSite with a SymplGetMemberBinder to be able to handle any kind of object (dynamic or static .NET).

<h3 id="sympl.cs-changes">23.1.3 Sympl.cs Changes</h3>

There are three basic changes to sympl.cs. It needs to change ExpandoObject to IDynamicMetaObjectProvider for the file module becoming DLR Scopes, as mentioned in the previous section. The Sympl class needs to provide a couple more entry points for how the DLR Hosting APIs call on the new Sympl LanguageContext to parse and run code. Lastly, the Sympl class now keeps a reference to the DLR's ScriptRuntime.Globals Scope so that Sympl programs can import host supplied globals for the host's object model.

The new entry points needed support the LanguageContext's CompileSourceCode method (see the section on dlrhosting.cs). The Sympl class now has ParseFileToLamba and ParseExprToLambda. Creating these was a straightforward extraction refactoring of ExecuteFile and Execute Expr, so there's nothing new to explain about how that code works.

The last change was to make the Sympl class constructor take a DLR Scope, which is the ScriptRuntime's Globals table. See the section on changes to runtime.cs for more information, but the change support host globals was a new else-if branch and a couple of lines of code.

<h3 id="why-not-show-using-scriptruntime.globals-namespace-reflection">23.1.4 Why Not Show Using ScriptRuntime.Globals Namespace Reflection</h3>

You might notice that the Sympl hosting class still calls AddAssemblyNamespacesAndTypes to build its own reflection modeling. It could use the DLR's ScriptRuntime.Globals which has names bound similarly for namespaces and types. There are a couple of issues with Sympl's using the DLR's reflection.

The first is that the DLR Globals scope effectively includes two dictionaries, a regular DLR Scope and NamespaceTracker (or a TypeTracker). The DLR's Globals scope looks names up both dictionaries. The NamespaceTracker object has a bug in that it fails to look up SymbolIDs case-INsensitively, which the Scope object does correctly. This means the Sympl expression "(import system)" fails to find the name "system". One way to work around this would have been to fetch all the keys from the Globals scope and compare each one for a case-INsenstive match. If there was only one, then return successfully. Furthermore, Sympl's binders would need updating to explicitly notice when a NamespaceTracker or TypeTracker flowed into the call site (say, for "system.console" binding) and have special binding logic for the trackers.

The second reason Sympl continues to build its own reflection modeling is that the DLR may fix the NamespaceTracker and related types to flow as dynamic objects with correct lookup behavior. The DLR might remove them entirely and no longer push them into the ScriptRuntime's Globals. Each language would then have to have its own reflection modeling like Sympl. The DLR would either have helpers, or people could copy code from IronPython to get the benefits of the tracker objects.

<h3 id="the-new-dlrhosting.cs-file">23.1.5 The New DlrHosting.cs File</h3>

This file is where the more interesting new code is. Primarily this file defines a LanguageContext, which is the representation of a language or execution engine to the DLR Hosting APIs. The LanguageContext in Sympl does two things. It supports compiling code, which can be run in a new DLR scope or in a provided scope. It also provides a service that returns the inner Sympl hosting object. The LanguageContext could do more, such as supporting a tokenizing/colorizing service, error formatting service, ObjectOperations, execute program semantics, etc (see the dlr-spec-hosting.doc on codeplex.com/dlr). The Sympl sample mostly just shows how to get code to run in a host that supports common DLR Hosting APIs.

**Here is the code for the SymplLangContext in dlrhosting.cs:**

public sealed class SymplLangContext : LanguageContext {

private readonly Sympl \_sympl;

public SymplLangContext(ScriptDomainManager manager,

IDictionary&lt;string, object&gt; options)

: base(manager) {

\_sympl = new Sympl(manager.GetLoadedAssemblyList(),

manager.Globals);

}

protected override ScriptCode CompileSourceCode(

SourceUnit sourceUnit, CompilerOptions options,

ErrorSink errorSink) {

using (var reader = sourceUnit.GetReader()) {

try {

switch (sourceUnit.Kind) {

case SourceCodeKind.SingleStatement:

case SourceCodeKind.Expression:

case SourceCodeKind.AutoDetect:

case SourceCodeKind.InteractiveCode:

return new SymplScriptCode(

\_sympl,

\_sympl.ParseExprToLambda(reader),

sourceUnit);

case SourceCodeKind.Statements:

case SourceCodeKind.File:

return new SymplScriptCode(

\_sympl,

sympl.ParseFileToLambda(sourceUnit.Path,

reader),

sourceUnit);

default:

throw Assert.Unreachable;

}

}

catch (Exception e) {

errorSink.Add(sourceUnit, e.Message,

SourceSpan.None, 0,

Severity.FatalError);

return null;

public override TService GetService&lt;TService&gt;(

params object\[\] args) {

if (typeof(TService) == typeof(Sympl)) {

return (TService)(object)\_sympl;

}

return base.GetService&lt;TService&gt;(args);

}

The key method is CompileSourceCode, which returns a SymplScriptCode object that is discussed below. There are several kinds of code the DLR might ask the language to compile. These kinds allow languages to set initial parser state (such as SourceCodeKind.Expression vs. SingleStatement) or to apply special semantics for magic interactive loop syntax or variables (SourceCodeKind.InteractiveCode). Sympl buckets all of these into either the Expression or File kind because Sympl doesn't need finer-grain distinctions. These two branches just call the new entry points in the Sympl class.

Regarding the catch block, a more serious language implementation would have a specific type of exception for parse errors. It would also pass the errorSink down into the parser and add messages while doing tighter error recovery and continuing to parse when possible. For the Sympl example, it just catches the first error, passes it to the errorSink, and punts.

**The SymplScriptCode returned from CompileSourceCode above represents code to the DLR Hosting APIs.** Sympl defines its ScriptCode as follows:

public sealed class SymplScriptCode : ScriptCode {

private readonly Expression&lt;Func&lt;Sympl,

IDynamicMetaObjectProvider,

object&gt;&gt;

\_lambda;

private readonly Sympl \_sympl;

private Func&lt;Sympl, IDynamicMetaObjectProvider, object&gt;

\_compiledLambda;

public SymplScriptCode(

Sympl sympl,

Expression&lt;Func&lt;Sympl, IDynamicMetaObjectProvider, object&gt;&gt;

lambda,

SourceUnit sourceUnit)

: base(sourceUnit) {

\_lambda = lambda;

\_sympl = sympl;

}

public override object Run() {

return Run(new Scope());

}

public override object Run(Scope scope) {

if (\_compiledLambda == null) {

\_compiledLambda = \_lambda.Compile();

}

var module = new SymplModuleDlrScope(scope);

if (this.SourceUnit.Kind == SourceCodeKind.File) {

DynamicObjectHelpers.SetMember(

module, "\_\_file\_\_",

Path.GetFullPath(this.SourceUnit.Path));

}

return \_compiledLambda(\_sympl, module);

There are three interesting notes on this code. The first is that we parse to a type of lambda just like Sympl does in the other version. In this version the type of the LambdaExpression is the same for both executing files and executing expressions because the DLR hosting expects a return value in both cases. In the other version, Sympl's file LambdaExpressions returned void. Now Sympl just returns null from each file lambda. The next point is that Sympl needs to wrap the Scope passed to the Run method. Sympl does this so that the DLR scope passed to Run can be passed around in Sympl code as a dynamic object. The last point is that before invoking the compiled lambda, the Run method stores the file module variable "\_\_file\_\_" so that Sympl's 'import' expressions work correctly. Sympl.ExecuteFile still stores "\_\_file\_\_" also.

**Lastly, DlrHosting.cs defines a wrapper class for DLR language-implementation Scopes so that Sympl can pass them around as dynamic objects:**

public sealed class SymplModuleDlrScope : DynamicObject {

private readonly Scope \_scope;

public SymplModuleDlrScope(Scope scope) {

\_scope = scope;

}

public override bool TryGetMember(GetMemberBinder binder,

out object result) {

return\_scope.TryGetName(

SymbolTable.StringToCaseInsensitiveId(binder.Name),

out result);

}

public override bool TrySetMember(SetMemberBinder binder,

object value) {

\_scope.SetName(SymbolTable.StringToId(binder.Name), value);

return true;

To get an easy implementation of IDynamicMetaObjectProvider, Sympl uses the DynamicObject type. It is a convenience type for library authors that enables you to avoid implementing a binder and generating expression trees. DynamicObject implements the IDynamicMetaObjectProvider interface by always returning a rule that calls Try... methods on the DynamicObject.

Sympl only needs to support GetMember and SetMember operations since it doesn't have any special InvokeMember semantics or other special behaviors for its file scopes. As described in the main sections of this document, Sympl stores IDs as they are spelled and stored in the binder's metadata. However, Sympl looks IDs up case-INsensitively. Since the underlying DLR Scope objects use SymbolID objects to represent names, Symp has to map the binder's Name property to an appropriate SymbolID object.

<h2 id="using-the-codeplex.com-defaultbinder-for-rich-.net-interop">23.2 Using the Codeplex.com DefaultBinder for rich .NET interop</h2>

Using the default binder gives you a complete .NET interoperability story. It has many customization hooks to tailor how it binds to .NET. IronPython and IronRuby use it as well as other language implementations. We'll demonstrate using it to get richer .NET binding and to convert Sympl nil to .NET False, TypeModel to .NET RuntimeType, and Cons to some .NET sequence types.

TBD

<h2 id="using-codeplex.com-namespacetype-trackers-instead-of-expandoobjects">23.3 Using Codeplex.com Namespace/Type Trackers instead of ExpandoObjects</h2>

For a faster and richer implementation of how Sympl provides access to .NET namespaces and types, we use the DLR's reflection trackers.

TBD

<h2 id="using-codeplex.com-generatorfunctionexpression">23.4 Using Codeplex.com GeneratorFunctionExpression</h2>

It is very easy to add generators or what C\# calls iterators (functions with 'yield' expressions) to your language. The support on Codeplex for this is very solid and used by IronPython and other languages. The only reason it didn't ship in CLR 4.0 is that the implementations has a couple of python-specific features that we did not have time to parameterize into a general model for usage.

TBD
