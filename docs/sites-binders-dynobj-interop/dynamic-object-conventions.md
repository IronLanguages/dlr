# 5 Dynamic Object Conventions

As a language implementer or library author, you may use the mechanisms described above to expose whatever arbitrary binding semantics you wish, including semantics that differ greatly from those of the language consuming your objects. The fallback system exposed by the DynamicMetaObjectBinder subclasses enables your objects to blend their semantics naturally with those of the language consuming them.

In addition to the fallback system, there are also some established conventions for dynamic interop that allow consuming languages to learn about the state of your objects in a standard way. By following these conventions, you can expose concepts such as enumerability or disposability in a way that can change at runtime.

<h2 id="enumerability">5.1 Enumerability</h2>

An object that represents a sequence of other objects is considered to be *enumerable*. Enumerable objects may then be enumerated over in a language such as C\# that supports “foreach” blocks, yielding each element in its sequence in turn.

In the static .NET world, objects are known to be enumerable if they implement the IEnumerable or IEnumerable&lt;T&gt; interface. This interface promises that your object implements a GetEnumerator method that returns an IEnumerator object, which can be used to move through your sequence. If you’re implementing an object library and know that a given class is always enumerable, the easiest way to specify enumerability is simply to implement IEnumerable&lt;T&gt; as normal.

However, you may be implementing library or language objects which are only sometimes enumerable, or which may become enumerable over time due to some dynamic operation on the object's type or itself. For example, in Python an object specifies that it is enumerable by defining an \_\_iter\_\_ method. Since this method may be added dynamically at runtime, the IronPython compiler cannot know whether to implement the IEnumerable interface on the objects it produces.

For these dynamic situations, an object may instead specify enumerability by binding a dynamic conversion to the IEnumerable type, succeeding if the object is indeed enumerable. Languages such as C\# will first attempt to convert dynamic objects being “foreached” to the IEnumerable interface using a Convert call site.

Note that if the IEnumerator object you return from GetEnumerator is itself disposable, this object should implement IDisposable statically.

<h2 id="disposability">5.2 Disposability</h2>

An object that has resources to release when the object is no longer required is considered to be *disposable*. Disposable objects offer a .Dispose method to call when the object is to be released, but more often the calling language will provide some sort of “using” block which will automatically call an object’s Dispose method when the block completes.

Similar to enumerability, objects may statically specify that they are disposable by implementing the IDisposable interface, and this is the most straightforward approach if given classes in your object library always require disposing.

If your objects can become disposable at runtime, you may encode this in a similar fashion to enumerability, by successfully binding dynamic conversions to IDisposable. Languages such as C\# using a dynamic object in a context that requires disposal will first attempt to dynamically convert the object to the IDisposable interface with a Convert call site.
