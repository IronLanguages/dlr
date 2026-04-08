# 3 IDynamicMetaObjectProvider and DynamicMetaObject

With just the dynamic call sites and binders logic defined above, you can already imagine implementing a language which takes advantage of the DLR’s caching system. You could emit a dynamic call site for each dynamic operation and write a binder that produces rules based on the types of the operands provided. Your binder could understand dynamic dispatch to static .NET types (whose static structure can be gleaned from reflection), as well as dispatch to its own dynamic types, which it natively understands.

However, what if you want the binder for your language to dispatch to functions on objects created by another dynamic language? In this case, reflection will not help you as it would only show you the static implementation details behind that type’s dynamic façade. Even if the object were to offer a list of possible operations to perform, there are still two problems. You would have to rebuild your language every time some new language or object showed up with a new operation your language didn’t know about. You would also really like to perform these operations with the semantics of the target object’s language, not your language, to ensure that object models relying on quirks of that language’s semantics still work as designed.

Also, a library author might have a very dynamic problem domain they want to model. For example, if you have a library that supports drilling into XML data, you might prefer that your users’ code to look like Customers.Address.ZipCode rather than XElement.Element("address").Element ("zipcode"), with the library allowing dynamic access to its data. Another example would be a library doing something similar with JSON objects returned from web services, or other highly dynamic data sources.

What’s needed is a mechanism through which objects defined in various dynamic languages can offer to *bind their own operations* in the way that they see fit, while still obtaining the benefits of the DLR’s caching infrastructure. The most visible part of this mechanism is the IDynamicMetaObjectProvider interface, which classes implement to offer dynamic dispatch support to a consuming language. The DynamicMetaObject class complements IDynamicMetaObjectProvider and serves as the common representation of dynamic metadata, allowing operations to be dispatched on dynamic objects.

<h2 id="idynamicmetaobjectprovider">3.1 IDynamicMetaObjectProvider</h2>

An object that offers up either its source language’s semantics or its own type’s custom dispatch semantics during dynamic dispatch must implement **IDynamicMetaObjectProvider**. The IDynamicMetaObjectProvider interface has a single method, GetMetaObject, which returns a DynamicMetaObject that represents this specific object’s binding logic. A key strategy for DLR languages is to use .NET's Object as the root of their type hierarchy, and to use regular .NET objects when possible (such as IronPython using .NET strings). However, the IDynamicMetaObjectProvider protocol may still be needed for these base types at times, such as with IronRuby's mutable strings.

Library authors (even those using static languages) might implement IDynamicMetaObjectProvider so that their objects can present a dynamic façade in addition to their static interface. These objects can then present a better programming experience to dynamic languages as well as enable lighterweight syntax in languages such as C\# with the 'dynamic' keyword.

<h2 id="dynamicmetaobject">3.2 DynamicMetaObject</h2>

An instance of **DynamicMetaObject** represents the binding logic for a given object, as well as the result for a given expression that's been bound. It has methods that allow you to continue composing operations to bind more complex expressions.

The three major components of a DynamicMetaObject are:

- The value, the underlying object or the result of an expression if it has one, along with information on its type.

- An expression tree, which represents the result of binding thus far.

- A set of restrictions, which represent the requirements gathered along the way for when this expression tree can serve as a valid implementation.

The expression tree and set of restrictions should feel familiar as they are similar in purpose to the implementation and test within a rule. In fact, when a DynamicMetaObject-aware binder such as DynamicMetaObjectBinder is asked to bind a given DynamicMetaObject, it uses the expression tree as the implementation and transforms the restrictions into the test for the rule.

Restrictions should be mostly of a static or invariant nature. For example, they test whether an object is of a specific static type or is a specific instance, something that will not change due to side effects during evaluation of this expression. The expression tree produced by the DynamicMetaObject binding may supply other tests that vary on dynamic state. For example, for objects whose member list itself is mutable, the expression tree may contain a test of an exact version number of the type. If this dynamic version test fails, the rule may be outdated, and the call site needs to call the binder and update the cache.

DynamicMetaObjects may also be composed to support merging several operations or compound dynamic expressions into a single call site. For example, `a.b.c.d()` would otherwise compile to three or four distinct dynamic call sites, calling three or four Target delegates each time the operation is invoked. Instead, the compiler may generate a single call site that represents the compound operation. Then, for each pass through this call site at runtime, the binder can decide how much of this compound expression it can bind in advance, knowing just the type of the source, a. If a has a certain static type, the return type of `a.b` may then be known, as well as `a.b.c` and the invocation `a.b.c.d()`, even before the methods are executed. In this case, the binder can return a single rule whose implementation covers the entire invocation.

<h2 id="dynamicmetaobjectbinder">3.3 DynamicMetaObjectBinder</h2>

If a language wants to participate not just in the DLR’s caching system, but also interop fully with other dynamic languages, its binders need to derive from one of the subclasses of **DynamicMetaObjectBinder**.

DynamicMetaObjectBinder acts as a standard interoperability binder. Its subclasses represent various standard operations shared between most languages, such as GetMemberBinder and InvokeMemberBinder. These binders encode the standard static information expected across languages by these operations (such as the name of a method to invoke for InvokeMemberBinder). At runtime, DynamicMetaObject’s Bind method accepts a target DynamicMetaObject and an array of argument objects. This Bind method, however, does not perform the binding itself. Instead, it first generates a DynamicMetaObject for each operand or argument as follows:

- **For objects that implement IDynamicMetaObjectProvider:** The Bind method calls .GetMetaObject() to get a custom DynamicMetaObject.

- **For all other static .NET objects:** The DLR generates a simple DynamicMetaObject that falls back to the semantics of the language, as represented by this binder class.

This subclass of DynamicMetaObjectBinder then calls out to the relevant Bind… method on the target DynamicMetaObject (such as BindInvokeMember), passing it the argument DynamicMetaObjects. By convention all binders defer to the DynamicMetaObjects before imposing their language's binding logic on the operation. In many cases, the DynamicMetaObject is owned by the language, or if it is a default DLR .NET DynamicMetaObject, it falls back to the language binder for deciding how to bind to static .NET objects. Deferring to the DynamicMetaObject first is important for interoperability.

Binding using DynamicMetaObjectBinders takes place at a higher level by using DynamicMetaObjects instead of the .NET objects themselves. This lets the designer of dynamic argument objects or operands retain control of how their operations are dispatched. Each object’s own defined semantics always take highest precedence, regardless of the language in which the object is used, and what other objects are used alongside them. Also, because all languages derive from the same common binder classes, the L0 target methods that are generated can cache implementations across the various dynamic languages and libraries where objects are defined, enabling high performance with semantics that are true to the source.

<h3 id="fallback-methods-implementing-the-languages-semantics">3.3.1 Fallback Methods – Implementing the Language’s Semantics</h3>

A major principle in the DLR design is that “the object is king”. This is why DynamicMetaObjectBinder always delegates binding first to the target’s DynamicMetaObject, which can dispatch the operation with the semantics it desires, often those of the source language that object was defined in. This helps make interacting with dynamic object models feel as natural from other languages as it is from the language the object model was designed for.

However, there are often times when you’ll write code that uses a language feature available in your language, but not in the language of your target object. For example, let’s say you’re coding in Python, which provides an implicit member \_\_class\_\_ on all objects that returns the object’s type. When dynamically dispatching member accesses in Python, you’ll want the \_\_class\_\_ member to be available not just on Python objects, but also on standard .NET objects, as well as objects defined in other dynamic languages. This is where Fallback methods come in.

Each of the DynamicMetaObjectBinder subclasses defines a **fallback method** for the specific operation it represents. This fallback method implements the language’s own semantics for the given operation, to be used when the object is unable to bind such an operation itself. While a language’s fallback methods may supply whatever semantics the language chooses, the intention is that the language exposes a reasonable approximation of its compile-time semantics at runtime, applied to the actual runtime types of the objects encountered.

For example, the SetMemberBinder class defines an abstract FallbackSetMember method that languages override to implement their dynamic member assignment semantics. At a member assignment call site, if the target DynamicMetaObject’s BindSetMember method can’t dispatch the assignment itself, it can delegate back to the language’s SetMemberBinder. The DynamicMetaObject does this by calling the binder's FallbackSetMember method, passing the target DynamicMetaObject (itself) and the value to assign to the member.

There are specific subclasses of DynamicMetaObjectBinder for each of 12 common language features in the interoperability protocol. For each kind of operation for which you support binding in your language, you must implement a binder that derives from its class. The API Reference section below describes these operation binder abstract classes and provides further detail on implementing them.

<h3 id="error-suggestions">3.3.2 Error Suggestions</h3>

An MO that wants even finer control over the “fallback dance” may also choose to pass an **error suggestion** to a language’s fallback method. An error suggestion lets a dynamic object supply a binding recommendation to the language binder, which the language binder should use if it fails its own binding process.

Many complex binding behaviors may be constructed in this way, but a typical technique is to facilitate a “static-first” binding strategy. A dynamic object that wants to prefer static members over dynamic members of the same name will immediately call the language binder’s Fallback method when asked to bind an operation. The dynamic object will supply an error suggestion MO to the language binder that contains its logic on how to perform dynamic member dispatch. The language will then attempt its own binding to the object’s static members. Only if this binding fails, will the language resort to the object’s error suggestion, which will then attempt dynamic binding.

This strategy of letting the language bind first prevents a dynamic member from shadowing a known static member available on an object, and is used by the DynamicObject abstract class described later in this document.

<h2 id="dynamic-binding-walkthroughs">3.4 Dynamic Binding Walkthroughs</h2>

The following sections walk through some simple binding operations to help demonstrate the dynamic binding “dance” that occurs between the target object’s MetaObject and the source language’s DynamicMetaObjectBinder. These sections also demonstrate the caching mechanism that works with the binding process to accelerate a given operation the second time it’s encountered.

<h3 id="a-b">3.4.1 a + b</h3>

This walkthrough describes how the C\# Compiler will dispatch the operation a + b dynamically:

> c = a + b;

The C\# language specifies that operations are dispatched dynamically when at least one operand has the static C\# type, *dynamic*, introduced in C\# 4.0. In this case, we’ll assume that at least the left operand has the type *dynamic*.

The following is the equivalent C\# source code for what the C\# compiler will emit for the dynamic call site above:

if (SiteContainer.Site1 == null)

{

<span id="_Toc230529392" class="anchor"></span>SiteContainer.Site1 =

CallSite&lt;Func&lt;CallSite, object, object, object&gt;&gt;.Create(

CSharpBinaryOperationBinderFactory.Create(

ExpressionType.Add, false, false,

new CSharpArgumentInfo\[\] {

new CSharpArgumentInfo(0, null),

new CSharpArgumentInfo(0, null)

}

)

);

}

object c = SiteContainer.Site1.Target(SiteContainer.Site1, a, b);

The lifetime of the CallSite proceeds as follows:

1.  The first block ensures that a singleton CallSite object for this call site has been initialized:

    1.  First, we check that we haven’t already generated this CallSite object. To avoid generating the CallSite object twice, C\# stores it in a static field in a generated SiteContainer class. Each call site will get its own field.

    2.  We then call CallSite&lt;T&gt;.Create to generate the CallSite object for this site. In this case, the delegate type T is the type Func&lt;CallSite, object, object, object&gt;. This means that the Target delegates generated by the DLR to cache and implement this operation will expect to take two objects as operands (along with the CallSite itself) and then return an object as the result. Taking two values and returning one is what we’d expect for a binary operation such as +.

    3.  We use a factory method from the C\# Runtime to generate an instance of C\#’s specific BinaryOperationBinder subclass. The factory method will ensure that we get the canonical binder instance for this call site. The method’s first parameter specifies that this BinaryOperation is an Add operation, while the remaining parameters supply C\#-specific information, such as whether this operation occurs in a C\# checked-arithmetic context and whether either operand was a literal value. Languages are free to stash away in their binder classes this sort of compile-time information if they’ll need to bind accurately at runtime.

2.  Once our CallSite is initialized, the Target delegate is invoked for the first time to bind and perform the operation as follows:

<!-- -->

1.  The Target delegate (L0 cache) starts out as simply a copy of the Update delegate, which is called to bind the operation and then update the Target delegate. Invoking the Update delegate indicates an L0 cache miss, which is to be expected as this is the first time through this call site.

2.  Before binding, the L1 and L2 caches are checked to see if an applicable binding already exists for the operand types provided. As this is the first time we have hit this particular call site, there will be no match in the call-site-specific L1 cache. We’ll assume for this walkthrough that there was no match in the global L2 cache as well.

3.  To actually perform the binding, the DynamicMetaObjectBinder base class first produces a DynamicMetaObject (MO) for each operand using DynamicMetaObject.Create. The MO produced for each operand depends on that operand’s type:

- For dynamic objects (objects that implement IDynamicMetaObjectProvider), an MO is produced by calling the object’s GetMetaObject method. This allows any custom binding semantics the object defines to take precedence over C\# semantics by default, as the 12 standard interoperability binders that derive from DyanmicMetaObjectBinder will always defer to an object’s MO before the source language.

- For all other .NET objects, a default MO implementation is returned which will always “fall back” to the language binder (in this case, C\#’s binder) to bind operations.

1.  With our operands’ MOs in hand, DynamicMetaObject.BindBinaryOperation is called on the left operand’s MO, passing it the right MO as an argument. How this binding proceeds depends on whether the left MO defines its own semantics for +:

- If the left MO is from a Python object, for example, it will implement Python’s semantics for binding the + operator and produce an expression tree to that effect, based on the specific Python type of the left operand and the CLR or Python type of the right operand. This expression tree is returned within a new MO as the result of the operation, along with a new set of restrictions for when this tree is a valid binding. In this case, step e below is skipped.

- However, not all MOs implement all dynamic operations. If this MO is from a dynamic library whose objects do not define custom semantics for +, or if this is the default .NET MO produced for non-dynamic objects, the BindBinaryOperation method will not generate an MO itself. Instead, it will “fall back” to the language binder passed in to BindBinaryOperation by calling FallbackBinaryOperation on our instance of CSharpBinaryOperationBinder.

1.  If the left operand’s MO can’t bind + itself, the MO calls the C\# binder’s FallbackBinaryOperation method to perform the binding. This allows the C\# binder to provide its own binding semantics for +, based on the runtime types and values of each operand, along with any other compile-time information it had stashed away. The resulting expression tree that represents the binding will be returned in a new MO with a new restrictions set, just as in the Python case.

2.  The CallSiteBinder base now produces a delegate of type Func&lt;CallSite, object, object, object&gt; that implements the returned expression tree and the restrictions contained by the resulting MO. This delegate then replaces the Target delegate (L0 cache) for this call site, and is stored in the L1 and L2 caches to speed up future binding as well. Later, when this delegate is pulled from the cache, it will test these restrictions, executing the compiled expression if this binding applies to the operands provided, and calling the Update delegate for a new binding if it does not.

3.  Now that we’ve updated the Target delegate, its compiled expression is executed to perform the operation on the operands provided.

<!-- -->

1.  Later, this same call site may be hit again. The stored CallSite object is retrieved and its Target delegate is invoked to perform the operation and any necessary binding, based on the results of the delegate’s restriction test:

    -   If we see the same operand types as we did previously, it’s likely that the Target delegate’s restriction test will succeed and the operation will be performed. In this case, the only overhead vs. a statically-bound operation is the delegate invocation and a type-test for each operand, both of which are quite fast in CLR 4.0.

    -   If we see different types, the Target delegate’s restriction will fail and the Update delegate will be called again. In this case, if no match is found in the L1 or L2 caches, binding will proceed as it did before, with the binding result replacing the current contents of the Target delegate. This will leave the new binding in the L0 cache, with both the new and the previous bindings in the L1 and L2 caches.

<h3 id="a.foob">3.4.2 a.Foo(b)</h3>

The process of binding other operations such as SetMember and GetIndex proceeds in much the same way, with any minor differences listed in the API sections below. One operation worth calling out specifically, however, is InvokeMember.

Languages may implement member invocations such as a.Foo(b) in one of two ways:

- A GetMember operation followed by an Invoke operation, as in Python. This allows the intermediate first-class function object to be accessed and manipulated directly.

- An atomic InvokeMember operation that both resolves the member access and dispatches the invocation, as in C\# or Ruby.

To be compatible with all DLR-aware languages, DynamicMetaObject (MO) implementations that bind member invocations must support both Invoke and InvokeMember operations. For example, when C\# is binding an InvokeMember operation against a dynamic object with first-class function members (like Python), the fallback mechanism is a bit more involved to allow Python to perform the GetMember and C\# to perform the Invoke half. Giving control of Invoke back to C\# allows it to control the implicit conversions of arguments, as well as other C\#-specific binding behaviors.

Consider the C\# Compiler dispatching the following member invocation dynamically, where a is a Python object and b is a standard .NET object:

> c = a.Foo(b);

Binding proceeds mostly as described for a+b, with some important differences around fallback methods:

1.  A CallSite singleton for this CSharpInvokeMemberBinder is initialized as normal.

2.  The call site’s Target delegate is then invoked, calling into the Update delegate that will bind the operation after failing to find a binding in the L1 or L2 caches.

    1.  A Python MO is produced for target a, and a default .NET MO is produced for argument b.

    2.  DynamicMetaObject.BindInvokeMember is called on the target's Python MO, passing it the argument MO. Python will then dispatch the GetMember half of this operation itself by doing its standard GetMember lookup on the member name to produce an intermediate MO for the first-class function object represented by that member. This MO contains an Expression representing a callable object, but the MO may not have a concrete value stored in it yet.

    3.  Python’s BindInvokeMember implementation will then fall back to the C\# binder to invoke this function object by calling InvokeMemberBinder.FallbackInvoke, and passing it the intermediate function object’s MO. The C\# binder will bind this function call as a standard delegate invocation, applying C\#’s own argument conversion and error semantics.

    4.  A delegate is then produced as normal for the resulting MO, updating the call site’s Target delegate and executing the compiled expression to perform the operation.

3.  When this call site is hit again, the Target delegate is executed, checking the L1 and L2 cache and perhaps rebinding if necessary. If no dynamic member is found with the name “Foo”, or if the MO is owned by an InvokeMember-oriented language such as Ruby, the MO may simply call FallbackInvokeMember rather than splitting lookup and invocation by calling FallbackInvoke.
