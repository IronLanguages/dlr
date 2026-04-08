# 17 Arithmetic, Comparison, and Boolean Operators

Sympl supports addition, subtraction, multiplication, division, less than, greater than, equals, and not equal as the obvious corresponding Expression Tree nodes. It support **and**, **or**, and **not** logical operators (see section for their semantics). Here are example Sympl expressions:

(+ 3 5)

(+ (\* 10 x) (/ y 2))

(&lt; x max-fixnum)

(or arg default-value)

(= (not (and a b))

(or (not a) (not b)))

<h2 id="analysis-and-code-generation-for-binary-operations">17.1 Analysis and Code Generation for Binary Operations</h2>

All these operators are all keyword forms that AnalyzeBinaryExpr in etgen.cs analyzes. It picks off **and** and **or** to handle specially since they are conditionally evaluating and need to support Sympl's truth value semantics (see section on **IF** expressions). Otherwise, AnalyzeBinaryExpr just emits a BinaryOperation DynamicExpression using the operator stored in the SymplBinaryExpr AST node.

Here's the code for AnalyzeBinaryExpr, which is discussed further below:

public static Expression AnalyzeBinaryExpr(SymplBinaryExpr expr,

AnalysisScope scope) {

if (expr.Operation == ExpressionType.And) {

return AnalyzeIfExpr(

new SymplIfExpr(

expr.Left, expr.Right, null),

scope);

} else if (expr.Operation == ExpressionType.Or) {

// (Let\* (tmp1 x)

// (If tmp1 tmp1

// (Let\* (tmp2 y) (If tmp2 tmp2))))

IdOrKeywordToken tmp2 = new IdOrKeywordToken(

"\_\_tmpLetVariable2");

var tmpExpr2 = new SymplIdExpr(tmp2);

var binding2 = new LetBinding(tmp2, expr.Right); ;

var ifExpr2 = new SymplIfExpr(

tmpExpr2, tmpExpr2, null);

var letExpr2 = new SymplLetStarExpr(

new\[\] { binding2 },

new\[\] { ifExpr2 });

// Build outer let\*

IdOrKeywordToken tmp1 = new IdOrKeywordToken(

"\_\_tmpLetVariable1");

var tmpExpr1 = new SymplIdExpr(tmp1);

LetBinding binding1 = new LetBinding(tmp1, expr.Left); ;

SymplExpr ifExpr1 = new SymplIfExpr(

tmpExpr1, tmpExpr1, letExpr2);

return AnalyzeLetStarExpr(

new SymplLetStarExpr(

new\[\] { binding1 },

new\[\] { ifExpr1 }

),

scope

);

}

return Expression.Dynamic(

scope.GetRuntime().GetBinaryOperationBinder(expr.Operation),

typeof(object),

AnalyzeExpr(expr.Left, scope),

AnalyzeExpr(expr.Right, scope));

Because **and** and **or** have equivalent semantic to **IF** (with some temporary bindings for **or**), the code above creates ASTs for **IF** and re-uses the AnalyzeIfExpr and AnalyzeLetStarExpr. Sympl could also have "open coded" the equivalent Expression Tree code generation here.

If the operation is other than **and** and **or**, AnalyzeBinaryExpr emits a DynamicExpression with a SymplBinaryOperationBinder. See section for a discussion of why this method calls GetBinaryOperationBinder rather than just calling the constructor. The reason Sympl uses a DynamicExpression when it only supports the static built-in semantics of Expression Trees is for interoperability with dynamic languages from other languages or libraries that might flow though a Sympl program.

<h2 id="analysis-and-code-generation-for-unary-operations">17.2 Analysis and Code Generation for Unary Operations</h2>

The only unary operation Sympl supports is logical negation. Here's the code for AnalyzeUnaryExpr in etgen.cs:

public static Expression AnalyzeUnaryExpr(SymplUnaryExpr expr,

AnalysisScope scope) {

if (expr.Operation == ExpressionType.Not) {

return Expression.Not(WrapBooleanTest(

AnalyzeExpr(expr.Operand,

scope)));

}

return Expression.Dynamic(

scope.GetRuntime()

.GetUnaryOperationBinder(expr.Operation),

typeof(object),

AnalyzeExpr(expr.Operand, scope));

Execution never reaches the DynamicExpression result. This is there as plumbing and an example should Sympl support other unary operations, such as binary or arithmetic negation.

Sympl's logical **not** translates directly to an Expression Tree Not node as long as the operand expression has a Boolean type. Because of Sympl's truth semantics (see section on **IF** expressions), AnalyzeUnaryExpr calls WrapBooleanTest which results in an Expression with Type bool.

<h2 id="symplbinaryoperationbinder">17.3 SymplBinaryOperationBinder</h2>

Binding binary operations is pretty easy in Sympl. Here's the code for the binder's FallbackBinaryOperation in runtime.cs:

public override DynamicMetaObject FallbackBinaryOperation(

DynamicMetaObject target, DynamicMetaObject arg,

DynamicMetaObject errorSuggestion) {

var restrictions = target.Restrictions.Merge(arg.Restrictions)

.Merge(BindingRestrictions.GetTypeRestriction(

target.Expression, target.LimitType))

.Merge(BindingRestrictions.GetTypeRestriction(

arg.Expression, arg.LimitType));

return new DynamicMetaObject(

RuntimeHelpers.EnsureObjectResult(

Expression.MakeBinary(

this.Operation,

Expression.Convert(target.Expression, target.LimitType),

Expression.Convert(arg.Expression, arg.LimitType))),

restrictions);

This function gathers all the restrictions for the arguments and merges them with the target's restrictions. Then it also merges in restrictions to ensure the arguments are the same LimitType as they have during this pass through the CallSite; the rule produced is only good for those types.

Then FallbackBinaryOperation returns a DynamicMetaObject with a BinaryExpression. The arguments are wrapped in ConvertExpressions to ensure they have the strict typing required for the BinaryExpression node returned. Recall that the argument's expression type may be more general than the actual LimitType they have at run time. The result BinaryExpression also passes through EnsureObjectResult in case it needs to be wrapped to ensure it is strictly typed as assignable to object. For more information, see section 3.2.4.

<h2 id="symplunaryoperationbinder">17.4 SymplUnaryOperationBinder</h2>

As stated above, Sympl never really uses its UnaryOperationBinder. It exists a plumbing for future unary features and as an example. It is exactly like FallbackBinaryOperation except that it only has one argument to process.

Here's the code form runtime.cs.

public override DynamicMetaObject FallbackUnaryOperation(

DynamicMetaObject target,

DynamicMetaObject errorSuggestion) {

return new DynamicMetaObject(

RuntimeHelpers.EnsureObjectResult(

Expression.MakeUnary(

this.Operation,

Expression.Convert(target.Expression, target.LimitType),

target.LimitType)),

target.Restrictions.Merge(

BindingRestrictions.GetTypeRestriction(

target.Expression, target.LimitType)));
