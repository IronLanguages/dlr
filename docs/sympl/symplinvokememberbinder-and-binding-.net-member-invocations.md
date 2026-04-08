# 14 SymplInvokeMemberBinder and Binding .NET Member Invocations

In Sympl member invocations look like this:

some-expr.property.(method "arg").property

(x.tostring)

(x.select (lambda (e) e.Name))

(expr.property.(method).method arg1 arg2) ;; two InvokeMembers

At runtime, when trying to invoke a member of a .NET static object, the default .NET meta-object will call FallbackInvokMember on SymplInvokeMemberBinder. This code is nearly the same as TypeModelMetaObject's BindInvokeMember, with a couple of changes. If the object that flows into the CallSite is a dynamic object, then its meta-object's BindInvokeMember will produce a rule for invoking the member, as TypeModel's meta-object in Hello World.

InvokeMemberBinders need FallbackInvokeMember and FallbackInvoke methods. The second came up in Sympl when cross-library function invocation started working (see section ) due to ExpandObject's implementation of InvokeMember. Some languages and dynamic objects do not or cannot perform InvokeMember operations. They can turn InvokeMember into a GetMember and a call to the InvokeMemberBinder's FallbackInvoke method. IronPython does this, passing a DynamicMetaObject that results in a callable object that is closed over the InvokeMember operation's target object.

<h2 id="fallbackinvokemember">14.1 FallbackInvokeMember</h2>

This code is nearly the same as TypeModelMetaObject's BindInvokeMember, with a couple of changes, such as using CreateThrow rather than falling back to a binder. The details are discussed below the code for SymplInvokeMemberBinder's FallbackInvokeMember from runtime.cs:

public override DynamicMetaObject FallbackInvokeMember(

DynamicMetaObject targetMO, DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion) {

// ... Deleted checking for COM and need to Defer for now ...

var flags = BindingFlags.IgnoreCase \| BindingFlags.Instance \|

BindingFlags.Public;

var members = targetMO.LimitType.GetMember(this.Name, flags);

if ((members.Length == 1) && (members\[0\] is PropertyInfo \|\|

members\[0\] is FieldInfo)){

// Code deleted, not implemented yet.

} else {

// Get MethodInfos with right arg counts.

var mi\_mems = members.

Select(m =&gt; m as MethodInfo).

Where(m =&gt; m is MethodInfo &&

((MethodInfo)m).GetParameters().Length ==

args.Length);

List&lt;MethodInfo&gt; res = new List&lt;MethodInfo&gt;();

foreach (var mem in mi\_mems) {

if (RuntimeHelpers.ParametersMatchArguments(

mem.GetParameters(), args)) {

res.Add(mem);

}

}

var restrictions = RuntimeHelpers.GetTargetArgsRestrictions(

targetMO, args, false);

if (res.Count == 0) {

return errorSuggestion ??

RuntimeHelpers.CreateThrow(

targetMO, args, restrictions,

typeof(MissingMemberException),

"Can't bind member invoke -- " +

args.ToString());

}

var callArgs = RuntimeHelpers.ConvertArguments(

args,

res\[0\].GetParameters());

return new DynamicMetaObject(

RuntimeHelpers.EnsureObjectResult(

Expression.Call(

Expression.Convert(targetMO.Expression,

targetMO.LimitType),

res\[0\], callArgs)),

restrictions);

Let's first talk about what we aren't talking about now. This code snippet omits the code to check if the target is a COM object and to use built-in COM support. See section for information adding this to your binders. The snippet also omits some very important code that protects binders and DynamicMetaObjects from infinitely looping due to producing bad rules. It is best to discuss this in one place, so see section for how the infinite loop happens and how to prevent it for all binders.

Sympl takes the name from the binder's metadata and looks for all public, instance members on the LimitType of the value represented by this meta-object. You could just as easily decide to bind to static members here as well if your language had those semantics. Because Sympl is a case-INsensitive language, the flags include IgnoreCase.

You could also bind to data members that held sub types of Delegate. You'd then emit code to fetch the member, similar to the expression in GetRuntimeTypeMoFromModel, and use an Invoke DynamicExpression. This nests a CallSite and defers binding to SymplInvokeBinder's FallbackInvoke method, similar to what SymplInvokeMemberBinder's FallbackInvoke does. Sympl doesn't bind to data members with delegate values just to simplify the sample.

FallbackInvokeMember filters for only the members that are MethodInfos and have the right number of arguments. Then the binding logic filters for the MethodInfos that have parameters that can be bound given the kinds of arguments present at this invocation of the call site. See section for a discussion of matching parameters in the filtered MethodInfos and choosing the overload to invoke because it is the same logic here.

If FallbackInvokeMember finds no matching MethodInfos, then it either uses the suggested result or creates a DynamicMetaObject result that throws an Exception. See section for a discussion of CreateThrow and restrictions. ErrorSuggestion is discussed in section .

The rest of this function is almost exactly TypeModelMetaObject's BindInvokeMember. See section for a discussion of restrictions and argument conversions for the resulting DynamicMetaObject's MethodCallExpression. See section 3.2.4 for a discussion of EnsureObjectResult. One difference to point out is that FallbackInvokeMember needs to convert the target object to the specific LimitType of the DynamicMetaObject. See section for a discussion of using LimitType over RuntimeType. It may seem odd to convert the object to the type that LimitType reports it to be, but the type of the meta-object's expression might be more general and require an explicit Convert node to satisfy the strict typing of the Expression Tree factory or the actual emitted code that executes. The Expression Tree compiler removes unnecessary Convert nodes.

<h2 id="fallbackinvoke">14.2 FallbackInvoke</h2>

This method exists for languages and dynamic objects do not or cannot perform InvokeMember operations. Instead, they can turn InvokeMember into a GetMember and a call to the InvokeMemberBinder's FallbackInvoke method. The DLR's ExpandoObjects do this, which section discusses to get cross-module top-level function calls working. IronPython uses FallbackInvoke, passing a DynamicMetaObject that results in a callable object that is closed over the InvokeMember operation's target object.

Here's the code for SymplInvokeMemberBinder's FallbackInvoke from runtime.cs:

public override DynamicMetaObject FallbackInvoke(

DynamicMetaObject targetMO, DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion) {

var argexprs = new Expression\[args.Length + 1\];

for (int i = 0; i &lt; args.Length; i++) {

argexprs\[i + 1\] = args\[i\].Expression;

}

argexprs\[0\] = targetMO.Expression;

return new DynamicMetaObject(

Expression.Dynamic(

new SymplInvokeBinder(

new CallInfo(args.Length)),

typeof(object),

argexprs),

targetMO.Restrictions.Merge(

BindingRestrictions.Combine(args)));

The target meta-object passed to FallbackInvoke is a callable object, not the target object passed to FallbackInvokeMember that might have a member with the name in the binder's metadata. There are no checks here for COM objects because no callable COM object should flow into FallbackInvoke.

or whether FallbackInvoke needs to Defer to a nested CallSite. No , and FallbackInvoke effectively always defers to a nested CallSite (section ).

FallbackInvoke just bundles the target and args into an array to pass to the Dynamic factory method. By returning a DynamicMetaObject with a DynamicExpression, FallbackInvoke is creating a nested CallSite, so regardless of whether any argument meta-objects need to defer computation, this code works (see section for information on Defer). As with all CallInfos, the count of arguments does not include the target object even though it is in the arguments array passed to Dynamic.

The restrictions are simple too, but it is important to collect them and propagate them to the new DynamicMetaObject. There's no need to add other restrictions since no other argument conditions were used to compute a binding. This method is just deferring to the SymplInvokeBinder's FallbackInvoke method to figure out a binding.

Note, Sympl calls the SymplInvokeBinder constructor here rather than calling GetInvokeBinder from an instance of the Sympl runtime class (see section ). This means the CallSite resulting from the DynamicExpression will not share any L2 caching with other call sites. At this point in the execution of a Sympl program, Sympl binders do not have access to the Sympl instance on whose behalf the Sympl code is running. Sympl could have added a property to the binder to stash the Sympl runtime instance when creating the InvokeMember DynamicExpression in AnalyzeFunCallExpr, but you want to do that very carefully to make sure you don't hold onto working set unintentionally. Sympl could have used GetInvokeBinder in AnalyzeFunCallExpr and tucked one into the SymplInvokeMemberBinder instance in case it was needed. There are various ways to handle this, but for the sample, losing L2 cache sharing here is acceptable.
