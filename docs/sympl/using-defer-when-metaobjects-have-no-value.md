# 20 Using Defer When MetaObjects Have No Value

Binders should always protect themselves by checking whether all the DynamicMetaObjects passed to their FallbackX methods have values (see HasValue property). As a conservative implementation, if any meta-object is lacking a concrete value, then call the binder's Defer method on all the meta-objects passed in. The Defer method results in a DynamicMetaObject with an Expression that creates a nested CallSite. This allows the DynamicMetaObject expressions to be evaluated and to flow into the nested site with values. If binders did not protect themselves in this way, an infinite loop results as they produce a rule that fails when the CallSite executes it, which forces a binding update, which causes the same target DynamicMetaObject to fallback with no value, which causes the binder to produce a bad rule, and so on.

Let's look at why DynamicMetaObjects might not have values and then look at a real situation in the Sympl code that infinitely loops without HasValue checks. Sometimes dynamic languages produce partial results or return rules for performing part of an operation but then need the language binder to do the rest. A prime example when interoperating with IronPython is how it handles InvokeMember. It will fetch the member, package it in an IronPython callable object represented as an IDynamicMetaObjectProvider, and then call FallbackInvoke on your binder. The dynamic object has no value, just an expression capturing how to get the callable object. Anytime a language or DynamicMetaObject needs to return a dynamic object to a FallbackX method, it should never place a value in the DynamicMetaObject it passes to the FallbackX method. Doing so would cause the FallbackX method to try to do static .NET binding on the object, but of course, that's not right since the static nature of the object is the carrier for the dynamic nature.

For a concrete example within Sympl's implementation, consider this line of code from indexing.sympl:

(set l (new (System.Collections.Generic.List\`1.MakeGenericType

types)))

Without SymplInvokeMemberBinder.FallbackInvokeMember testing whether the DynamicMetaObjects passed to it have values and deferring, it would infinitely loop with TypeModelMetaObject.BindInvokeMember for the "MakeGenericType" member. BindINvokeMember would fall back with no value (as shown below), and the binder would produce a binding result whose rule restrictions would fail. The CallSite would then try to update the rule. The TypeModelMetaObject would fall back again with no value, and this would repeat forever.

Before adding the HasValue check to the Sympl binders, the runtime helper function GetRuntimeTypeMoFromModel had to supply a value to the meta-object it produced. This is not always possible or the right thing to do, but it worked for GetRuntimeTypeMoFromModel because it could produce a regular .NET static object for the binder that was consistent with an instance restriction on the type object.

public static DynamicMetaObject GetRuntimeTypeMoFromModel

(DynamicMetaObject typeModelMO) {

Debug.Assert((typeModelMO.LimitType == typeof(TypeModel)),

"Internal: MO is not a TypeModel?!");

// Get tm.ReflType

var pi = typeof(TypeModel).GetProperty("ReflType");

Debug.Assert(pi != null);

return new DynamicMetaObject(

Expression.Property(

Expression.Convert(typeModelMO.Expression,

typeof(TypeModel)),

pi),

typeModelMO.Restrictions.Merge(

BindingRestrictions.GetTypeRestriction(

typeModelMO.Expression, typeof(TypeModel)))//,

//((TypeModel)typeModelMO.Value).ReflType

);

When the highlight code above gets comment out, the code below is what prevents the FallbackInvokeMember function from infintely looping through the CallSite, trying to bind with the TypeModel's meta-object:

public override DynamicMetaObject FallbackInvokeMember(

DynamicMetaObject targetMO, DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion) {

// ... code deleted for example ...

if (!targetMO.HasValue \|\| args.Any((a) =&gt; !a.HasValue)) {

var deferArgs = new DynamicMetaObject\[args.Length + 1\];

for (int i = 0; i &lt; args.Length; i++) {

deferArgs\[i + 1\] = args\[i\];

}

deferArgs\[0\] = targetMO;

return Defer(deferArgs);

Every FallbackX method on all your binders should protect themselves by checking all arguments for HasValue. If HasValue is false for any, then call Defer as shown in the Sympl binders. Note, the above code is the most complicated, and for FallbackGetMember, it is just this:

if (!targetMO.HasValue) return Defer(targetMO);
