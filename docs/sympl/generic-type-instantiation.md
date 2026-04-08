# 16 Generic Type Instantiation

Instantiating .NET generic types in Sympl may not be obvious. It is similar to IronPython, but IronPython provides a bit of syntactic sugar to make it easier. Here are a couple of examples in Sympl:

;;; Create List&lt;int&gt;

(set types (system.array.CreateInstance system.type 1))

(set (elt types 0) System.Int32)

(new (system.collections.generic.list\`1.MakeGenericType types)))

;;; Create a Dictionary&lt;string,int&gt;

(set types (system.array.CreateInstance system.type 2))

(set (elt types 0) system.string)

(set (elt types 1) system.int32)

(new (system.collections.generic.dictionary\`2.MakeGenericType

types)))

Sympl could do two things to make this better. The first is to provide nicer name mappings to hide the .NET true names of types, that is, hide the backquote-integer naming. Sympl's identifiers are so flexible, this wasn't necessary or worth demonstrating in the example. The second affordance would be some keyword form like the following:

(new (generic-type system.collections.generic.list system.int32))

IronPython supports indexing syntax on generic type names, and it also supports cleaner name mapping to avoid the backquote characters in its identifiers.

The above working examples in Sympl work because of the binding logic discussed several times regarding mapping TypeModel to RuntimeType. See ParametersMatchArguments and GetRuntimeTypeMoFromModel discussions in other sections.
