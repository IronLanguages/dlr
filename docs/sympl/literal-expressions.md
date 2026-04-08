# 8 Literal Expressions

This section describes some Sympl features added along with **eq** before **IF** and **loop**. These features are quoted symbols, quoted lists, and literal keyword constants. They enable more testing of **IF** and **loop**. This section also describes integer and string literals which of course were added at the beginning along with the lexer. You could skip this section unless you're curious about the built-in list objects and their runtime helpers.

<h2 id="integers-and-strings">8.1 Integers and Strings</h2>

These come out of the parser as SymplLiteralExprs with the value tucked in them. This snippet from AnalyzeExpr in etgen is the code generation:

if (expr is SymplLiteralExpr) {

return Expression.Constant(((SymplLiteralExpr)expr).Value)

<h2 id="keyword-constants">8.2 Keyword Constants</h2>

Literal keyword constants are nil, false, and true. Sympl includes nil for its built-in lists, and it includes true and false for easier .NET interoperability. Sympl could do more work in its runtime binders to map nil to false for Boolean parameters, and map anything else to true for Boolean parameters. Adding this to Sympl didn't seem to add any new lessons given the TypeModel mappings binders do.

These literal keywords come out of the parser as SymplIdExprs. Section discussed part of AnalyzeIdExpr from etgen.cs, but it omitted the literal keywords branch shown here:

public static Expression AnalyzeIdExpr(SymplIdExpr expr,

AnalysisScope scope) {

if (expr.IdToken.IsKeywordToken) {

if (expr.IdToken == KeywordToken.Nil)

return Expression.Constant(null, typeof(object));

else if (expr.IdToken == KeywordToken.True)

return Expression.Constant(true);

else if (expr.IdToken == KeywordToken.False)

return Expression.Constant(false);

else

throw new InvalidOperationException(

"Internal: unrecognized keyword literal constant.");

} else {

var param = FindIdDef(expr.IdToken.Name, scope);

if (param != null) {

return param;

} else {

return Expression.Dynamic(

new SymplGetMemberBinder(expr.IdToken.Name),

typeof(object),

scope.GetModuleExpr());

Handling this is straightforward; just turn them into ConstantExpressions with obvious .NET representations.

<h2 id="quoted-lists-and-symbols">8.3 Quoted Lists and Symbols</h2>

Sympl does stand for Symbolic Programming Language, so it needs to have symbols and built-in lists as a nod to its ancestry in Lisp-like languages. Sympl also includes these because it provided a nice domain for writing a little Sympl library, lists.sympl, and demonstrating loading libraries and cross-module access. Quoted literals are symbols, numbers, strings, lists, or lists of these things. A Symbol is an object with a name that's interned into a Sympl runtime's symbol table. All Symbols with the same name are the same object, or eq. Sympl's Symbols are similar to Lisp's, but don't have all the same slots.

<h3 id="analyzequoteexpr----code-generation">8.3.1 AnalyzeQuoteExpr -- Code Generation</h3>

The high-level idea is that quoted constants are literal constants, so Sympl builds the constant and burns it into the resulting Expression Tree as a ConstantExpression. Here's the code for AnalyzeQuoteExpr and its helper from etgen.cs:

public static Expression AnalyzeQuoteExpr(SymplQuoteExpr expr,

AnalysisScope scope) {

return Expression.Constant(MakeQuoteConstant(

expr.Expr, scope.GetRuntime()));

}

private static object MakeQuoteConstant(object expr,

Sympl symplRuntime) {

if (expr is SymplListExpr) {

SymplListExpr listexpr = (SymplListExpr)expr;

int len = listexpr.Elements.Length;

var exprs = new object\[len\];

for (int i = 0; i &lt; len; i++) {

exprs\[i\] = MakeQuoteConstant(listexpr.Elements\[i\],

symplRuntime);

}

return Cons.\_List(exprs);

} else if (expr is IdOrKeywordToken) {

return symplRuntime.MakeSymbol(

((IdOrKeywordToken)expr).Name);

} else if (expr is LiteralToken) {

return ((LiteralToken)expr).Value;

} else {

throw new InvalidOperationException(

"Internal: quoted list has -- " + expr.ToString());

As stated above, AnalyzeQuoteExpr just creates a ConstantExpression. MakeQuoteConstant does the work, and it takes a Sympl runtime instance both for runtime helper functions and to intern symbols as they are created.

Skipping the first case for a moment, if the expression is an identifier or keyword, Sympl interns its name to create a Symbol as the resulting constant. If the expression is a literal constant (string, integer, nil, false, true), the resulting constant is just the value.

If the expression is a SymplListExpr, then MakeQuoteConstant recurses to make constants out of the elements. Then it calls the runtime helper function Cons.\_List to create a Sympl built-in list as the constant to emit. See the next section for more on lists and this helper function.

<h3 id="cons-and-list-keyword-forms-and-runtime-support">8.3.2 Cons and List Keyword Forms and Runtime Support</h3>

Sympl's lists have the structure of Lisp lists, formed from chaining Cons cells together. A Cons cell has two pointers, conventionally with the first pointing to data and the second pointing to the rest of the list (another Cons cell). You can also create a Cons cell that just points to two objects even if the second object is not a Cons cell (or list tail).

Sympl provides a **cons** keyword form and a **list** keyword form for creating lists:

(cons 'a (cons 'b nil)) --&gt; (a b)

(cons 'a (cons 'b 'c)) --&gt; (a b . c)

(list 'a 'b 3 "c") --&gt; (a b 3 "c")

(cons 'a (list 2 '(b c) 3)) --&gt; (a 2 (b c) 3)

Here is the code for analyzing **cons** and **list** from etgen.cs:

public static Expression AnalyzeConsExpr (SymplConsExpr expr,

AnalysisScope scope) {

var mi = typeof(RuntimeHelpers).GetMethod("MakeCons");

return Expression.Call(mi, Expression.Convert(

AnalyzeExpr(expr.Left, scope),

typeof(object)),

Expression.Convert(

AnalyzeExpr(expr.Right, scope),

typeof(object)));

}

public static Expression AnalyzeListCallExpr

(SymplListCallExpr expr, AnalysisScope scope) {

var mi = typeof(Cons).GetMethod("\_List");

int len = expr.Elements.Length;

var args = new Expression\[len\];

for (int i = 0; i &lt; len; i++) {

args\[i\] = Expression.Convert(AnalyzeExpr(expr.Elements\[i\],

scope),

typeof(object));

}

return Expression.Call(mi, Expression

.NewArrayInit(typeof(object),

args));

Cons is just a Call node for invoking RuntimeHelpers.MakeCons. Sympl analyzes the left and right expressions, and wraps them in Convert nodes to satisfy the Call factory and make sure conversions are explicit for the Expression Tree compiler and .NET CLR. Emitting the code to call MakeCons in the IronPython implementation is not this easy. You can't call the Call factory with MethodInfo for IronPython methods. You need to emit an Invoke DynamicExpression with a no-op InvokeBinder. See the code for comments and explanation.

The List keyword form analyzes all the arguments and ultimately just emits a Call node to Cons.\_List. Because the helper function takes a params array, Sympl emits a NewArrayInit node that results in an array of objects. Each of the element arguments needs to be wrapped in a Convert node to make all the strict typing consistent for Expression Trees. The same comment as above holds about this code generation being easier in C\# than IronPython.

You can look at RuntimeHelpers.MakeCons and Cons.\_List in runtime.cs to see the code. It's not worth excerpting for this document, but there are a couple of comments to make. The reason Sympl has MakeCons at this point in Sympl's implementation evolution is that Sympl does not have type instantiation. Also, without some basic SymplGetMemberBinder or SymplSetMemberBinder that just looked up the name and assumed it was a property, for example, Sympl couldn't have some of the tests it had at this time with Cons members. The IronPython implementation at this point still didn't need the binder to do more than convey the name because the IronPython implementation of Cons had a DynamicMetaObject for free from IronPython, which handled the binding. Since IronPython ignores the ignoreCase flag, the tests had to be written with uppercase First and Rest names.
