# 4 Runtime

It is hard to separate language implementation concepts from the runtime concepts with dynamic languages. However, we try to do so by defining the runtime aspects of the DLR as DynamicSites, SiteBinders, and Rules for fast dynamic invocation. We include higher-level objects as helpers for library authors who want their objects to participate well in dynamic operations -- DynamicObject and ExpandoObject. We also include utilities, default binding helpers, and COM IDispatch interoperability.

<h2 id="dynamic-call-sites">4.1 Dynamic Call Sites</h2>

Dynamic call sites allow dynamic language code to run fast. They manage the method caching for performing specific operations on specific types of objects. The dynamic sites mechanism the DLR uses is based on research and experience with tried-and-true dynamic language implementations. For deeper discussion than provided here search the web for dynamic language method caching or polymorphic inline method caching.

Dynamic language performance is hindered by the extra checks and searches that occur at each call site. Straightforward implementations have to repeatedly search class precedence lists for members and potentially resolve overloads on method argument types each time you execute a particular line of code. In an expression such as "o.m(x, y)" or "x + y", dynamic languages need to check exactly what kind of object o is, what is m bound to for o, what type x is, what type y is, or what '+' means for the actual runtime types of x and y. In a statically typed language (or with enough type hints in the code and using type inference), you can emit exactly the instructions or runtime function calls that are appropriate at each call site. You can do this because you know from the static types what is needed at compile time.

Dynamic languages provide great productivity enhancements and powerful terse expressions due to their dynamic capabilities. However, in practice code tends to execute on the same types of objects each time. This means you can improve performance by remembering the results of method searches the first time a section of code executes. For example, with "x + y", if x and y are integers the first time that expression executes, we can remember a code sequence or exactly what runtime function performs addition given two integers. Then each time that expression executes, there is no search involved. The code just checks that x and y are integers again, and dispatches to the right code with no searching. The result can literally reduce to inlined code generation with a couple of type checks and an add instruction depending on the semantics of an operation and method caching mechanisms used.

<h3 id="before-dynamic-call-sites">4.1.1 Before Dynamic Call Sites</h3>

To step back a moment, let's look at what language implementations did before polymorphic inline caching. They would use runtime helper methods that open-coded all the checks for common type combinations that showed up in applications for different kinds of operations. For example, in the original implementation of IronPython before the DLR, there were vast quantities of code in associated helper classes that looked like this:

``` csharp
object Add(object x, object y) {
    if (x is int) return IntOps.Add((int)x, y);
    if (x is double) return DoubleOps.Add((double)x, y);
    // ... lots more special cases ...
    // Fall back to reflecting over operand types for op_Addition method
}
```

… with this static method in the IntOps class:

``` csharp
object Add(int x, object y) {
    if (y is int) return x + (int)y; // modulo overflow handling
    if (y is double) return (double)x + (double)y;
    // ... lots more special cases ...
}
```

This was unfortunate for two reasons. One problem was that it bloated the resulting IronPython assemblies with optimized fast-paths for thousands of cases (about 180kb in the DLL). The larger issue, however, was that this only optimized for classes were known when the fast-paths were fixed. Any classes with user-defined conversions end up falling back to the full search reflection case on each execution of "x + y".

By implementing an adaptive caching system that caches the implementations for the actual sets of types observed by a given dynamic call site, both the generally common cases and the cases specific to a given application can be fast.

<h3 id="language-interoperability">4.1.2 Language Interoperability</h3>

Assuming you were willing to do the optimizations discussed above, it would be possible to create a high-performing .NET dynamic language today without the DLR, as the DLR is simply an API that sits on top of the CLR and does not require any new built-in CLR features. This is, in fact, what the first versions of IronPython did, directly compiling Python code to IL that called run-time helper methods defined by IronPython. As the IronPython compiler knew about the semantics of standard .NET types, it could also provide a great experience targeting the BCL and existing C\#/VB assemblies as well.

However, this story breaks down when you want an IronPython object to call into, for example, an IronRuby library and dynamically invoke functions on the objects it gets back. With no standard mechanism to understand how to perform dynamic operations on an external dynamic object, IronPython could only treat the object as a .NET object, accessing its statically-known API. Most likely any visible methods will be an implementation detail of IronPython rather than a representation of the user’s intent.

The DLR provides for this common currency when calling into objects defined in other dynamic languages by establishing a protocol for those implementing dynamic objects. As a language creator or library author who wants other DLR-aware languages to consume your objects dynamically, you must have the objects implement IDynamicMetaObjectProvider, offering up appropriate implementations of DynamicMetaObject when IDynamicMetaObjectProvide's GetDynamicMetaObject() is called.

<h3 id="creating-dynamic-call-sites">4.1.3 Creating Dynamic Call Sites</h3>

At a high level there are two ways to create dynamic call sites. A call site encapsulates a language's binder and the dynamic expression or operation to perform. A regular .NET language compiler (for example, what C\# does for its 'dynamic' feature) emits a call site where dynamic expressions occur. The language can create the ahead of time, stash them in a functions constants pool, put them in static types, or whatever. The compiler then emits a call to the site's Target delegate to invoke the dynamic operation.

Dynamic languages built completely on the DLR, such as IronPython, use DynamicExpression nodes from Expression Trees v2 (which the DLR ships in .NET 4.0). The Expression.Compile method builds the call site objects and emits the right calls on their Target delegate when compiling.

<h2 id="rules">4.2 Rules</h2>

Each dynamic call site keeps track how to perform the operation it represents. It learns different ways to perform the operation depending on the data (operands) that flows into the site. The binder provides these implementations of the operation along with restrictions as to when the site can correctly use the implementations. We refer to these restrictions and implementations together as rules. The call site compiles the rules it sees into a target delegate that embodies the site' cache.

To explore rules, let’s consider the case of adding two dynamic operands in a DLR-aware language:

``` csharp
d1 + d2
```

Let’s assume that the first time we hit this expression, both operands have a runtime type of int. The operations’ dynamic call site has not yet bound any implementations for this addition site, and so it will immediately ask the runtime binder what to do. In this case, the runtime binder examines the types of the operands and decides to ensure that each operand is an int and then performs addition. It therefore returns an expression tree representing this:

``` csharp
(int)d1 + (int)d2
```

This expression tree serves as the **implementation**, which is the specific meaning the runtime binder has given to this dynamic expression in this exact situation. This expression tree alone would be sufficient to finish evaluating this expression, as it could be wrapped in a lambda expression, compiled into a delegate, and executed on the given operands. However, to implement a cache we also need to understand the extent of the situations in which we can reuse this implementation in the future. Is this implementation applicable whenever both operands are typed as int, or must the operands have not just the type int but perhaps an exact values encountered by this expression? The conditions under which an implementation applies in the future are known as the **test**.

For example, in the case above, the test returned by the binder might say that this implementation applies whenever the types of both arguments are int. This can be thought of as an if-condition that wraps the implementation:

``` csharp
if (d1 is int && d2 is int) {
    return (int)d1 + (int)d2;
}
```

By providing this implementation and this test, the binder is saying that in any case where d1 and d2 both have the runtime type int, the correct implementation will always be to cast d1 and d2 to int and add the results. This combination of a test, plus an implementation that is applicable whenever that test is met is what forms a **rule**. These rules are represented in the DLR as compound expressions formed from the implementation expression and the test.

A rule may also be generated for a failed binding to optimize future calls where it’s guaranteed to fail again. For example, if the left operand was of type MyClass and this class does not have a user-defined + operator, the following rule may be returned:

``` csharp
if (d1 != null && d1.GetType() == typeof(MyClass)) {
throw new InvalidOperationException("Runtime binding failed");
}
```

This rule chooses to test type identity so that it’s only applicable when the type is *exactly* MyClass, as an instance of a derived class may derive its own + operator. If the language was such that it could be known that no derived class is allowed to define a + operator, or perhaps that this type is sealed, the binder could return a more efficient is-test. This is the type of decision the language binder makes as it is the arbiter of its language’s semantics.

Languages with highly dynamic objects may define more advanced tests as well. For example, if an object allows methods to be added or removed at runtime, it’s not correct to simply cache what to do when that method is invoked for some operand types, as the method may be different or missing on the next invocation. In this case, the object may choose to keep track of a version number, incrementing the version each time a method is added or removed from the object. The test would then check both that the parameter types match, and that the version number is still the same. In the case that the version number has changed, the test would fail, and this rule would no longer apply. The site would then call the binder to bind again.

<h2 id="binders-and-callsitebinder">4.3 Binders and CallSiteBinder</h2>

The component of a dynamic site that actually accepts an operation’s arguments at runtime and produces rules is known as a **binder**. A binder encapsulates the runtime semantics of the language that emitted the call site as well as any information the language needs at run time to bind the operation. Each dynamic call site the compiler emits has an instance of a binder. There are several kinds of binders for different kinds of operations, each deriving from **CallSiteBinder**. At runtime, this class will perform the binding the compiler is skipping during compile-time.

An instance of a binder should encode all statically available information that distinguishes this operation from a similar operation. For our addition example, the language may define a general OperationBinder class, and then specify when constructing an instance of OperationBinder that the site’s specific operation is addition.

It is up to the language to decide the specific static information it will need at runtime in addition to the list of arguments to bind this operation accurately. For a method invocation, say `d.Foo(bar, 123)`, the binder instance encodes the method name Foo, but may also choose to encode facts like the following if they are material to binding this invocation at runtime:

- the second parameter was passed a literal value

- named argument-passing was not used

- the entire expression was inside a checked-arithmetic block

It is entirely up to each language what compile-time information it chooses to encode in its binder for a given operation.

See the sites-binders-dynobj-interop.doc document for details on binders and API reference. See the section below on L2 Cache for high-level discussion of placing unique binder instances on multiple call sites when the binding metadata in the binders is equivalent.

<h2 id="callsitet-and-caching">4.4 CallSite&lt;T&gt; and Caching</h2>

When a compiler emits a dynamic call site, it must first generate a **CallSite&lt;T&gt;** object by calling the static method CallSite&lt;T&gt;.Create. The compiler passes in the language-specific binder it wants this call site to use to bind operations at runtime. The T in the CallSite&lt;T&gt; is the delegate type that provides the signature for the site’s target delegate that holds the compiled rule cache. T’s delegate type often just takes Object as the type of each of its arguments and its return value, as a call site may encounter or return various types. However, further optimization is possible in more restricted situations by using more specific types. For example, in the expression `a > 3`, a compiler can encode in the delegate signature that the right argument is fixed as int, and the return type is fixed as bool, for any possible value of a.

To allow for more advanced caching behavior at a given dynamic call site, as well as caching of rules across similar dynamic call sites, there are three distinct **caching levels** used, known as the L0, L1, and L2 caches. The code emitted at a dynamic site searches the L0 cache by invoking the site’s Target delegate, which will fall back to the L1 and L2 caches upon a cache miss. Only if all three caches miss will the site call the runtime binder to bind the operation.

<h3 id="l0-cache-callsites-target-delegate">4.4.1 L0 Cache: CallSite’s Target Delegate</h3>

The core of the caching system is the **Target** delegate on each dynamic site, also called the site’s **L0 cache**, which points to a compiled dynamic method. This method implements the dynamic site’s current caching strategy by baking some subset of the rules the dynamic site has seen into a single pre-compiled method. Each dynamic site also contains an **Update** delegate which is responsible for handling L0 cache misses by continuing on to search the L1 and L2 caches, eventually falling back to the runtime binder. The current method referenced by the Target delegate always contains a call to Update in case the rule does not match.

For example, an L0 Target method for a call site that has most recently adding ints might look like this:

``` csharp
if (d1 is int && d2 is int) {
    return (int)d1 + (int)d2;
}
return site.Update(site, d1, d2);
```

Note that the CLR’s JIT will compile this down to an extremely tight method (for example, the `is` tests should be just a few machine instructions, and the casts will disappear). This means that while it’s somewhat expensive to update the L0 cache for a new set of encountered types (as this requires compiling a new dynamic method), for L0 cache hits execution will come very close to the performance of static code.

<h3 id="l1-cache-callsites-rule-set">4.4.2 L1 Cache: CallSite’s Rule Set</h3>

As it takes time to generate the dynamic methods needed for the L0 cache, we don’t want to throw them away when a new set of types is encountered. For this reason, a call site will keep track of the 10 most recently seen dynamic methods that it has generated in its **L1 cache**. When there is an L0 cache miss, and the Update method is called, each dynamic method in the L1 cache is called in turn until a hit is found. When a hit is found, the L0 cache is updated to point to this method once again.

<h4 id="l2-cache-combined-rule-sets-of-all-equivalent-callsites">4.4.2.1 L2 Cache: Combined Rule Sets of All Equivalent CallSites</h4>

Each individual dynamic call site has a delegate type and a specific binder instance. The **L2 cache** is maintained across dynamic call sites with the same binder instance. The L2 cache allows the sitesto share rules via the binder instance.. The L2 cache helps eliminate startup costs for call sites that perform operations similar to those already performed elsewhere. Since the L2 cache currently holds 100 delegates, it also helps improve the performance of highly polymorphic sites that encounter many various types. Producers of binders must put canonical binder instances for a set of given metadata so that semantically equivalent call sties have the same binder instance on them.

Upon an L1 cache miss the binder would otherwise be required to generate a new dynamic method, either because this particular call site has not yet bound that operation, or because it has fallen off the end of this site’s L1 cache. The shared L2 cache solves both of these problems by providing a larger cache that retains the rules seen by all equivalent call sites. An L2 hit means that the rule can be added back to the site’s L1 cache and L0 target delegate without regenerating the dynamic method.

<h3 id="other-optimizations">4.4.3 Other Optimizations</h3>

The general architecture of the DLR's call sites and rules is open to several optimization techniques. For example, the rule expression generated for a given execution of a call site may follow a similar template as the last rule generated, differing only in some ConstantExpression nodes. The DLR can recognize rules that are similar in this way and combine them into one compiled rule that replaces each differing ConstantExpression with a ParameterExpression that expects the value as a parameter. This saves the cost of rebuilding and re-JIT’ing the Target delegate as the constant changes. This is one example of various techniques call sites allow us to employ in the DLR.

<h2 id="dynamicobject-and-expandoobject">4.5 DynamicObject and ExpandoObject</h2>

We make life much simpler for library authors who want to create objects in static languages so that the object can behave dynamically and participate in the interoperability and performance of dynamic call sites. Library authors can avoid the full power of DynamicMetaObjects, but they can also employ DynamicMetaObjects if they wish. The DLR provides two higher level abstractions over DynamicMetaObject: DynamicObject and ExpandoObject. For most APIs, these objects provide more than enough performance and flexibility to expose your functionality to dynamic hosts.

<h3 id="dynamicobject">4.5.1 DynamicObject</h3>

The simplest way to give your own class custom dynamic dispatch semantics is to derive from the **DynamicObject** base class. DynamicObject lets your objects fully participate in the dynamic object interoperability protocol, supporting the full set of operations available to objects that provide their own custom DynamicMetaObjects. DynamicObject lets you choose which operations to implement, and allows you to implement them much more easily than a language implementer who uses DynamicMetaObject directly.

DynamicObject provides a set of 12 virtual methods, each corresponding to a Bind... method defined on DynamicMetaObject. These methods represent the dynamic operations others may perform on your objects:

public abstract class DynamicObject : IDynamicMetaObjectProvider {

public virtual bool TryGetMember(GetMemberBinder binder,

out object result)

public virtual bool TrySetMember(SetMemberBinder binder,

object value)

public virtual bool TryDeleteMember(DeleteMemberBinder binder)

public virtual bool TryConvert(ConvertBinder binder,

out object result)

public virtual bool TryUnaryOperation

(UnaryOperationBinder binder, out object result)

public virtual bool TryBinaryOperation

(BinaryOperationBinder binder, object arg,

out object result)

public virtual bool TryInvoke

(InvokeBinder binder, object\[\] args, out object result)

public virtual bool TryInvokeMember

(InvokeMemberBinder binder, object\[\] args,

out object result)

public virtual bool TryCreateInstance

(CreateInstanceBinder binder, object\[\] args,

out object result)

public virtual bool TryGetIndex

(GetIndexBinder binder, object\[\] args, out object result)

public virtual bool TrySetIndex

(SetIndexBinder binder, object\[\] indexes, object value)

public virtual bool TryDeleteIndex

(DeleteIndexBinder binder, object\[\] indexes)

You could have your own TryGetMember implementation look up “Foo” in a dictionary, crawl through a dynamic model like XML, make a web request for a value, or some other custom operation. To do so, you would override the TryGetMember method and just implement whatever custom action you want to expose through member evaluation syntax. You return true from the method to indicate that your implementation has handled this situation, and supply the value you want returned as the out parameter, result.

By default, the methods that you don’t override on DynamicObject fall back to the language binder to do binding, offering no special behavior themselves. For example, let’s say you have a class, MyClass, derived from DynamicObject that does *not* override TryGetMember. You also have an instance of MyClass in a variable, myObject, of type C\# 'dynamic'. If you evaluate `myObject.Foo`, the evaluation falls back to C\#’s runtime binder, which will simply look for a field or property named Foo (using .NET reflection) defined as a member of MyClass. If there is none, the C\# binder will store a binding in the cache that will throw a runtime binder exception in this situation. A more real example is that you do override TryGetMember to look in your dictionary of dynamic members, and you return false if you have no such members. The DynamicMetaObject for MyClass produces a rule that first looks for static members on MyClass, then calls TryGetMember which may return false, and finally throws a language-specific exception when it finds no members.

In the full glory of the interoperability protocol, a dynamic object implements IDynamicMetaObjectProvider and returns a DynamicMetaObject to represent the dynamic view of the object at hand. The DynamicMetaObject looks a lot like DynamicObject, but its methods have to return Expression Trees that plug directly into the DLR's dynamic caching mechanisms. This gives you a great deal of power, and the ability to squeeze out some extra efficiency, while DynamicObject gives you nearly the same power in a form much simpler to consume. With DynamicObject, you simply override methods for the dynamic operations in which your dynamic object should participate. The DLR automatically creates a DynamicMetaObject for your DynamicObject. This DynamicMetaObject creates Expression Trees (for the DLR’s caching system) that simply call your overridden DynamicObject methods.

<h3 id="expandoobject">4.5.2 ExpandoObject</h3>

The **ExpandoObject** class is an efficient implementation of a dynamic property bag provided for you by the DLR. It allows you to dynamically retrieve and set its member values, adding new members per instance as needed at runtime. Because ExpandoObject implements the standard DLR interface IDynamicMetaObjectProvider, it is portable between DLR-aware languages. You can create an instance of an ExpandoObject in C\#, give its members specific values and functions, and pass it on to an IronPython function, which can then evaluate and invoke its members as if it was a standard Python object. ExpandoObject is a useful library class when you need a reliable, plain-vanilla dynamic object.

To allow easy enumeration of its values, ExpandoObject implements IDictionary&lt;String, Object&gt;. Casting an ExpandoObject to this interface will allow you to enumerate its Keys and Values collections as you could with a standard Dictionary&lt;String, Object&gt; object. This can be useful when your key value is specified in a string variable and thus you cannot specify the member name at compile-time.

ExpandoObject also implements INotifyPropertyChanging, raising a PropertyChanging event whenever a member is modified. This allows ExpandoObject to work well with WPF data-binding and other environments that need to know when the contents of the ExpandoObject change.

<h2 id="default-binder----runtime-utility-for-language-implementers">4.6 Default Binder -- Runtime Utility for Language Implementers</h2>

The DefaultBinder provides a simple way for languages that do not have deep specific .NET binding semantics to get started. It provides a number of hooks that allow the language to partially customize the Defaultbinder to their specific behavior. The default binder supports the following on static .NET objects:

- Conversions

- creating new instances

- getting/setting members (fields, properties, methods)

- invoking objects (delegates or other types that define a specially named call method)

- performing operations (addition, subtraction, etc.)

- binding to a selection of overloaded methods

Languages use the DefaultBinder by deriving from it. From there they can start overriding virtual methods to customize the behavior. The first function typically to override is GetMember. This method provides the DLR GetMemberBinder (code is slightly out of date here), which has the information the language needs such as the .NET type and member name. The language can use this information to return the member's value. The default binder uses the GetMember method for all of its member resolution. Thus, languages can

- manufacture members/methods that don't really exist on static .NET types (such as meta-programming hooks)

- implement extension methods

- filter out members from .NET types (for example, Python could hide String.Trim this way and manufacture the Python strip method)

The next functionality languages usually override is overload resolution. The default binder has two different mechanisms for overload resolution which we'll merg. Languages get overload resolution that works similar to C\#’s but that's also customized for their rules. For example, a language might want to allow passing bools where functions take integers. The language could indicate that bools are convertible to ints and provide conversion logic when the call happens. Much more is possible of course.

The overload resolution uses DynamicMetaObjects. The methods on DefaultBinder take DynamicMetaObjects and return one with the appropriate restrictions for invoking the chosen overload. For example, if there two overloads with two positional arguments, both typed to object, there is no need to do a type check on the argument at that position.

Finally the language implementer would probably customize the results of failures. Errors should be raised as they are expected in that language. The default binder provides a number of hooks for the language to produce the Expression Tree that embodies the semantics of what the language would do in a particular situation. For example, if a member doesn’t exist, Python wants to throw an exception, but JS wants to return it’s sentinel $Undefined value. Another example is when an error message is actually reported. Python would like to report the error in terms of Python type names (str, int, long, float, etc.), but other languages would want their own type names (or maybe the standard .NET names).
