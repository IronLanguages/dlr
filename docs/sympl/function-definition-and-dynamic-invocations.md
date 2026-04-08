# 5 Function Definition and Dynamic Invocations

This section focuses on defining Sympl functions and invoking them. Section on function call and dotted expressions discusses Sympl semantics and AnalyzeFunCallExpr quite a bit. It may be useful to read the Sympl language definition about function calls, section .

<h2 id="defining-functions">5.1 Defining Functions</h2>

Sympl uses the **defun** keyword form to define functions. **Defun** takes a name as the first argument and a list of parameter names as the second. These are non-evaluated contexts in the Sympl code. The rest of a **defun** form is a series of expressions. The last expression executed in a Sympl function provides the result value of the function. Sympl does not currently support a **return** keyword form, but you'll see the implementation is plumbed for its support. You could look at how Sympl implements **break** for loops and probably add this in 20 minutes or so as an exercise. You can see example function definitions in the .sympl files.

Code generation for defun is pretty easy. At a high-level, you add the parameter names to a new, nested AnalysisScope, analyze the sub expressions, and then emit an assignment to a file scope global whose value is a LambdaExpression. When the code in a file is gathered into an outer Lambda and compiled, all the contained lambdas get compiled (see section for more information on executing Sympl files). Here's the code from etgen.cs, which is discussed more below:

public static DynamicExpression AnalyzeDefunExpr

(SymplDefunExpr expr, AnalysisScope scope) {

if (!scope.IsModule) {

throw new InvalidOperationException(

"Use Defmethod or Lambda when not defining " +

"top-level function.");

}

return Expression.Dynamic(

scope.GetRuntime().GetSetMemberBinder(expr.Name),

typeof(object),

scope.ModuleExpr,

AnalyzeLambdaDef(expr.Params, expr.Body, scope,

"defun " + expr.Name));

}

private static Expression AnalyzeLambdaDef

(IdOrKeywordToken\[\] parms, SymplExpr\[\] body,

AnalysisScope scope, string description) {

var funscope = new AnalysisScope(scope, description);

funscope.IsLambda = true; // needed for return support.

var paramsInOrder = new List&lt;ParameterExpression&gt;();

foreach (var p in parms) {

var pe = Expression.Parameter(typeof(object), p.Name);

paramsInOrder.Add(pe);

funscope.Names\[p.Name.ToLower()\] = pe;

}

var bodyexprs = new List&lt;Expression&gt;();

foreach (var e in body) {

bodyexprs.Add(AnalyzeExpr(e, funscope));

}

var funcTypeArgs = new List&lt;Type&gt;();

for (int i = 0; i &lt; parms.Length + 1; i++) {

funcTypeArgs.Add(typeof(object));

}

return Expression.Lambda(

Expression.GetFuncType(funcTypeArgs.ToArray()),

Expression.Block(bodyexprs),

paramsInOrder);

Looking at the helper AnalyzeLambdaDef first, functions definitions need to push an AnalysisScope on the chain. The scope can serve a few purposes. Sympl uses it to hold the locals for the function's parameters. Any references to these names, barring any intervening **let\*** bindings, will resolve to the ParameterExpressions stored in this new AnalysisScope. References from nested **lambda** expressions will automatically become closure environment references thanks to Expression Trees.

The new AnalysisScope also has IsLambda set to true as plumbing for adding **return** keyword forms. Then analyzing a **return** form would just search the AnalysisScope chain for a first IsLambda scope and use a return LabelTarget stored in the scope. The body of the lambda would also need to be wrapped in a LabelExpression that used the return LabelTarget.

After storing ParameterExpressions for the locals, AnalyzeDefunExpr analyzes all the body expressions in the context of the new AnalysisScope.

To create a LambdaExpression, Sympl needs to create an array of Types determined from the parameter definitions. Of course, in Sympl, these are all object.

Now AnalyzeDefunExpr can emit the code. There is a SetMember DynamicExpression to store into the ExpandoObject for the file module. It stores a binding for the function's name to the LambdaExpression. The LambdaExpression is just a BlockExpression of the body expressions. As explained a couple of times now, just assume GetSetMemberBinder is a call to the constructor for SymplSetMemberBinder and see section for an explanation. Note, still at this point in the evolution of Sympl, its SetMemberBinder doesn't really do any work, other than convey the name and ignoreCase metadata.

This is the code from AnalyzeFunCallExpr in etgen.cs (discussed further in section ) for invoking Sympl functions (or any callable object from another language, a library, or a delegate from .NET):

var fun = AnalyzeExpr(expr.Function, scope);

List&lt;Expression&gt; args = new List&lt;Expression&gt;();

args.Add(fun);

args.AddRange(expr.Arguments.Select(a =&gt; AnalyzeExpr(a, scope)));

return Expression.Dynamic(

scope.GetRuntime()

.GetInvokeBinder(new CallInfo(expr.Arguments.Length)),

typeof(object),

args);

<h2 id="symplinvokebinder-and-binding-function-calls">5.2 SymplInvokeBinder and Binding Function Calls</h2>

At runtime, when trying to call a Sympl function, a delegate will flow into the CallSite. The default .NET meta-object will call FallbackInvoke on SymplInvokeBinder. This code is much simpler than the code for binding InvokeMember we looked at before. As a reminder, if the object that flows into the CallSite is some dynamic object that's callable (for example, an IronPython runtime function or first class type object), then its DynamicMetaObject's BindInvoke will produce a rule for invoking the object with the given arguments.

Here's the code for SymplInvokeBinder from runtime.cs, which is discussed in detail below:

public class SymplInvokeBinder : InvokeBinder {

public SymplInvokeBinder(CallInfo callinfo) : base(callinfo) {

}

public override DynamicMetaObject FallbackInvoke(

DynamicMetaObject targetMO, DynamicMetaObject\[\] argMOs,

DynamicMetaObject errorSuggestion) {

// ... Deleted COM support and checking for Defer for now ...

if (targetMO.LimitType.IsSubclassOf(typeof(Delegate))) {

var parms = targetMO.LimitType.GetMethod("Invoke")

.GetParameters();

if (parms.Length == argMOs.Length) {

var callArgs = RuntimeHelpers.ConvertArguments(

argMOs, parms);

var expression = Expression.Invoke(

Expression.Convert(targetMO.Expression,

targetMO.LimitType),

callArgs);

return new DynamicMetaObject(

RuntimeHelpers.EnsureObjectResult(expression),

BindingRestrictions.GetTypeRestriction(

targetMO.Expression,

targetMO.LimitType));

}

}

return errorSuggestion ??

RuntimeHelpers.CreateThrow(

targetMO, argMOs,

BindingRestrictions.GetTypeRestriction(

targetMO.Expression,

targetMO.LimitType),

typeof(InvalidOperationException),

"Wrong number of arguments for function -- " +

targetMO.LimitType.ToString() + " got " +

argMOs.ToString());

Let's first talk about what we aren't talking about now. This code snippet omits the code to check if the target is a COM object and to use built-in COM support. See section for information adding this to your binders. The snippet also omits some very important code that protects binders and DynamicMetaObjects from infinitely looping due to producing bad rules. It is best to discuss this in one place, so see section for how the infinite loop happens and how to prevent it for all binders.

Because language binders are expected to provide the rules for binding to static .NET objects, and the convention is that DynamicMetaObjects fall back to binders for this purpose, first this code checks for a Delegate type. This code doesn't do much for Sympl invocations. Dynamic objects fall back to languages for Delegates because the language needs to control implicit argument conversions, may provide special mappings like nil to false, and so on.

FallbackInvoke then checks the parameter and argument counts. If the delegate looks callable, Sympl optimistically converts each argument to the corresponding parameter type. See section on TypeModelMetaObject's BindInvokeMember for details on ConvertArguments. Sympl doesn't need to do more argument and parameter type matching here. This method doesn't need the information for resolving methods, and users will get an error about not being able to convert the argument to the function's parameter type as appropriate.

Sympl then creates code to convert the target object to the specific Delegate sub type, which is the LimitType of the DynamicMetaObject. See section for a discussion of using LimitType over RuntimeType. It may seem odd to convert the object to the type that LimitType reports it to be, but the type of the meta-object's expression might be more general and require an explicit Convert node to satisfy the strict typing of the Expression Tree factory or the actual emitted code that executes. The Expression Tree compiler removes unnecessary Convert nodes.

Then FallbackInvoke creates an InvokeExpression. This expression passes through EnsureObjectResult in case it needs to be wrapped to ensure it is strictly typed as assignable to object. For more information, see section 3.2.4. Then FallbackInvoke wraps this expression in the DynamicMetaObject result with the restrictions for when this rule is valid. The restrictions for Sympl's Invoke are much simpler than the InvokeMember restrictions. This rule should apply to any Delegate with the same type since the rule converts the target and arguments to the types captures in the Delegate type.

If the parameters do not match the arguments, then Sympl either uses the suggested result or creates a DynamicMetaObject result that throws an Exception. See section for a discussion of CreateThrow and restrictions. ErrorSuggestion is more interesting to discuss as it relates to member fetching and setting since it is most likely null in InvokeBinder.FallbackInvoke. If the target callable object were dynamic, then its meta-object would have directly returned a rule, rather than fall back with a result to use if the language didn't know how to invoke the dynamic object.
