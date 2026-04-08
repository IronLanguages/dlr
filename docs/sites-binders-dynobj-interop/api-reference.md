# 6 API Reference

The following sections provide a detailed API reference for the types introduced in the conceptual overview above, with definitions for each type’s most important members.

<h2 id="dynamicmetaobject-class">6.1 DynamicMetaObject Class</h2>

An instance of **DynamicMetaObject** (abbreviated MO) represents the binding logic for a given object. Binding an operation on an MO returns another MO with an Expression property representing the accumulated binding up to this point. The methods defined on MO allow you to continue composing these operations to bind more complex expressions.

<h3 id="class-summary">6.1.1 Class Summary</h3>

public class DynamicMetaObject {

public static readonly DynamicMetaObject\[\] EmptyMetaObjects;

public DynamicMetaObject

(Expression expression, BindingRestrictions restrictions,

Object value);

public DynamicMetaObject

(Expression expression, BindingRestrictions restrictions);

public Expression Expression { get; }

public Boolean HasValue { get; }

public Type LimitType { get; }

public BindingRestrictions Restrictions { get; }

public Type RuntimeType { get; }

public Object Value { get; }

public virtual DynamicMetaObject BindBinaryOperation

(BinaryOperationBinder binder, DynamicMetaObject arg);

public virtual DynamicMetaObject BindConvert(ConvertBinder binder);

public virtual DynamicMetaObject BindCreateInstance

(CreateInstanceBinder binder, DynamicMetaObject\[\] args);

public virtual DynamicMetaObject BindDeleteIndex

(DeleteIndexBinder binder, DynamicMetaObject\[\] indexes);

public virtual DynamicMetaObject BindDeleteMember

(DeleteMemberBinder binder);

public virtual DynamicMetaObject BindGetIndex

(GetIndexBinder binder, DynamicMetaObject\[\] indexes);

public virtual DynamicMetaObject BindGetMember

(GetMemberBinder binder);

public virtual DynamicMetaObject BindInvoke

(InvokeBinder binder, DynamicMetaObject\[\] args);

public virtual DynamicMetaObject BindInvokeMember

(InvokeMemberBinder binder, DynamicMetaObject\[\] args);

public virtual DynamicMetaObject BindSetIndex

(SetIndexBinder binder, DynamicMetaObject\[\] indexes,

DynamicMetaObject value);

public virtual DynamicMetaObject BindSetMember

(SetMemberBinder binder, DynamicMetaObject value);

public virtual DynamicMetaObject BindUnaryOperation

(UnaryOperationBinder binder);

public static DynamicMetaObject Create

(Object value, Expression expression);

public virtual IEnumerable&lt;String&gt; GetDynamicMemberNames();

<h3 id="bindoperation-methods">6.1.2 Bind*\*Operation\** Methods</h3>

There is a Bind*\*Operation\** method defined for each of the dynamic operation types that may be dispatched on a DynamicMetaObject (MO). Each Bind method accepts a target binder representing the call site at which the operation is performed. It also takes any operands required to bind the operation, such as values, arguments, or indexes. Each Bind method returns a DynamicMetaObject containing the expression that represents the result of binding the operation and the restrictions under which the binding applies.

Note that all Bind methods besides the BindConvert method should return a DynamicMetaObject instance with a specified LimitType of Object. To produce a specific result type, compilers should emit an explicit ConvertBinder call site that wraps the resulting MO.

<h3 id="expression-property">6.1.3 Expression Property</h3>

Returns an expression that represents the result of binding operations on a previous MO. An MO will always have an expression, even if it is a ParameterExpression to access some value in a program, since you can only create DynamicMetaObjects by passing an expression.

<h3 id="restrictions-property">6.1.4 Restrictions Property</h3>

Returns a BindingRestrictions object, which represents the constraints gathered during the binding process. The constraints capture when this MO’s Expression property can serve as a valid implementation of the bound operations.

<h3 id="value-and-hasvalue-properties">6.1.5 Value and HasValue Properties</h3>

The Value property returns the concrete object represented by the DynamicMetaObject. The HasValue property returns a Boolean value as to whether the Value property is valid; this lets you disambiguate null for HasValue == False vs. a real null value represented by the MO.

The great majority of DynamicMetaObjects will have a value, either because they were directly produced from a dynamic object by calling its GetMetaObject method, or because they are the intermediate result of a dynamic operation where partial binding has produced a value. There are certain cases, however, where a DynamicMetaObject is produced without an underlying value.

An example is the intermediate result of a compound operation such as InvokeMember. A DynamicMetaObject that prefers to bind InvokeMember operations as a GetMember followed by a separate Invoke will likely bind the GetMember half of an InvokeMember operation and produce a new MO representing this intermediate result. The the original MO would use FallbackInvoke to have the language binder bind the Invoke operation on the result. In this case, you should not provide a value to the new intermediate result MetaObject produced by the GetMember operation, as this could allow the source language’s binder to perform its own Invoke binding against the static .NET type used for the intermediate object. Instead, omit the value.

NOTE, binders must be careful to detect DynamicMetaObjects passed with HasValue== False and call the binder's Defer method to produce a nested CallSite. The nested CallSite in the case above would have an InvokeBinder, and the nesting allows the arguments to all get evaluated before flowing into the new CallSite. Then the InvokeBinder will see all MOs with actual values so that it can perform a proper binding. Without the Defer call, the binder and original DynamicMetaObject described in the previous paragraph will infinitely loop in the original CallSite. There is a concrete example of this explained in the sympl.doc file on [www.codeplex.com/dlr](http://www.codeplex.com/dlr) (see the "Documents and Specs" link on the home page).

<h3 id="limittype-property">6.1.6 LimitType Property</h3>

Returns the most specific type this DynamicMetaObject is known to be. This will be the actual runtime type of the value, if available, or else the static type of the bound expression (its Expression.Type value).

<h3 id="create-static-method">6.1.7 Create Static Method</h3>

Returns a new DynamicMetaObject instance that allows you to bind further operations against a given object.

This method requires the actual object in question along with an Expression object that can represent this object in the newly bound tree. For example, when using this factory to create a DynamicMetaObject for a method call argument, you might pass the actual argument value being supplied, along with a ParameterExpression for a local in the calling context to represent the argument in any resulting bindings. Without such an expression, binding expressions would have to bake in the actual value, making the binding rule very specific and not very re-usable in the CallSite's caching.

<h3 id="getdynamicmembernames-method">6.1.8 GetDynamicMemberNames Method</h3>

Returns a list of member names that this DynamicMetaObject asserts could be accessed dynamically on this MO’s value or bound expression. This list is useful while debugging to allow an IDE to populate the list of members available on an object within debugger tool windows, or to allow an interactive prompt to display the list of members on an object.

<h2 id="idynamicmetaobjectprovider-interface">6.2 IDynamicMetaObjectProvider Interface</h2>

This interface marks an object as a dynamic object that participates in the DLR's dynamic object interoperability protocol. A type which implements this interface is advertising the ability to offer up its own custom dynamic dispatch semantics in addition to a source language’s standard .NET binding. The IDynamicMetaObjectProvider interface has a single method, GetMetaObject, which returns a DynamicMetaObject representing the binding logic for either this specific object, the object's type, or its defining language.

For example, an object representing an XML node may implement IDynamicMetaObjectProvider to allow dynamic member access to its descendant nodes, while the type used to represent IronPython objects would implement the interface to expose Python’s dispatch semantics to whichever language is ultimately consuming the object.

<h3 id="class-summary-1">6.2.1 Class Summary</h3>

public interface IDynamicMetaObjectProvider {

DynamicMetaObject GetMetaObject(Expression parameter);

<h3 id="getmetaobject-method">6.2.2 GetMetaObject Method</h3>

Returns a DynamicMetaObject that supports binding dynamic operations on the current object. Objects that implement IDynamicMetaObjectProvider will define their own subclass of DynamicMetaObject that implements the desired dynamic binding semantics for the object and return an instance of this subclass when GetMetaObject is called.

<h2 id="callsitebinder-abstract-class">6.3 CallSiteBinder Abstract Class</h2>

CallSiteBinder is an abstract class from which the binder classes for each of a language’s specific operations are derived.

The code emitted by a language compiler will create an instance of a CallSiteBinder subclass for each dynamic call site. When this call site is reached, its Bind method will be called to produce an expression tree that represents the bound action required to execute this operation for the particular arguments specified.

Most language implementers will not directly derive from CallSiteBinder, but rather from DynamicMetaObjectBinder, which allows the use of DynamicMetaObject instances as a common currency during binding. The scenarios for directly implementing CallSiteBinder would be for binding and caching highly language-specific concepts, such as IronPython's global variable references or imports statements, which will not need to participate in interop, but would benefit from call site caching.

<h3 id="class-summary-2">6.3.1 Class Summary</h3>

public abstract class CallSiteBinder {

protected CallSiteBinder();

public static LabelTarget UpdateLabel { get; }

public abstract Expression Bind

(Object\[\] args,

ReadOnlyCollection&lt;ParameterExpression&gt; parameters,

LabelTarget returnLabel);

public virtual T BindDelegate&lt;T&gt;

(CallSite&lt;T&gt; site,

Object\[\] args);

protected void CacheTarget&lt;T&gt;(T target);

<h3 id="bind-method">6.3.2 Bind Method</h3>

Returns the bound expression produced by binding the operation represented by this call site for the arguments provided. BindDelegate calls the Bind method when a binding for a given operation is not present in the cache.

The args parameter contains the argument values passed in to this instance of the binder, while the parameters parameter contains a ParameterExpression instance for each parameter. These ParameterExpression instances may be used when referring to the parameter variables themselves in the resulting expression tree.

The returnLabel parameter provides a LabelTarget instance representing the point in the caller to continue from when this operation returns. This label should be passed to Expression.Return to generate the GotoExpression used within the bound expression to return its value.

<h3 id="binddelegate-method">6.3.3 BindDelegate Method</h3>

If you need more fine-grained control over the process by which bound expressions are transformed into delegates for execution, you may optionally override the BindDelegate method. Typically, it is only necessary to override the Bind method.

This method is called when there is a cache miss, and may decide for itself whether to call Bind to generate expression trees, as well as how and whether to call CacheTarget to add a new delegate to the cache.

For example, this enables you to implement a hardcoded fast-path for operations you know will be common so that you won’t need to bind their expression trees and generate delegates at runtime. You may also use this to implement your own custom caching strategy if you wish to optimize for particular patterns of use you know your objects will often encounter.

<h3 id="cachetarget-method">6.3.4 CacheTarget Method</h3>

This method caches a given delegate representing a rule and must be called by overridden implementations of BindDelegate to add rules to the cache.

<h3 id="updatelabel-static-property">6.3.5 UpdateLabel Static Property</h3>

Returns the LabelTarget to use in a GotoExpression from within an expression representing the binding result. A cached expression may perform a Goto to this label if the rule must now fail the binding process and request that the CallSite bind the expression again.

This is often used within a bound expression in the 'else' branch of a version-check condition. If the expression’s version check indicates the object has changed shape since binding last occurred (for example, adding or removing members), the cached binding may no longer apply, and so the binding process must occur again.

<h2 id="dynamicmetaobjectbinder-abstract-class">6.4 DynamicMetaObjectBinder Abstract Class</h2>

If a language wants to participate not just in the DLR’s caching system, but also interop fully with other dynamic languages, its binders need to derive from DynamicMetaObjectBinder. DynamicMetaObjectBinder extends CallSiteBinder to use DynamicMetaObjects as a common currency, allowing the exchange of dynamic objects between languages.

The 12 predefined subclasses of DynamicMetaObjectBinder represent various standard operations shared between most languages, such as GetMemberBinder and BinaryOperationBinder. By implementing its operations using these standard binder classes, a language may consume DynamicMetaObjects provided by other languages.

For example, when IronPython binds a GetMember operation on an IronRuby object, its binder's base gets the Ruby object’s DynamicMetaObject and calls BindGetMember on it. By passing an instance of IronPython’s GetMemberBinder implementation to Ruby’s BindGetMember method, the Ruby object may handle requests for Python-specific members it doesn’t understand, such as \_\_class\_\_, by falling back to the IronPython binder.

For other operations outside the scope of these predefined subclasses, a language may define its own binder types that derive directly from DynamicMetaObjectBinder. A language which requires richer dispatch semantics for a standard operation may also choose to define its own direct DynamicMetaObjectBinder implementation for such operations, with this special binder reverting to an implementation of the standard subclass only when explicitly performing interop with objects from another language.

<h3 id="fallback-methods">6.4.1 Fallback Methods</h3>

Each abstract subclass of DynamicMetaObjectBinder defines one or more abstract Fallback methods to be implemented by the language implementer. Each subclass will at least define a method named Fallback*\*Operation\** where *\*Operation\** is the operation represented by that subclass. These methods are called when a DynamicMetaObject is unable to bind the operation represented by the binder instance and wishes to “fall back” to the language to attempt its own binding. Binders should always call the Fallback*\*Operation\** method in case of binding errors as the source language may allow a given operation that the object itself does not, or if not, may have language-specific error semantics that should be observed, such as returning a sentinel value.

Each Fallback method accepts the following parameters:

- The target object of the operation

- Any relevant operands to the operation such as values, arguments, or indexes

- An optional errorSuggestion DynamicMetaObject representing the object’s suggestion for how to bind this operation if the language is also unable to produce a valid binding. This is usually null, or an expression for looking up a member dynamically should the binder fail to find a static member with the name.

Some subclasses also allow the object to “fall back” to a different operation than is represented by this binder. These cases are described in the specific sections below.

Also, be sure to check the HasValue property of each of the DynamicMetaObjects passed to your Fallback methods. Ensure that each parameter has a valid value before proceeding with binding. If not, you must use the binder’s Defer method to emit a nested dynamic call site that will obtain values for these parameters at runtime before proceeding with the rest of the Fallback implementation.

<h3 id="placing-canonical-binders-on-callsites">6.4.2 Placing Canonical Binders on CallSites</h3>

To support proper L2 caching and sharing of rules between dynamic call sites, each site must share its binder instance with any other site representing an equivalent operation, as defined by the source language.

For example, C\# supports two arithmetic contexts, checked and unchecked, which determine the semantics during integer overflow. As this distinction affects binding, C\# should never unify binder instances for call sites which differ on the checked context.

A compiler may proactively enforce canonicalization by limiting the instantiation of its binder instances to factory methods which can ensure that duplicate binder instances are not produced for equivalent sites.

<h3 id="com-support">6.4.3 COM Support</h3>

This support has been moved to Codeplex only, in the Microsoft.Dynamic.dll. It is the COM binding C\# uses, but it uses it privately now.

To support consuming COM objects dynamically, your language’s binder classes must explicitly attempt COM binding, as seen below for GetMember operations:

DynamicMetaObject com;

if (System.Dynamic.ComBinder.TryBindGetMember

(this, target, out com)) {

return com;

}

For more information on the members of ComBinder, you may download the full DLR sources, including the COM binder at the [DLR CodePlex site](http://www.codeplex.com/dlr).

<h3 id="class-summary-3">6.4.4 Class Summary</h3>

public abstract class DynamicMetaObjectBinder : CallSiteBinder {

protected DynamicMetaObjectBinder();

public virtual Type ReturnType { get; }

public abstract DynamicMetaObject Bind

(DynamicMetaObject target, DynamicMetaObject\[\] args);

public DynamicMetaObject Defer(params DynamicMetaObject\[\] args);

public DynamicMetaObject Defer

(DynamicMetaObject target, params DynamicMetaObject\[\] args);

public Expression GetUpdateExpression(Type type);

<h3 id="bind-method-1">6.4.5 Bind Method</h3>

Returns a DynamicMetaObject that represents the result of binding this operation on the target DynamicMetaObject with the set of arguments provided. Each of the 12 standard interoperability binders defined by the DLR overrides this method with a sealed implementation that calls on the target MO.

<h3 id="defer-method">6.4.6 Defer Method</h3>

Returns a DynamicMetaObject containing a DynamicExpression that defers a branch of the binding process until execution, once the value of the target and all arguments are known. This DynamicExpression will ultimately resolve to a nested CallSite. This allows the DyanmicMetaObject operand that has no value to get evaluated before flowing into the nested site. The nested site sees a brand new MO representing the value produced in the outer CallSite argument execution.

Note that a binder may defer either to itself or to another binder. For example, one common use of Defer is within a language’s InvokeMemberBinder to implement FallbackInvokeMember. When its Bind call receives an IDO as the target object, FallbackInvokeMember may choose to defer binding to an instance of InvokeBinder which can bind later when the type of the retrieved member is known.

<h3 id="returntype-property">6.4.7 ReturnType Property</h3>

Returns a Type object representing the return type of the bound expressions this binder class will produce. The general principle for the binder base classes in the DLR is that binders produce bound expressions with either Void or Object values, except for ConvertBinders which produce expressions of a specific type. In this way, a language which requires a value of a specific type from a dynamic expression will introduce an explicit ConvertBinder call site for this final conversion if it wishes to dispatch the conversion dynamically. Requiring this convention avoids very complicated conversion logic (and frankly guess work) in dynamic objects that would need to produce values appropriate for any language that happens to own the call site.

You only need to override this property if you choose to derive your binders directly from DynamicMetaObjectBinder, as the built-in DynamicMetaObjectBinder subclasses will override this property for you.

<h3 id="getupdateexpression-method">6.4.8 GetUpdateExpression Method</h3>

Returns an Expression that when executed will cause the currently executing operation to be bound again.

Even after the binding process searches through cached rules and finds an applicable binding, it may turn out that something has changed dynamically about a given object. An example would be a dynamic object which may have members added and removed. If the object has changed since the previous binding was performed, that binding may no longer be applicable.

When a chosen binding expression discovers that it is no longer an applicable binding for the current operation, it can branch to the Expression returned by GetUpdateExpression. This expression will cause the binding process that chose the current rule to attempt binding again without considering this rule. Most likely, no other rules will apply and a new binding will be produced for this operation.

<h2 id="getmemberbinder-abstract-class">6.5 GetMemberBinder Abstract Class</h2>

Represents an access to an object’s member that retrieves the value. In some languages the value may be a first-class function, such as a delegate, that closes over the instance, o, which can later be invoked.

*Example:* o.m

If the member doesn't exist, the binder may return an expression that creates a new member with some language-specific default value, returns a sentinel value like $Undefined, throws an exception, etc.

<h3 id="class-summary-4">6.5.1 Class Summary</h3>

public abstract class GetMemberBinder : DynamicMetaObjectBinder {

protected GetMemberBinder(String name, Boolean ignoreCase);

public Boolean IgnoreCase { get; }

public String Name { get; }

public abstract DynamicMetaObject FallbackGetMember

(DynamicMetaObject target, DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackGetMember

(DynamicMetaObject target);

<h3 id="name-property">6.5.2 Name Property</h3>

Returns the name of the member to retrieve.

<h3 id="ignorecase-property">6.5.3 IgnoreCase Property</h3>

Returns a Boolean value as to whether the member name lookup should be case-insensitive.

<h2 id="setmemberbinder-abstract-class">6.6 SetMemberBinder Abstract Class</h2>

Represents an access to an object’s member that assigns a value.

*Example:* o.m = 12

If the member doesn't exist, the binder may return an expression that creates a new member to hold this value, throws an exception, etc.

By convention, SetMemberBinder implementations should return rules whose result value is the value being assigned. This allows chaining of assignments in languages where assignment is an expression rather than a statement (e.g. a = b = c).

<h3 id="class-summary-5">6.6.1 Class Summary</h3>

public abstract class SetMemberBinder : DynamicMetaObjectBinder {

protected SetMemberBinder(String name, Boolean ignoreCase);

public Boolean IgnoreCase { get; }

public String Name { get; }

public abstract DynamicMetaObject FallbackSetMember

(DynamicMetaObject target, DynamicMetaObject value,

DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackSetMember

(DynamicMetaObject target, DynamicMetaObject value);

<h3 id="name-property-1">6.6.2 Name Property</h3>

Returns the name of the member to assign.

<h3 id="ignorecase-property-1">6.6.3 IgnoreCase Property</h3>

Returns a Boolean value as to whether the member name lookup should be case-insensitive.

<h2 id="deletememberbinder-abstract-class">6.7 DeleteMemberBinder Abstract Class</h2>

Represents an access to an object’s member that deletes the member.

*Example:* delete o.m

This may not be supported on all objects.

<h3 id="class-summary-6">6.7.1 Class Summary</h3>

public abstract class DeleteMemberBinder : DynamicMetaObjectBinder {

protected DeleteMemberBinder(String name, Boolean ignoreCase);

public Boolean IgnoreCase { get; }

public String Name { get; }

public abstract DynamicMetaObject FallbackDeleteMember

(DynamicMetaObject target, DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackDeleteMember

(DynamicMetaObject target);

<h3 id="name-property-2">6.7.2 Name Property</h3>

Returns the name of the member to delete.

<h3 id="ignorecase-property-2">6.7.3 IgnoreCase Property</h3>

Returns a Boolean value as to whether the member name lookup should be case-insensitive.

<h2 id="getindexbinder-abstract-class">6.8 GetIndexBinder Abstract Class</h2>

Represents an access to an indexed element of an object that retrieves the value.

*Example:* o\[10\] *or* o\[“key”\]

If the element doesn't exist, the binder may return an expression that creates a new element with some language-specific default value, return a sentinel value like $Undefined, throw an exception, etc.

<h3 id="class-summary-7">6.8.1 Class Summary</h3>

public abstract class GetIndexBinder : DynamicMetaObjectBinder {

protected GetIndexBinder(CallInfo CallInfo);

public CallInfo CallInfo { get; }

public abstract DynamicMetaObject FallbackGetIndex

(DynamicMetaObject target, DynamicMetaObject\[\] indexes,

DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackGetIndex

(DynamicMetaObject target, DynamicMetaObject\[\] indexes);

<h3 id="calllnfo-property">6.8.2 Calllnfo Property</h3>

Returns a CallInfo instance providing more information about the indexes provided. Note that the CallInfo instance only represents information about the index arguments and not the receiver of the indexing operation.

<h2 id="setindexbinder-abstract-class">6.9 SetIndexBinder Abstract Class</h2>

Represents an access to an indexed element of an object that assigns a value.

*Example:* o\[10\] = 12 *or* o\[“key”\] = value

If the member doesn't exist, the binder may return an expression that creates a new element to hold this value, throw an exception, etc.

By convention, SetIndexBinder implementations should return rules whose result value is the value being assigned. This allows chaining of assignments in languages where assignment is an expression rather than a statement (e.g. a\[1\] = b\[2\] = c).

<h3 id="class-summary-8">6.9.1 Class Summary</h3>

public abstract class SetIndexBinder : DynamicMetaObjectBinder {

protected SetIndexBinder(CallInfo CallInfo);

public CallInfo CallInfo { get; }

public abstract DynamicMetaObject FallbackSetIndex

(DynamicMetaObject target, DynamicMetaObject\[\] indexes,

DynamicMetaObject value, DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackSetIndex

(DynamicMetaObject target, DynamicMetaObject\[\] indexes,

DynamicMetaObject value);

<h3 id="calllnfo-property-1">6.9.2 Calllnfo Property</h3>

Returns a CallInfo instance providing more information about the indexes provided. Note that the CallInfo instance only represents information about the index arguments and not the receiver of the indexing operation.

<h2 id="deleteindexbinder-abstract-class">6.10 DeleteIndexBinder Abstract Class</h2>

Represents an access to an indexed element of an object that deletes the element.

*Example:* delete o.m\[10\] *or* delete o\[“key”\]

This may not be supported on all indexable objects.

<h3 id="class-summary-9">6.10.1 Class Summary</h3>

public abstract class DeleteIndexBinder : DynamicMetaObjectBinder {

protected DeleteIndexBinder(CallInfo CallInfo);

public CallInfo CallInfo { get; }

public abstract DynamicMetaObject FallbackDeleteIndex

(DynamicMetaObject target, DynamicMetaObject\[\] indexes,

DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackDeleteIndex

(DynamicMetaObject target, DynamicMetaObject\[\] indexes);

<h3 id="calllnfo-property-2">6.10.2 Calllnfo Property</h3>

Returns a CallInfo instance providing more information about the indexes provided. Note that the CallInfo instance only represents information about the index arguments and not the receiver of the indexing operation.

<h2 id="invokebinder-abstract-class">6.11 InvokeBinder Abstract Class</h2>

Represents invocation of an invocable object, such as a delegate or first-class function object.

*Example:* a(3)

<h3 id="class-summary-10">6.11.1 Class Summary</h3>

public abstract class InvokeBinder : DynamicMetaObjectBinder {

protected InvokeBinder(CallInfo CallInfo);

public CallInfo CallInfo { get; }

public abstract DynamicMetaObject FallbackInvoke

(DynamicMetaObject target, DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackInvoke(DynamicMetaObject target,

DynamicMetaObject\[\] args);

<h3 id="calllnfo-property-3">6.11.2 Calllnfo Property</h3>

Returns a CallInfo instance providing more information about the arguments provided. Note that the CallInfo instance only represents information about the arguments and not the receiver of the invocation.

<h2 id="invokememberbinder-abstract-class">6.12 InvokeMemberBinder Abstract Class</h2>

Represents invocation of an invocable member on an object, such as a method.

*Example:* a.b(3)

If invoking a member is an atomic operation in a language, its compiler can choose to generate sites using InvokeMemberBinder instead of GetMemberBinder nested within InvokeBinder. For example, in C\#, a.b may be a method group representing multiple overloads and would have no intermediate object representation for a GetMemberBinder to return.

<h3 id="class-summary-11">6.12.1 Class Summary</h3>

public abstract class InvokeMemberBinder : DynamicMetaObjectBinder {

protected InvokeMemberBinder

(String name, Boolean ignoreCase, CallInfo CallInfo);

public CallInfo CallInfo { get; }

public Boolean IgnoreCase { get; }

public String Name { get; }

public abstract DynamicMetaObject FallbackInvoke(

DynamicMetaObject target, DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackInvokeMember(

DynamicMetaObject target, DynamicMetaObject\[\] args);

public abstract DynamicMetaObject FallbackInvokeMember

(DynamicMetaObject target, DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion);

<h3 id="name-property-3">6.12.2 Name Property</h3>

Returns the name of the member to invoke.

<h3 id="ignorecase-property-3">6.12.3 IgnoreCase Property</h3>

Returns a Boolean value as to whether the member name lookup should be case-insensitive.

<h3 id="calllnfo-property-4">6.12.4 Calllnfo Property</h3>

Returns a CallInfo instance providing more information about the arguments provided. Note that the CallInfo instance only represents information about the index arguments and not the receiver of the indexing operation.

<h3 id="fallbackinvoke-method">6.12.5 FallbackInvoke Method</h3>

When emitting a callsite for the operation “a.foo()”, a language may choose to emit either a single InvokeMemberBinder or a GetMemberBinder then an InvokeBinder. To support dynamic objects that expect GetMember + Invoke in languages that create InvokeMemberBinders, an InvokeMemberBinder must provide a FallbackInvoke implementation.

The BindInvokeMember method for such an object will perform the GetMember itself and then call FallbackInvoke to ask the language to perform its invocation semantics on that member. The easiest way to implement this method is simply to create a DynamicExpression with an InvokeBinder instance and dispatch to a nested call site that will perform the Invoke on the intermediate object.

<h2 id="createinstancebinder-abstract-class">6.13 CreateInstanceBinder Abstract Class</h2>

Represents instantiation of an object with a set of constructor arguments. The object represents a type, prototype function, or other language construct that supports instantiation.

*Example:* new X(3, 4, 5)

<h3 id="class-summary-12">6.13.1 Class Summary</h3>

public abstract class CreateInstanceBinder : DynamicMetaObjectBinder {

protected CreateInstanceBinder(CallInfo CallInfo);

public CallInfo CallInfo { get; }

public abstract DynamicMetaObject FallbackCreateInstance

(DynamicMetaObject target, DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackCreateInstance

(DynamicMetaObject target, DynamicMetaObject\[\] args);

<h3 id="calllnfo-property-5">6.13.2 Calllnfo Property</h3>

Returns a CallInfo instance providing more information about the constructor arguments provided.

<h2 id="convertbinder-abstract-class">6.14 ConvertBinder Abstract Class</h2>

Represents a conversion of an object to a target type.

This conversion may be marked as being an implicit compiler-inferred conversion, or an explicit conversion specified by the developer.

*Example:* (TargetType)o

<h3 id="class-summary-13">6.14.1 Class Summary</h3>

public abstract class ConvertBinder : DynamicMetaObjectBinder {

protected ConvertBinder(Type type, Boolean explicit);

public Boolean Explicit { get; }

public Type Type { get; }

public abstract DynamicMetaObject FallbackConvert

(DynamicMetaObject target, DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackConvert(DynamicMetaObject target);

<h3 id="type-property">6.14.2 Type Property</h3>

Returns a Type instance representing the target type of the conversion operation.

<h3 id="explicit-property">6.14.3 Explicit Property</h3>

Returns true if the conversion was specified explicitly in the source code, and false if the conversion was introduced implicitly by the compiler.

<h2 id="unaryoperationbinder-abstract-class">6.15 UnaryOperationBinder Abstract Class</h2>

Represents a unary operation.

*Examples:* -a, !a

Contains an ExpressionType value that specifies the unary operation to perform, such as Negate, etc.

<h3 id="class-summary-14">6.15.1 Class Summary</h3>

public abstract class UnaryOperationBinder : DynamicMetaObjectBinder {

protected UnaryOperationBinder(ExpressionType operation);

public ExpressionType Operation { get; }

public abstract DynamicMetaObject FallbackUnaryOperation

(DynamicMetaObject target, DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackUnaryOperation

(DynamicMetaObject target);

<h3 id="operation-property">6.15.2 Operation Property</h3>

Returns an ExpressionType value representing the specific unary operation to be performed.

<h2 id="binaryoperationbinder-abstract-class">6.16 BinaryOperationBinder Abstract Class</h2>

Represents a binary operation.

*Examples:* a + b, a \* b

Contains an ExpressionType value that specifies the binary operation to perform, such as Add, Subtract, etc. The left operand’s DynamicMetaObject performs the binding or falls back to the language binder. There is currently no protocol for a call site to ask the right operand to perform a binary operation with a left operand (you can't assume the operation is commutative).

<h3 id="class-summary-15">6.16.1 Class Summary</h3>

public abstract class BinaryOperationBinder : DynamicMetaObjectBinder {

protected BinaryOperationBinder(ExpressionType operation);

public ExpressionType Operation { get; }

public abstract DynamicMetaObject FallbackBinaryOperation

(DynamicMetaObject target, DynamicMetaObject errorSuggestion);

public DynamicMetaObject FallbackBinaryOperation

(DynamicMetaObject target);

<h3 id="operation-property-1">6.16.2 Operation Property</h3>

Returns an ExpressionType value representing the specific binary operation to be performed.

<h2 id="callinfo-class">6.17 CallInfo Class</h2>

This class represents information about the arguments passed to an invocation binder, such as InvokeMemberBinder, CreateInstanceBinder, or GetIndexBinder.

Note that when you are encoding an InvokeMemberBinder, you should not include the receiver (the implicit “this” argument) as a parameter.

<h3 id="class-summary-16">6.17.1 Class Summary</h3>

public sealed class CallInfo {

public CallInfo(Int32 argCount, IEnumerable&lt;String&gt; argNames);

public CallInfo(Int32 argCount, params String\[\] argNames);

public int ArgumentCount { get; }

public ReadOnlyCollection&lt;string&gt; ArgumentNames {get; }

<h3 id="argumentcount-property">6.17.2 ArgumentCount Property</h3>

This property returns the total number of argument expressions. For example, the invocation "foo(1, 2, bar=3, baz=4)" has a count of four.

<h3 id="argumentnames-property">6.17.3 ArgumentNames Property</h3>

This property return the names used for any named arguments. If there are N names, then they are the names used in the last N argument expressions. For example "foo(1, 2, bar=3, baz=4)" has a collection of "bar" and "baz".

<h2 id="bindingrestrictions-class">6.18 BindingRestrictions Class</h2>

An instance of the BindingRestrictions class lets a DynamicMetaObject specify the constraints under which its expression would be a valid binding for a given call site in the future. If these constraints are met the next time this call site is hit, the resulting delegate will be found in the cache and rebinding will not occur.

Note that to prevent common mistakes all rules must have restrictions specified, as the vast majority of rule bindings will be applicable in only a subset of situations. If you are certain that a rule you are producing should be applicable every time this call site is reached, you can use GetExpressionRestriction to produce a conditional “if(true)” test that will be optimized away. Typically, this is not the case, and there would be at least one restriction specified, constraining this binding to a given set of input types.

It is not recommended to implement restrictions that test that a value has a nullable type as nullability is lost when boxing value types to Object. Such restrictions will be modified to test the object against the underlying non-nullable value type and nullability will not be assured.

<h3 id="class-summary-17">6.18.1 Class Summary</h3>

public abstract class BindingRestrictions {

public static readonly BindingRestrictions Empty;

public static BindingRestrictions Combine

(IList&lt;DynamicMetaObject&gt; contributingObjects);

public static BindingRestrictions GetExpressionRestriction

(Expression expression);

public static BindingRestrictions GetInstanceRestriction

(Expression expression, Object instance);

public static BindingRestrictions GetTypeRestriction

(Expression expression, Type type);

public BindingRestrictions Merge(BindingRestrictions restrictions);

public Expression ToExpression();

<h3 id="gettyperestriction-method">6.18.2 GetTypeRestriction Method</h3>

Returns a BindingRestrictions instance that validates that the specified expression’s value has the specified type at runtime.

<h3 id="getinstancerestriction-method">6.18.3 GetInstanceRestriction Method</h3>

Returns a BindingRestrictions instance that validates that the specified expression’s value is the exact specified instance at runtime.

<h3 id="getexpressionrestriction-method">6.18.4 GetExpressionRestriction Method</h3>

Returns a BindingRestrictions instance that validates that the arbitrary restriction expression specified evaluates to be true at runtime.

<h3 id="merge-method">6.18.5 Merge Method</h3>

Returns a BindingRestrictions instance that combines this instance with the restrictions specified in the argument. This is an AND combination, requiring all restrictions to pass for the rule to match.

<h3 id="combine-static-method">6.18.6 Combine Static Method</h3>

Returns a BindingRestrictions instance which combines the restrictions from each of a set of DynamicMetaObject instances. This is an AND combination, requiring all restrictions to pass for the rule to match.

<h2 id="callsite-and-callsitet-classes">6.19 CallSite and CallSite&lt;T&gt; Classes</h2>

An instance of CallSite&lt;T&gt; represents a given dynamic call site emitted by a language compiler when it encounters a dynamic expression. Compiling a DynamicExpression node in an expression tree also produces a CallSite&lt;T&gt;.

When emitting a dynamic call site, the compiler must first generate a CallSite&lt;T&gt; object by calling the static method CallSite&lt;T&gt;.Create. The compiler passes in the language-specific binder it wants this call site to use to bind operations at runtime. The T in the CallSite&lt;T&gt; is the delegate type that provides the signature for the site’s Target delegate that holds the currently cached rule, and must accept a first parameter of type CallSite.

<h3 id="class-summary-18">6.19.1 Class Summary</h3>

public abstract class CallSite {

public CallSiteBinder Binder { get; }

public static CallSite Create

(Type delegateType, CallSiteBinder binder);

}

public sealed class CallSite&lt;T&gt; : CallSite {

public T Target;

public T Update { get; }

public static CallSite&lt;T&gt; Create

(CallSiteBinder binder);

}

<h3 id="create-static-method-1">6.19.2 Create Static Method</h3>

The static factory method Create on the CallSite&lt;T&gt; class returns a new instance of CallSite&lt;T&gt; for use as a dynamic call site. Compilers that generate assemblies directly as .NET IL (such as the C\# compiler) should make sure the user assembly caches these CallSite instances at runtime so they don’t need to be regenerated each time this call site is hit. Languages that use Expression Trees as their code generation (languages built on the DLR such as IronPython) can just use DynamicExpressions, letting the Expression Tree compiler do the work.

<h3 id="target-field">6.19.3 Target Field</h3>

Holds onto the delegate representing the L0 cache for this dynamic call site. This delegate may be invoked with a set of arguments to dispatch the dynamic operation represented by the call site.

<h3 id="update-property">6.19.4 Update Property</h3>

Returns the delegate to be invoked from within the Target delegate’s implementation when there is an L0 cache miss, making it necessary to update the Target delegate.

<h2 id="strongbox-class">6.20 StrongBox Class</h2>

An instance of StrongBox&lt;T&gt; may be used by binders to represent values passed by reference. The type parameter T represents the type of the value to be referenced.

<h3 id="class-summary-19">6.20.1 Class Summary</h3>

public class StrongBox&lt;T&gt; {

public T Value;

public StrongBox&lt;T&gt;(T value);

public StrongBox&lt;T&gt;();

<h2 id="dynamicobject-class">6.21 DynamicObject Class</h2>

The simplest way to give your own class custom dynamic dispatch semantics is to derive from the DynamicObject base class. While ExpandoObject only dynamically adds and removes members, DynamicObject lets your objects fully participate in the dynamic object interoperability protocol. There are several abstract operations users of your object can then request of it dynamically, such as getting a member, setting a member, invoking a member on the object, indexing the object, invoking the object itself, or performing standard operations such as addition, multiplication, etc. DynamicObject lets you choose which operations to implement, and lets you do it much more easily than a language implementer.

For more information about DynamicObject, check out the accompanying Getting Started with the DLR for Library Authors on the [DLR CodePlex site](http://www.codeplex.com/dlr) under Specs and Docs.

<h3 id="class-summary-20">6.21.1 Class Summary</h3>

public class DynamicObject : IDynamicMetaObjectProvider {

protected DynamicObject();

public virtual Boolean TryBinaryOperation

(BinaryOperationBinder binder, Object arg, out Object result);

public virtual Boolean TryConvert

(ConvertBinder binder, out Object result);

public virtual Boolean TryCreateInstance

(CreateInstanceBinder binder, Object\[\] args,

out Object result);

public virtual Boolean TryDeleteIndex

(DeleteIndexBinder binder, Object\[\] indexes);

public virtual Boolean TryDeleteMember(DeleteMemberBinder binder);

public virtual Boolean TryGetIndex

(GetIndexBinder binder, Object\[\] args, out Object result);

public virtual Boolean TryGetMember

(GetMemberBinder binder, out Object result);

public virtual Boolean TryInvoke

(InvokeBinder binder, Object\[\] args, out Object result);

public virtual Boolean TryInvokeMember

(InvokeMemberBinder binder, Object\[\] args, out Object result);

public virtual Boolean TrySetIndex

(SetIndexBinder binder, Object\[\] indexes, Object value);

public virtual Boolean TrySetMember

(SetMemberBinder binder, Object value);

public virtual Boolean TryUnaryOperation

(UnaryOperationBinder binder, out Object result);

public virtual DynamicMetaObject GetMetaObject

(Expression parameter);

<h2 id="expandoobject-class">6.22 ExpandoObject Class</h2>

The ExpandoObject class is an efficient implementation of a dynamic property bag provided for you by the DLR. It allows you to dynamically retrieve and set its member values, adding new members per instance as needed at runtime, as typically expected of dynamic objects in languages such as Python and JScript. Instances of ExpandoObject support consumers in writing code that naturally accesses these dynamic members with dot notation (o.foo) as if they were static members, instead of something more heavyweight such as o.GetAttribute("foo").

For more information about ExpandoObject, check out the accompanying Getting Started with the DLR for Library Authors on the [DLR CodePlex site](http://www.codeplex.com/dlr) under Specs and Docs.

<h3 id="class-summary-21">6.22.1 Class Summary</h3>

public sealed class ExpandoObject : IDynamicMetaObjectProvider, IDictionary&lt;String,Object&gt;, ICollection&lt;KeyValuePair&lt;String,Object&gt;&gt;, IEnumerable&lt;KeyValuePair&lt;String,Object&gt;&gt;, IEnumerable {

public ExpandoObject();
