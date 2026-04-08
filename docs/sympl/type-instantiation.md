# 10 Type instantiation

Sympl has a **new** keyword form. It takes as its first argument an expression that results in a type. Sympl code can get types in one of two ways, importing them from the hosting globals table in the Sympl runtime instance or from the result of a .NET call. One code path has to bind Sympl's TypeModel objects to constructors (TypeModelMetaObject.BindCreateInstance), and the other handles more direct .NET binding (SymplCreateInstanceBinder's FallbackCreateInstance method). The rest of the arguments to the **new** keyword form are used to find an appropriate constructor to call.

<h2 id="new-keyword-form-code-generation">10.1 New Keyword Form Code Generation</h2>

The analysis and code generation for New is pretty easy since all the work is in runtime binders and the TypeModelMetaObject. Here's the code from etgen.cs:

public static Expression AnalyzeNewExpr(SymplNewExpr expr,

AnalysisScope scope) {

List&lt;Expression&gt; args = new List&lt;Expression&gt;();

args.Add(AnalyzeExpr(expr.Type, scope));

args.AddRange(expr.Arguments.Select(

a =&gt; AnalyzeExpr(a, scope)));

return Expression.Dynamic(

scope.GetRuntime().GetCreateInstanceBinder(

new CallInfo(expr.Arguments.Length)),

typeof(object),

args);

AnalyzeNewExpr just analyzes the type expression and all the arguments to build a list of arguments for a DynamicExpression with a SymplCreateInstanceBinder. The metadata for binding is a description of the arguments. For Sympl, this is just an argument count, but note that the count does not include the target type expression even though it is in the 'args' variable passed to the Dynamic factory method.

For now, ignore GetCreateInstanceBinder. Imagine this is just a call to the constructor:

new SymplCreateInstanceBinder(CallInfo)

GetCreateInstanceBinder produces canonical binders, a single binder instance used on every call site with the same metadata. This is important for DLR L2 caching of rules. See section for how Sympl makes canonical binders and why, and see sites-binders-dynobj-interop.doc for more details on CallSite rule caching.

This DynamicExpression has a result type of object. You might think Sympl could statically type this CallSite to the type of the instance being created. However, the type is unknown until run time when some expression results in a first class type object. Therefore, as with all Dynamic expressions in Sympl, the type is object.

<h2 id="binding-createinstance-operations-in-typemodelmetaobject">10.2 Binding CreateInstance Operations in TypeModelMetaObject</h2>

One path that type instantiation can take in Sympl is from code like the following:

(set x (new System.Text.StringBuilder "hello"))

In this case one of Sympl's TypeModel objects (representing StringBuilder) flows into the CallSite into which AnalyzeNewExpr's CreateInstance DynamicExpression compiles. Then TypeModelMetaObject's BindCreateInstance produces a rule for creating the StringBuilder.

Here's the code from sympl.cs (the python code is in runtime.py):

public override DynamicMetaObject BindCreateInstance(

CreateInstanceBinder binder, DynamicMetaObject\[\] args) {

var constructors = ReflType.GetConstructors();

var ctors = constructors.

Where(c =&gt; c.GetParameters().Length == args.Length);

List&lt;ConstructorInfo&gt; res = new List&lt;ConstructorInfo&gt;();

foreach (var c in ctors) {

if (RuntimeHelpers.ParametersMatchArguments(

c.GetParameters(),

args)) {

res.Add(c);

}

}

if (res.Count == 0) {

return binder.FallbackCreateInstance(

RuntimeHelpers.GetRuntimeTypeMoFromModel(this),

args);

}

var restrictions = RuntimeHelpers.GetTargetArgsRestrictions(

this, args, true);

var ctorArgs =

RuntimeHelpers.ConvertArguments(

args, res\[0\].GetParameters());

return new DynamicMetaObject(

Expression.New(res\[0\], ctorArgs),

restrictions);

First BindCreateInstance gets the underlying RuntimeType's constructors and finds those with marching parameter counts. Then Sympl filters for constructors with matching parameters as discussed in section on TypeModelMetaObject's BindInvokeMember method.

If no constructors match, then Sympl falls back to the language binder after converting the TypeModel to a meta-object representing the RuntimeType object. See the sub section below on instantiating arrays for information on GetRuntimeTypeMoFromModel. Falling back may seem futile, but in addition to other language binders having richer matching rules that might succeed, the convention is to fall back to the binder to get a language-specific error for failing to bind.

Finally, BindCreateInstance gathers restrictions for the rule it produces and converts the arguments, as discussed in section for TypeModelMetaObject's BindInvokeMember method. Then BindCreateInstance returns the DynamicMetaObject whose restrictions and Expression property (using the Expression Tree New factory) form a rule for creating instances of the target type. The resulting expression does not need to go through EnsureObjectResult since creating an instance necessarily returns objects.

<h2 id="binding-createinstance-operations-in-fallbackcreateinstance">10.3 Binding CreateInstance Operations in FallbackCreateInstance</h2>

One path that type instantiation can take in Sympl is from code like the following:

;; x is a StringBuilder instance from the previous section

(set y (new (x.GetType) (x.ToString)))

In this case one of the DLR's default DynamicMetaObjects for static .NET objects (representing the RuntimeType object for StringBuilder) calls the SymplCreateInstanceBinder's FallbackCreateInstance method.

Here's the code from runtime.cs:

public class SymplCreateInstanceBinder : CreateInstanceBinder {

public SymplCreateInstanceBinder(CallInfo callinfo)

: base(callinfo) {

}

public override DynamicMetaObject FallbackCreateInstance(

DynamicMetaObject target,

DynamicMetaObject\[\] args,

DynamicMetaObject errorSuggestion) {

// ... Deleted checking for Defer for now ...

if (!typeof(Type).IsAssignableFrom(target.LimitType)) {

return errorSuggestion ??

RuntimeHelpers.CreateThrow(

target, args, BindingRestrictions.Empty,

typeof(InvalidOperationException),

"Type object must be used when " +

"creating instance -- " +

args.ToString());

}

var type = target.Value as Type;

Debug.Assert(type != null);

var constructors = type.GetConstructors();

// Get constructors with right arg counts.

var ctors = constructors.

Where(c =&gt; c.GetParameters().Length == args.Length);

List&lt;ConstructorInfo&gt; res = new List&lt;ConstructorInfo&gt;();

foreach (var c in ctors) {

if (RuntimeHelpers.ParametersMatchArguments(

c.GetParameters(),

args)) {

res.Add(c);

}

}

var restrictions =

RuntimeHelpers.GetTargetArgsRestrictions(

target, args, true);

if (res.Count == 0) {

return errorSuggestion ??

RuntimeHelpers.CreateThrow(

target, args, restrictions,

typeof(MissingMemberException),

"Can't bind create instance -- " +

args.ToString());

}

var ctorArgs =

RuntimeHelpers.ConvertArguments(

args, res\[0\].GetParameters());

return new DynamicMetaObject(

Expression.New(res\[0\], ctorArgs),

restrictions);

Let's first talk about what we aren't talking about now. This code snippet omits some very important code that protects binders and DynamicMetaObjects from infinitely looping due to producing bad rules. It is best to discuss this in one place, so see section for how the infinite loop happens and how to prevent it for all binders.

This coded is nearly the same as TypeModelMetaObject's BindCreateInstance discussed in the previous section. One difference to note is that while DynamicMetaObjects can fall back to binders for errors or potentially more binding searching, the binder creates Throw expressions. FallbackCreateInstance has to gather the target and argument restrictions before deciding to return an error DynamicMetaObject result so that it can ensure it uses the same restrictions it would use in a positive result. See section for a discussion of CreateThrow and restrictions.

<h2 id="instantiating-arrays-and-getruntimetypemofrommodel">10.4 Instantiating Arrays and GetRuntimeTypeMoFromModel</h2>

Because Sympl has no built-in notion of arrays (similar to IronPython), you create arrays in Sympl like you would in IronPython:

(System.Array.CreateInstance System.String 3)

This expression turns into an InvokeMember DynamicExpression. The resulting CallSite gets a Sympl TypeModel object for System.String. This is an example of why ConvertArguments and GetTargetArgsRestrictions conspire to match TypeModel objects to parameters of type Type and convert the former to the latter, as discussed in section .

Here's the code for RuntimeHelpers.GetRuntimeTypeMoFromModel from runtime.cs, which converts a DynamicMetaObject holding a TypeModel value to one holding a RuntimeType value:

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

typeModelMO.Expression, typeof(TypeModel))));

The key here is to refrain from lifting the RuntimeType value out of the TypeModel and burning it into the rule as a ConstantExpression. Instead this function returns an Expression in the DynamicMetaObject that fetches the ReflType property from the result of the Expression in the TypeModel's meta-object. This allows the rule to work more generally.
