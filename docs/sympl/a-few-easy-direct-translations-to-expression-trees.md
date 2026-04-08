# 7 A Few Easy, Direct Translations to Expression Trees

Next the Sympl implementation gained some easy constructs that are either direct translations to Expression Trees or features that leverage code already written for Sympl.

<h2 id="let-binding">7.1 Let\* Binding</h2>

**Let\*** binding creates a shadowing bind of an identifier name. For example, a function has a parameter named "x" that comes in set to 10. You **let\*** bind "x" to 20, and maybe you set it to other values. When you exit the scope of the **let\*** keyword form, references to "x" see the value 10, barring any sets or other intervening **let\*** bindings. **Let\*** takes one or more lexical bindings and a sequence of sub expressions. It results in the value of the last sub expression executed. The bindings occur serially so that later initialization expressions can refer to earlier bindings. Here's an example:

(let\* ((var1 init1)

(var2 init2))

e1 e2 e3)

Here's the code from etgen.cs that handles this keyword form:

public static Expression AnalyzeLetStarExpr(SymplLetStarExpr expr,

AnalysisScope scope) {

var letscope = new AnalysisScope(scope, "let\*");

// Analyze bindings.

List&lt;Expression&gt; inits = new List&lt;Expression&gt;();

List&lt;ParameterExpression&gt; varsInOrder =

new List&lt;ParameterExpression&gt;();

foreach (var b in expr.Bindings) {

// Need richer logic for mvbind

var v = Expression.Parameter(typeof(object),

b.Variable.Name);

varsInOrder.Add(v);

inits.Add(

Expression.Assign(

v,

Expression.Convert(AnalyzeExpr(b.Value, letscope),

v.Type))

);

letscope.Names\[b.Variable.Name.ToLower()\] = v;

}

List&lt;Expression&gt; body = new List&lt;Expression&gt;();

foreach (var e in expr.Body) {

body.Add(AnalyzeExpr(e, letscope));

}

inits.AddRange(body);

return Expression.Block(typeof(object), varsInOrder.ToArray(),

inits);

This code pushes a new, nested AnalysisScope just like Sympl did for function definition parameters. However, AnalyzeLetStarExpr handles the nested scope differently. It adds each variable to the nested scope after analyzing the initialization expression for the variable. This is obvious from the stand point that the initialization expression cannot refer to the variable for which it is producing the initial value. However, to implement let\* semantics instead of let semantics (which is effectively "parallel" assignment), you need to make sure you add each variable to the scope so that successive variable initialization expressions can refer to previous variables.

The rest of the code just analyzes each sub expression in the nested scope and wraps them in a BlockExpression, which returns the value of the last sub expression.

<h2 id="lambda-expressions-and-closures">7.2 Lambda Expressions and Closures</h2>

Sympl has lambda keyword forms for first class functions. These are just like defun keyword forms, except they have no name. The results of lambda expression can be assigned to lexical or global variables, and assigning one to a global variable would be the same as using defun. To create local functions that are recursive, you need to create a let\* binding of some variable, define a lambda that refers to that variable, and then assign the lambda's result to the variable.

As mentioned previously, closures are automatic with Expression Trees. Any uses references to ParameterExpressions that were established as lambda parameters or Block variables outside of the referencing lambda get lifted to closure environments as needed.

The code for lambda expression is AnalyzeLambdaExpr in etgen.cs. It just calls the helper method used by AnalyzeDefunExpr discussed in section .

<h2 id="conditional-if-expressions">7.3 Conditional (IF) Expressions</h2>

Sympl has an **IF** keyword expression. **IF** takes two or three arguments. If the first argument is true (that is, NOT **nil** or **false**), the **IF** results in the value produced by the second argument. If the first argument is **nil** or **false**, then **IF** results in the value of the third argument. If there is no third argument, then the value is **false**.

To have something easy to test at this point in Sympl's implementation, we added an **eq** test and a few keyword literal constants (**nil**, **true**, **false**), which are described in sub sections below.

Here's the code for **IF** from etgen.cs, which is described further below:

public static Expression AnalyzeIfExpr (SymplIfExpr expr,

AnalysisScope scope) {

Expression alt = null;

if (expr.Alternative != null) {

alt = AnalyzeExpr(expr.Alternative, scope);

} else {

alt = Expression.Constant(false);

}

return Expression.Condition(

WrapBooleanTest(AnalyzeExpr(expr.Test, scope)),

Expression.Convert(AnalyzeExpr(expr.Consequent,

scope),

typeof(object)),

Expression.Convert(alt, typeof(object)));

}

private static Expression WrapBooleanTest (Expression expr) {

var tmp = Expression.Parameter(typeof(object), "testtmp");

return Expression.Block(

new ParameterExpression\[\] { tmp },

new Expression\[\]

{Expression.Assign(tmp, Expression

.Convert(expr,

typeof(object))),

Expression.Condition(

Expression.TypeIs(tmp, typeof(bool)),

Expression.Convert(tmp, typeof(bool)),

Expression.NotEqual(

tmp,

Expression.Constant(null,

typeof(object))))});

The first thing AnalyzeIfExpr does is get an expression for the alternative branch or third argument to IF. This defaults to the constant false. Then it simply emits a Conditional Expression with the analyzed sub expressions, forcing everything to type object so that the Conditional factory method sees consistent types and also emits code to return a value.

AnalyzeIfExpr uses WrapBoolenTest. It emits an Expression that captures Sympl's truth value semantics (anything that is not nil or false is true). WrapBooleanTest uses a Block and temporary variable to evaluate the test expression only once. Then it uses an inner ConditionalExpression. If the type of the test expression is bool, then just convert the value to bool. Otherwise, return whether the test value is not null (our runtime representation of nil).

<h2 id="eq-expressions">7.4 Eq Expressions</h2>

To have something to test along with IF at this point in Sympl's evolution, we added the **eq** keyword form. We can implement this quickly as a runtime helper rather than getting into BinaryOperationBinders and design in the parser. **Eq** has the semantics of returning **true** if the arguments are integers with the same value, or the arguments are the same identical object in memory. To implement **eq**, Sympl just emits a call to it the runtime helper (from etgen.cs):

public static Expression AnalyzeEqExpr (SymplEqExpr expr,

AnalysisScope scope) {

var mi = typeof(RuntimeHelpers).GetMethod("SymplEq");

return Expression.Call(mi, Expression.Convert(

AnalyzeExpr(expr.Left, scope),

typeof(object)),

Expression.Convert(

AnalyzeExpr(expr.Right, scope),

typeof(object)));

Sympl needs to place the ConvertExpressions in the argument list to satisfy the Call factory and make sure conversions are explicit for the Expression Tree compiler and .NET CLR.

Emitting the code to call SymplEq in the IronPython implementation is not this easy. You can't call the Call factory with MethodInfo for IronPython methods. You need to emit an Invoke DynamicExpression with a no-op InvokeBinder. See the code for comments and explanation.

This is the code for the runtime helper function, from the class RuntimeHelpers in runtime.cs:

public static bool SymplEq (object x, object y) {

if (x == null)

return y == null;

else if (y == null)

return x == null;

else {

var xtype = x.GetType();

var ytype = y.GetType();

if (xtype.IsPrimitive && xtype != typeof(string) &&

ytype.IsPrimitive && ytype != typeof(string))

return x.Equals(y);

else

return object.ReferenceEquals(x, y);

There's not much to explain further since It is a pretty direct implementation of the semantics described above.

<h2 id="loop-expressions">7.5 Loop Expressions</h2>

Adding loops is almost straightforward translations to Expression Trees. Sympl only has one kind of loop. It repeats forever until the code calls the **break** keyword form. It would be trivial to add **continue** since it is directly supported by Expression Trees like **break**, but demonstrating **break** is enough to show you what to do. For a whileloop and foreach example, see the Expression Tree spec on [www.codeplex.com/dlr](http://www.codeplex.com/dlr) .

Here's the code for AnalyzeLoopExpr and AnalyzeBreakExpr from etgen.cs, which are described further below:

public static Expression AnalyzeLoopExpr (SymplLoopExpr expr,

AnalysisScope scope) {

var loopscope = new AnalysisScope(scope, "loop ");

loopscope.IsLoop = true; // needed for break and continue

loopscope.LoopBreak = Expression.Label(typeof(object),

"loop break");

int len = expr.Body.Length;

var body = new Expression\[len\];

for (int i = 0; i &lt; len; i++) {

body\[i\] = AnalyzeExpr(expr.Body\[i\], loopscope);

}

return Expression.Loop(Expression.Block(typeof(object), body),

loopscope.LoopBreak);

}

public static Expression AnalyzeBreakExpr (SymplBreakExpr expr,

AnalysisScope scope) {

var loopscope = \_findFirstLoop(scope);

if (loopscope == null)

throw new InvalidOperationException(

"Call to Break not inside loop.");

Expression value;

if (expr.Value == null)

value = Expression.Constant(null, typeof(object));

else

// Ok if value jumps to break label.

value = AnalyzeExpr(expr.Value, loopscope);

return Expression.Break(loopscope.LoopBreak, value,

typeof(object));

AnalyzeLoopExpr needs to push a new AnalysisScope on the chain. This enables AnalyzeBreakExpr to confirm the break is within a loop and to find the LabelTarget to use to escape from the innermost containing loop expression. Sympl makes the LabelTarget have type object because Sympl loops are expressions that can produce values. This is explained further below.

AnalyzeLoopExpr then just analyzes the sequence of body expressions and emits a LoopExpression. The LoopExpression takes one expression, hence the Block wrapping the body expressions. The factory also takes the LabelTarget that is used to jump past the end of the loop. The break target's type becomes the value of LoopExpression.Type.

Sympl's break keyword form optionally takes one argument, the result value for the loop. AnalyzeBreakExpr finds the innermost containing loop. Then it analyzes the value expression if there is one, or just uses null (which is Sympl's nil value). The Break factory returns a GotoExpression node that compiles to a jump to the loop's break label target, carrying along the value. The object type argument to the Break factory is required because the factory doesn't use the LabelTarget's Type property to set the GotoExpresson's Type property. Without the object type argument, the Goto would have type void. If the code had an IF expression with a branch that called break, then the ConvertExpressions that Sympl wraps around IF branches would throw because they couldn't convert void (from the break expression) to object.
