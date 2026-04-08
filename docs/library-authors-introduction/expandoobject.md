# 2 ExpandoObject

The **ExpandoObject** class is an efficient implementation of a dynamic property bag provided for you by the DLR. It allows you to dynamically retrieve and set its member values, adding new members per instance as needed at runtime, as typically expected of dynamic objects in languages such as Python and JScript. Instances of ExpandoObject support consumers in writing code that naturally accesses these dynamic members with dot notation (o.foo) as if they were static members, instead of something more heavyweight such as o.GetAttribute("foo").

For example, you could use instances of the ExpandoObject class to hold onto Name and Age values for the effect of dynamic or structural typing:

static void Main(string\[\] args) {

dynamic student, teacher;

student = new ExpandoObject();

student.Name = "Billy";

student.Age = 12;

teacher = new ExpandoObject();

teacher.Name = "Ms. Anderson";

teacher.Age = "thirty";

WritePerson(student);

WritePerson(teacher);

}

private static void WritePerson(dynamic person) {

Console.WriteLine("{0} is {1} years old.",

person.Name, person.Age);

}

This produces the following output:

``` csharp
Billy is 12 years old.
Ms. Anderson is thirty years old.
```

You can also store lambda expressions as delegate values to define first-class function members, which you can later invoke:

static void Main(string\[\] args) {

dynamic employee = new ExpandoObject();

employee.Name = "Mr. Smith";

employee.Age = 42;

employee.CelebrateBirthday

= (Action&lt;dynamic&gt;)(person =&gt; { person.Age++; });

WritePerson(employee);

// Note: ExpandoObject doesn't support InvokeMember with an

// implicit self parameter. You fetch a member and supply

// all arguments explicitly.

employee.CelebrateBirthday(employee);

}

private static void WritePerson(dynamic person) {

Console.WriteLine("{0} is {1} years old.",

person.Name, person.Age);

}

This produces the following output:

``` csharp
Mr. Smith is 42 years old.
Mr. Smith is 43 years old.
```

Because ExpandoObject implements the standard DLR interface IDynamicMetaObjectProvider, it is portable between DLR-aware languages. You can create an instance of an ExpandoObject in C\#, give its members specific values and functions, and pass it on to an IronPython function, which can then evaluate and invoke its members as if it was a standard Python object. ExpandoObject respects the case-sensitivity of the language binder interacting with it, binding to an existing member if there is exactly one case-insensitive match and throwing an AmbiguousMatchException if there are multiple.

To allow easy enumeration of its values, ExpandoObject implements IDictionary&lt;String, Object&gt;. Casting an ExpandoObject to this interface will allow you to enumerate its Keys and Values collections as you could with a standard Dictionary&lt;String, Object&gt; object. This can be useful when your key value is specified in a string variable and thus you cannot specify the member name at compile-time.

ExpandoObject also implements INotifyPropertyChanging, raising a PropertyChanging event whenever a member is modified. This allows ExpandoObject to work well with WPF data-binding and other environments that need to know when the contents of the ExpandoObject change.

ExpandoObject is a useful library class when you need a reliable, plain-vanilla dynamic object, as an end-user or even as a library author, but what if you want more power to define your own types with their own dynamic dispatch semantics? This is where DynamicObject comes in.
