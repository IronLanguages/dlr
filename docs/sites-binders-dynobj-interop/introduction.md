# 1 Introduction

The Dynamic Language Runtime's (DLR) mission is to enable an ecosystem of dynamic languages on .NET and to support developing libraries that work well with dynamic language features. Dynamic languages have become very popular in the last several years, and static languages are adopting affordances for dynamic operations on objects. The DLR helps language implementers move languages to .NET and enhance existing languages with dynamic features. Library authors can engage easily in this space too.

The history of dynamic dispatch on Microsoft platforms stretches back to the advent of COM and the IDispatch interface. A COM object which chose to implement IDispatch could either have its methods and properties invoked statically by binding calls to its vtable entries, or dynamically by retrieving a list of available names and IDs, and then invoking the desired ID. Visual Basic and VBScript then provided a robust abstraction over this capability by letting you write direct method calls that would bind statically (early-binding) if the underlying object had a specific type or dynamically (late-binding) if its type was Object.

While Visual Basic retained this ability during the migration to the .NET Framework, late-binding on .NET has been limited to COM libraries that implement IDispatch and reflection over the static shape of .NET types. It’s become necessary to provide a new mechanism for objects to specify their desired runtime binding logic that’s shared by all .NET dynamic languages and which maintains as much of the performance of static binding as possible.

A few years ago, implementations of fully dynamic languages such as IronPython began targeting the .NET Framework, including the CLR as it presently exists. However, these languages each had to provide their own systems to implement dynamic calls, and needed to implement their own optimizations for common operations. Also, while these languages were designed to provide a great experience against their own objects as well as static .NET types, one dynamic language could not consume objects defined in another.

With the dynamic sites concept introduced by the DLR, we’ve solved both the performance and language interop problems with dynamic language implementations on the .NET Framework.

<h2 id="performance">1.1 Performance</h2>

A major motivation behind the DLR has been to optimize the performance of dynamic operations without hard-coding specific fast-paths in advance.

For example, in the original implementation of IronPython before the DLR, there were vast quantities of code in associated helper classes that looked like this:

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

<h2 id="language-interop">1.2 Language Interop</h2>

Assuming you were willing to do such optimizations yourself, it would be possible to create a high-performing .NET dynamic language today without the DLR, as the DLR is simply an API that sits on top of the CLR and does not require any new built-in CLR features. This is, in fact, what the first versions of IronPython did, directly compiling Python code to IL that called run-time helper methods defined by IronPython. As the IronPython compiler knew about the semantics of standard .NET types, it could also provide a great experience targeting the BCL and existing C\#/VB assemblies as well.

However, this story breaks down when you want an IronPython object to call into, for example, an IronRuby library and dynamically invoke functions on the objects it gets back. With no standard mechanism to understand how to perform dynamic operations on an external dynamic object, IronPython could only treat the object as a .NET object, accessing its statically-known API. Most likely any visible methods will be an implementation detail of IronPython rather than a representation of the user’s intent.

The DLR provides for this common currency when calling into objects defined in other dynamic languages by establishing a protocol for those implementing dynamic objects. As a language creator or library author who wants other DLR-aware languages to consume your objects dynamically, you must have the objects implement IDynamicMetaObjectProvider, offering up appropriate implementations of DynamicMetaObject when IDynamicMetaObjectProvider.GetMetaObject() is called.
