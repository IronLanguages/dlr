# 3 DynamicObject

The simplest way to give your own class custom dynamic dispatch semantics is to derive from the **DynamicObject** base class. While ExpandoObject only dynamically adds and removes members, DynamicObject lets your objects fully participate in the dynamic object interoperability protocol. There are several abstract operations users of your object can then request of it dynamically, such as getting a member, setting a member, invoking a member on the object, indexing the object, invoking the object itself, or performing standard operations such as addition, multiplication, etc. DynamicObject lets you choose which operations to implement, and lets you do it much more easily than a language implementer.

DynamicObject provides a set of 12 virtual methods, each representing a possible dynamic operation on your objects:

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

We refer to each place in your code where a dynamic operation occurs as a dynamic call site. Each dynamic call site contains a cache of how to perform a given operation at that location in the code, given the particular types of objects that have been seen previously at that site. Each site has attached to it a runtime binder from the language in which the code is written. The binder encodes the static information about the operation at this call site. The binder then uses this static information and the arguments at runtime to figure out how to perform the specified operation for the given argument types. By convention, however, language binders defer binding to dynamic objects first, before doing the binding on their own. This follows the DLR principle that “the object is king”, and allows objects from dynamic languages to bind under the semantics of the language that defined them.

You could have your own TryGetMember implementation look up “Foo” in a dictionary, crawl through a dynamic model like XML, make a web request for a value, or some other custom operation. To do so, you would override the TryGetMember method and just implement whatever custom action you want to expose through member evaluation syntax. You return true from the method to indicate that your implementation has handled this situation, and supply the value you want returned as the out parameter, result.

In the full glory of the interoperability protocol, a dynamic object implements IDynamicMetaObjectProvider and returns a DynamicMetaObject to represent the dynamic view of the object at hand. The DynamicMetaObject looks a lot like DynamicObject, but its methods have to return Expression Trees that plug directly into the DLR's dynamic caching mechanisms. This gives you a great deal of power, and the ability to squeeze out some extra efficiency, while DynamicObject gives you nearly the same power in a form much simpler to consume. With DynamicObject, you simply override methods for the dynamic operations in which your dynamic object should participate. The DLR automatically creates a DynamicMetaObject for your DynamicObject. This DynamicMetaObject creates Expression Trees (for the DLR’s caching system) that simply call your overridden DynamicObject methods.

<h2 id="dynamicbag-implementing-our-own-expandoobject">3.1 DynamicBag: Implementing Our Own ExpandoObject</h2>

As a simple example of using DynamicObject, let’s implement DynamicBag, our own version of ExpandoObject. DynamicBag derives from DynamicObject and overrides the TryGetMember and TrySetMember methods:

public class DynamicBag : DynamicObject {

Dictionary&lt;string, object&gt; items

= new Dictionary&lt;string, object&gt;();

public override bool TryGetMember(

GetMemberBinder binder, out object result) {

return items.TryGetValue(binder.Name, out result);

}

public override bool TrySetMember(

SetMemberBinder binder, object value) {

items\[binder.Name\] = value;

return true;

}

}

We first set up a Dictionary that maps string values to object values, and then implement TryGetMember and TrySetMember to manipulate entries in this Dictionary. The first parameter of each override is the DLR call site binder provided by the dynamic call site that began this operation. In this case, the binder has stashed at compile time the name of the member being retrieved or being set.

Further parameters serve two purposes. Some are the runtime arguments to the operation, such as the value parameter on TrySetMember. Some are the out parameters through which the operation’s value is returned, such as the result parameter on TryGetMember. In the case of TryGetMember, we call the TryGetValue method on the items Dictionary and return its false return value if the item can’t be found. By returning false instead of just throwing an exception, we’re telling the DLR to fall back to the specific error semantics of the calling language (which might be returning a sentinel value like JS's $Undefined).

Because the DynamicObject base class implements IDynamicMetaObjectProvider, this class is now ready to act as an Expando object in C\#, Visual Basic, Python, Ruby or any other language that can dynamically consume DLR objects:

static void Main(string\[\] args) {

dynamic student, teacher;

student = new DynamicBag();

student.Name = "Billy";

student.Age = 12;

teacher = new DynamicBag();

teacher.Name = "Ms. Anderson";

teacher.Age = "thirty";

WritePerson(student);

WritePerson(teacher);

}

private static void WritePerson(dynamic person) {

Console.WriteLine("{0} is {1} years old.",

person.Name, person.Age);

}

<h2 id="namedbag-optimizing-dynamicobject-with-statically-defined-members">3.2 NamedBag: Optimizing DynamicObject with Statically Defined Members</h2>

We can do some simple optimizations to tune our object. Let’s say we want to allow dynamic members, but we know the object will always have a Name member. We can simply include these statically defined members on our object. The DLR calls on the language to look for these names first before calling TryGetMember:

public class NamedBag : DynamicObject {

Dictionary&lt;string, object&gt; items

= new Dictionary&lt;string, object&gt;();

public string Name;

public string LowercaseName {

get { return Name.ToLower(); }

}

public override bool TryGetMember(GetMemberBinder binder,

out object result) {

// Don't need to test for "Name" or "LowercaseName".

// Will never get called with those names.

result = items\[binder.Name\];

return true;

}

public override bool TrySetMember(SetMemberBinder binder,

object value) {

//if (binder.Name == "LowercaseName") return false;

items\[binder.Name\] = value;

return true;

}

}

This NamedBag now stores the value of its Name member in a real string field, making access to this field faster. We’ve also added a property LowercaseName which gives us the name converted to lowercase. The DynamicObject's DynamicMetaObject first calls on the binder to get a language-specific expression tree that captures any access to statically defined members. The meta object then causes that expression to be wrapped so that if the static binding fails, the expression then calls TryGetMember or TrySetMember. Finally if that fails too, the meta object's expression includes a language-specific error (or sentinel value like JS's $Undefined).

In TrySetMember you can decide whether to protect against dynamically setting the read-only property. A language may not roll over to the try to set the name dynamically when it sees the read-only property. However, by convention the language may still allow TrySetMember to execute. If it set the name to a value in the dictionary, there's no harm done, but the programmer will never be able to retrieve that value. The statically defined LowercaseName property will always dominate the binding for reading the value. Testing for the name and returning false would cause the language to throw an exception about setting a read-only property.

You’ve seen how you can easily start handling dynamic operations by deriving from DynamicObject and then implementing just the operations you care about. Most often, this should be flexible enough and fast enough for your needs as a library author, although it’s still not as optimized as it could be. For example, lookups that we don’t fast-path to fields still require a hashtable lookup, which is not nearly as efficient. If we know there are a few member names we’ll be accessing most often, but don’t know their names at compile time, we may wish to optimize our bag for that case.

In the full glory of the interoperability protocol, a dynamic object implements IDynamicMetaObjectProvider and returns a DynamicMetaObject to represent the dynamic view of the object at hand. The DynamicMetaObject looks a lot like DynamicObject, but its methods have to return Expression Trees that plug directly into the DLR's dynamic caching mechanisms. This gives you a great deal of power, and the ability to squeeze out some extra efficiency, while DynamicObject gives you nearly the same power in a form much simpler to consume. With DynamicObject, you simply override methods for the dynamic operations in which your dynamic object should participate, and the DLR automatically creates a DynamicMetaObject for your DynamicObject. This DynamicMetaObject creates Expression Trees (for the DLR’s caching system) that simply call your overridden DynamicObject methods.

To take advantage of the DLR caching system and get the flexibility to tweak our performance, we’ll look at how to get closer to the metal of the DLR and directly implement IDynamicMetaObjectProvider (and DynamicMetaObject) ourselves.
