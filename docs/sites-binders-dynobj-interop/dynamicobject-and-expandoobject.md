# 4 DynamicObject and ExpandoObject

We make life much simpler for library authors who want to create objects in static languages so that the object can behave dynamically and participate in the interoperability and performance of dynamic call sites. Library authors can avoid the full power of DynamicMetaObjects, but they can also employ DynamicMetaObjects if they wish. The DLR provides two higher level abstractions over DynamicMetaObject: DynamicObject and ExpandoObject. For most APIs, these objects provide more than enough performance and flexibility to expose your functionality to dynamic hosts.

<h2 id="dynamicobject">4.1 DynamicObject</h2>

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

<h2 id="expandoobject">4.2 ExpandoObject</h2>

The **ExpandoObject** class is an efficient implementation of a dynamic property bag provided for you by the DLR. It allows you to dynamically retrieve and set its member values, adding new members per instance as needed at runtime. Because ExpandoObject implements the standard DLR interface IDynamicMetaObjectProvider, it is portable between DLR-aware languages. You can create an instance of an ExpandoObject in C\#, give its members specific values and functions, and pass it on to an IronPython function, which can then evaluate and invoke its members as if it was a standard Python object. ExpandoObject is a useful library class when you need a reliable, plain-vanilla dynamic object.

<h2 id="further-reading">4.3 Further Reading</h2>

If you’d like to learn more about DynamicObject and ExpandoObject, check out the accompanying Getting Started with the DLR for Library Authors on the [DLR CodePlex site](http://www.codeplex.com/dlr) under Specs and Docs. That document covers DynamicObject and ExpandoObject at a deeper level and includes an example of using DynamicMetaObject to optimize a dynamic library.
