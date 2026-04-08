# 2 Dynamic Call Sites

Dynamic expressions are those expressions a language defines that may only be fully bound at runtime, usually based on the runtime types of its operands or parameters. However, for most languages, the binding of such operations takes a non-trivial amount of time. If such an operation occurs in a loop, or inside a commonly invoked function, we wish to avoid calling into the language’s runtime binder every time this code path executes.

To avoid repeatedly binding the same expressions, the DLR provides a caching mechanism, the **dynamic call site**. A call site encapsulates a language's binder and the dynamic expression or operation to perform. A language compiler for a regular .NET language, such as C\# or Visual Basic, emits a call site where dynamic expressions occur. Dynamic languages built completely on the DLR, such as IronPython, use DynamicExpression nodes from Expression Trees v2 (which the DLR ships in .NET 4.0, which the expression compiler resolves to dynamic call sites.

Each dynamic call site keeps track how to perform the operation it represents. It learns different ways to perform the operation depending on the data (operands) that flows into the site. The binder provides these implementations of the operation along with restrictions as to when the site can correctly use the implementations. We refer to these restrictions and implementations together as rules. The call site compiles the rules it sees into a target delegate that embodies the site' cache.

<h2 id="rules">2.1 Rules</h2>

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

<h2 id="callsitebinder">2.2 CallSiteBinder</h2>

The component of a dynamic site that actually accepts an operation’s arguments at runtime and produces rules is known as a **binder**. A binder encapsulates the runtime semantics of the language that emitted the call site as well as any information the language needs at run time to bind the operation. Each dynamic call site the compiler emits has an instance of a binder. There are several kinds of binders for different kinds of operations, each deriving from CallSiteBinder. At runtime, this class will perform the binding the compiler is skipping during compile-time.

To actually perform binding and produce a rule, the CallSiteBinder’s BindDelegate method is called. BindDelegate produces a rule delegate for the operation encoded by this binder by passing the parameters provided to the abstract Bind method to produce an expression tree representing the bound operation. This Bind method is implemented by the various subclasses of CallSiteBinder, including the primary set of DynamicMetaObject-based subclasses which allow for interop between objects of various languages.

An instance of a binder should encode all statically available information that distinguishes this operation from a similar operation. For our addition example, the language may define a general OperationBinder class, and then specify when constructing an instance of OperationBinder that the site’s specific operation is addition.

It is up to a language to decide the specific static information it will need at runtime in addition to the list of arguments to bind this operation accurately. For a method invocation, say `d.Foo(``bar``, 123)`, the binder instance encodes the method name Foo, but may also choose to encode facts like the following if they are material to binding this invocation at runtime:

- the second parameter was passed a literal value

- named argument-passing was not used

It is entirely up to each language what compile-time information it chooses to encode in its binder for a given operation. Languages should be sure to reuse canonical binder instances, however, taking care to avoid generating two binders of the same type with the same compile-time information. As described below in the L2 cache section, equivalent call sites that share a common binder instance may share rules and avoid rebinding operations already bound elsewhere in the program.

<h2 id="callsitet">2.3 CallSite&lt;T&gt;</h2>

When a compiler emits a dynamic call site, it must first generate a **CallSite&lt;T&gt;** object by calling the static method CallSite&lt;T&gt;.Create. The compiler passes in the language-specific binder it wants this call site to use to bind operations at runtime. The T in the CallSite&lt;T&gt; is the delegate type that provides the signature for the site’s target delegate that holds the compiled rule cache. T’s delegate type often just takes Object as the type of each of its arguments and its return value, as a call site may encounter or return various types. However, further optimization is possible in more restricted situations by using more specific types. For example, in the expression `a > 3`, a compiler can encode in the delegate signature that the right argument is fixed as int, and the return type is fixed as bool, for any possible value of a.

To allow for more advanced caching behavior at a given dynamic call site, as well as caching of rules across similar dynamic call sites, there are three distinct **caching levels** used, known as the L0, L1, and L2 caches. The code emitted at a dynamic site searches the L0 cache by invoking the site’s Target delegate, which will fall back to the L1 and L2 caches upon a cache miss. Only if all three caches miss will the site call the runtime binder to bind the operation.

<h3 id="l0-cache-callsites-target-delegate">2.3.1 L0 Cache: CallSite’s Target Delegate</h3>

The core of the caching system is the **Target** delegate on each dynamic site, also called the site’s **L0 cache**, which points to a compiled dynamic method. This method implements the dynamic site’s current caching strategy by baking some subset of the rules the dynamic site has seen into a single pre-compiled method. Each dynamic site also contains an **Update** delegate which is responsible for handling L0 cache misses by continuing on to search the L1 and L2 caches, eventually falling back to the runtime binder’s BindDelegate method to produce a new rule. The current method referenced by the Target delegate always contains a call to Update in case the rule does not match.

For example, an L0 Target method for a call site that has most recently adding ints might look like this:

``` csharp
if (d1 is int && d2 is int) {
    return (int)d1 + (int)d2;
}
return site.Update(site, d1, d2);
```

Note that the CLR’s JIT will compile this down to an extremely tight method (for example, the `is` tests should be just a few machine instructions, and the casts will disappear). This means that while it’s somewhat expensive to update the L0 cache for a new set of encountered types (as this requires compiling a new dynamic method), for L0 cache hits execution will come very close to the performance of static code.

<h3 id="l1-cache-callsites-rule-set">2.3.2 L1 Cache: CallSite’s Rule Set</h3>

As it takes time to generate the dynamic methods needed for the L0 cache, we don’t want to throw them away when a new set of types is encountered. For this reason, a call site will keep track of the 10 most recently seen dynamic methods that it has generated in its **L1 cache**. When there is an L0 cache miss, and the Update method is called, each dynamic method in the L1 cache is called in turn until there’s a cache hit. When a hit is found, the L0 cache is updated to point to this method once again. If not cache hit occurs, the call site proceeds to the L2 cache (see below).

<h3 id="l2-cache-combined-rule-sets-of-all-equivalent-callsites">2.3.3 L2 Cache: Combined Rule Sets of All Equivalent CallSites</h3>

Each dynamic call site has a delegate type and a binder instance. For each unique binder instance, a separate **L2 cache** is maintained that allows the sharing of rules between all call sites that reference this binder instance. The L2 cache helps eliminate startup costs for call sites that bind operations equivalent to those already bound elsewhere. Since the L2 cache may hold 100 delegates (compared to 10 in the L1 cache), it also provides an extra mechanism to improve the performance of highly polymorphic sites that encounter many various types.

Upon an L1 cache miss the binder would otherwise be required to generate a new dynamic method, either because this particular call site has not yet bound that operation, or because it has fallen off the end of this site’s L1 cache. The shared L2 cache solves both of these problems by providing a larger cache that retains the rules seen by all equivalent call sites. An L2 hit means that the rule can be added back to the site’s L1 cache and L0 target delegate without regenerating the dynamic method.

To facilitate this sharing of rules between call sites, language compilers and others who produce call sites must be sure to reuse a canonical binder instance across all “equivalent” call sites. It is up to a given compiler what set of criteria is used to determine call site equivalence, but the set must include all the site metadata that could influence rule production. For example, in C\#, an addition operation may appear in either a checked or unchecked arithmetic context, which determines overflow behavior. As this context affects the semantics of arithmetic operations bound in C\#, its compiler would only share a binder instance between “Add” call sites if they are all “checked” or all “unchecked”. This way, an Add rule generated in a checked context would not be shared in an unchecked context.

<h3 id="other-optimizations">2.3.4 Other Optimizations</h3>

The general architecture of the DLR's call sites and rules is open to several optimization techniques. For example, the rule expression generated for a given execution of a call site may follow a similar template as the last rule generated, differing only in some ConstantExpression nodes. The DLR can recognize rules that are similar in this way and combine them into one compiled rule that replaces each differing ConstantExpression with a ParameterExpression that expects the value as a parameter. This saves the cost of rebuilding and re-JIT’ing the Target delegate as the constant changes. This is one example of various techniques call sites allow us to employ in the DLR.
