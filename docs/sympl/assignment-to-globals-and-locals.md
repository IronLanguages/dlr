# 4 Assignment to Globals and Locals

We already discussed lexical and globals in general in section . This section discusses adding variable assignment to Sympl. This starts with the keyword **set**, for which we won't discuss lexical scanning or parsing. As a reminder, Sympl is expression-based, and everything returns value. The **set** keyword form returns the value that is stored.

Let's look at the AnalyzeAssignExpr from etgen.cs:

public static Expression AnalyzeAssignExpr(SymplAssignExpr expr,

AnalysisScope scope) {

if (expr.Location is SymplIdExpr) {

var idExpr = (SymplIdExpr)(expr.Location);

var lhs = AnalyzeExpr(expr.Location, scope);

var val = AnalyzeExpr(expr.Value, scope);

var param = FindIdDef(idExpr.IdToken.Name, scope);

if (param != null) {

return Expression.Assign(

lhs,

Expression.Convert(val, param.Type)

);

} else {

var tmp = Expression.Parameter(typeof(object),

"assignTmpForRes");

return Expression.Block(

new\[\] { tmp },

Expression.Assign(

tmp,

Expression.Convert(val, typeof(object))

),

Expression.Dynamic(

scope.GetRuntime()

.GetSetMemberBinder(idExpr.IdToken.Name),

typeof(object),

scope.GetModuleExpr(),

tmp

),

tmp

);

}

// Ignore rest of function for now, discussed later with elt

// keyword and SetMember.

There are only two cases to implement at this point, lexical variables and file global variables. Later, Sympl adds setting indexed locations and .NET members. The key here is the chain of AnalysisScopes and that Expression Trees provide automatic closure environments if lifting is needed. FindIdDef (code is in etgen.cs) searches up the chain until it finds a scope with the identifier mapped to a ParameterExpression. If it finds the name, then it is a lexical variable. For lexical identifiers AnalyzeAssignExpr emits an Assign node, which guarantees to return the value stored. You also need to ensure the val expression converts to the ParameterExpression's type. The Assign factory method would throw if the Expression types were inconsistent.

If FindIdDef finds no scope mapping the identifier to a ParameterExpression, then the variable is a file global. As described in section , Sympl leverages the DLR's ExpandoObjects to represent file scopes. Sympl uses a Dynamic expression with one of its SymplSetMemberBinders, which carries the identifier name as metadata. Sympl's binders also set ignoreCase to true implicitly. We'll discuss the use of the BlockExpression after digging into the DynamicExpression a bit more.

There are a couple of points to make now in Sympl's evolving implementation. The first is to ignore GetSetMemberBinder. Imagine this is just a call to the constructor:

new SymplSetMemberBinder(idExpr.IdToken.Name)

GetSetMemberBinder produces canonical binders, a single binder instance used on every call site with the same metadata. This is important for DLR L2 caching of rules. See section for how Sympl does this and why, and see sites-binders-dynobj-interop.doc for more details on CallSite rule caching. The second point is that right now the SymplSetMemberBinder doesn't do any other work, other than convey the identifier name as metadata. We know the ExpandoObject's DynamicMetaObject will provide the implementation at runtime for how to store the value as a member.

Sympl is a case-INsensitive language. Sympl is case-preserving with identifiers stored in tokens and in binder metadata. Preserving case provides a bit more opportunity for interoperability. For example, if a Sympl file module flowed into some IronPython code, and it did case-sensitive lookups, the python code is more likely to just work. As another example, in the IronPython implementation of Sympl, where the Cons class is implemented in IronPython, it is IronPython's DynamicMetaObject that looks up the members First and Rest. IronPython still has a bug that it ignores ignoreCase on binders. While Sympl preserves case in the metadata, it uses lowercase as the canonical representation of identifiers in AnalysisScopes.

There's more code to the globals branch than the SetMember DynamicExpression. Sympl's semantics is to return a value from every expression, and it returns the value stored from assignments. Sympl wraps the DynamicExpression in a Block with a temporary variable to ensure it only evaluates the value expression once. The Block has as its last expression the ParameterExpression for the temporary variable so that the BlockExpression returns the value stored. This code is for example now, but when written ExpandoObject didn't return the values it stored. The convention for binders and DynamicMetaObjects is that they should return rules for SetMember and SetIndex operations that result in the value stored.
