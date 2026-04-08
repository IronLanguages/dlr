# 4 API Reference

This section contains sub sections for each type of ET node, and they have sub sections for each member. While all factories are on the Expression type, this document describes them in a sub section for the type of ET node that the factories create. All factory methods return new objects each time you call them. They return fresh objects so that you can associate them with unique annotations when that's needed. If you need caching for working set pressure or other performance turning (for example, re-using all Expression.Constant(1) nodes), then you need to provide that.

For economy of definition, this spec often refers to C\# operators or behaviors. The ET model is explicitly NOT slanted towards C\#. There are times when the nodes do not have equivalent C\# semantics for generality of modeling, for example, the TypeIs node kind or BinaryExpressions with comparison node kinds. It is often MUCH easier for the reader to site a C\# behavior with equivalent semantics than to spec fundamental operators, CLR behaviors, numeric types, etc., to capture the semantics of an ET node.

<h2 id="terminology-for-lifted-v1-spec-text-marked-by-boxed-red-text">4.1 Terminology for Lifted V1 Spec Text (marked by boxed red text)</h2>

The Expression class contains many static factory methods. These are the only public means of creating ETs. The factory methods are described in the sub sections for the classes identified as the factory method return types.

**Integral type** -- one of byte, sbyte, short, ushort, int, uint, long, ulong, or the corresponding nullable types

**Numeric type** -- an integral type; one of float, double, decimal, or char; or the corresponding nullable types

**Predefined type** -- a numeric or boolean type (usually written as "numeric or boolean" instead of "predefined" to avoid ambiguity with BCL types)

**Predefined operator** -- one that is specified to exist by the C\# Language Specification (usually written with "C\#" in front to avoid ambiguity with IL instructions).

A type T1 is assignable to another T2 only if one of the following criteria holds:

- T1 and T2 are the same

- T1 and T2 are classes and T1 directly or indirectly inherits from T2

- T1 and T2 are interfaces and T1 directly or indirectly inherits from T2

- T1 is an array type T\[\] and T2 is IList&lt;T&gt; or one of the interfaces it derives from

Note that a value type is only assignable to itself. Assignability does not include notions of boxing, conversions, etc., so while a value in some languages may be allowed to be assigned to type Object, in an ET, you would need an explicit conversion to Object.

<h2 id="quirks-mode-for-silverlight">4.2 Quirks Mode for Silverlight</h2>

Silverlight has a high bar for compatibility between its releases. Due to code shipped in SL3, for which we fixed bugs, we had to keep some behaviors when programmers throw the Quirks Mode switch for SL4 and later SL versions. Here's a list of those changes when you use ETs in Quirks Mode (these are NOT the behaviors in .NET 4.0 or desktop builds of DLR from Codeplex.com):

- Improvements to ToString results are reverted in Quirks Mode.

- The Quote factory method allows calls in Quirks Mode that resulted in trees that caused errors as explained in section .

- The Call factory method does not throw an error when you pass a 'this' argument to a static method in Quirks Mode.

- When passing get\_... and set\_... MemberInfos to factory methods, we do not map them in some cases to PropertyInfos in Quirks Mode.

- If you pass a ReadOnlyCollection&lt;T&gt; to some factory method in Quirks Mode, you will be able to mutate the underlying data and effectively undermind the immutability of ET nodes.

<h2 id="expression-abstract-class">4.3 Expression Abstract Class</h2>

This abstract class is the base class from which all ET node types derive. This class contains many static factory methods (listed in the class summary) for sub types. See the node type sub section for factory methods semantics.

If we add annotations back in v-next+1, we need to make sure we capture that passing null Annotations is the same as supplying Annotations.Empty.

<h3 id="class-summary">4.3.1 Class Summary</h3>

public abstract class Expression {

protected Expression();

public virtual ExpressionType NodeType { get; }

public virtual Type Type { get; }

public virtual Boolean CanReduce { get; }

public virtual Expression Reduce();

public Expression ReduceAndCheck();

public Expression ReduceExtensions();

public static Type GetActionType(params Type\[\] typeArgs);

public static bool TryGetActionType(Type\[\] typeArgs,

out Type actionType)

public static Type GetFuncType(params Type\[\] typeArgs);

public static bool TryGetFuncType(Type\[\] typeArgs,

out Type funcType);

public static Type GetDelegateType(params Type\[\] typeArgs)

protected virtual Expression VisitChildren

(ExpressionVisitor visitor);

protected internal virtual Expression Accept

(ExpressionVisitor visitor)

// These build only on Codeplex.com for debugging aids (may

// be added in v-next+1). DebugView is build into CLR 4.0, but it

// it is private for use with VS datatips and debugging tools only.

public string DebugView {get {}}

public void DumpExpression(string descr, TextWriter writer) {

public static BinaryExpression Add

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Add(Expression left,

Expression right);

public static BinaryExpression AddAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AddAssign(Expression left,

Expression right);

public static BinaryExpression AddAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression AddAssignChecked(Expression left,

Expression right);

public static BinaryExpression AddAssignChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AddAssignChecked

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression AddChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AddChecked(Expression left,

Expression right);

public static BinaryExpression And(Expression left,

Expression right);

public static BinaryExpression And

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AndAlso(Expression left,

Expression right);

public static BinaryExpression AndAlso

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AndAssign(Expression left,

Expression right);

public static BinaryExpression AndAssign

(Expression left, Expression right, MethodInfo method);

public static IndexExpression ArrayAccess

(Expression array, IEnumerable&lt;Expression&gt; indexes);

public static IndexExpression ArrayAccess

(Expression array, params Expression\[\] indexes);

// The ArrayIndex factories will be obsolete in lieu of more

// general IndexExpression factories above.

public static BinaryExpression ArrayIndex(Expression array,

Expression index);

public static MethodCallExpression ArrayIndex

(Expression array, params Expression\[\] indexes);

public static MethodCallExpression ArrayIndex

(Expression array, IEnumerable&lt;Expression&gt; indexes);

public static UnaryExpression ArrayLength(Expression array);

public static BinaryExpression Assign(Expression left,

Expression right);

public static MemberAssignment Bind(MethodInfo propertyAccessor,

Expression expression);

public static MemberAssignment Bind(MemberInfo member,

Expression expression);

public static BlockExpression Block

(IEnumerable&lt;ParameterExpression&gt; variables,

params Expression\[\] expressions);

public static BlockExpression Block

(Type type,

IEnumerable&lt;ParameterExpression&gt; variables,

params Expression\[\] expressions);

public static BlockExpression Block

(IEnumerable&lt;ParameterExpression&gt; variables,

IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(Type type,

IEnumerable&lt;ParameterExpression&gt; variables,

IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(Type type, IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(Expression arg0, Expression arg1, Expression arg2,

Expression arg3, Expression arg4);

public static BlockExpression Block

(params Expression\[\] expressions);

public static BlockExpression Block

(Type type, params Expression\[\] expressions);

public static BlockExpression Block(Expression arg0,

Expression arg1);

public static BlockExpression Block

(Expression arg0, Expression arg1, Expression arg2);

public static BlockExpression Block

(Expression arg0, Expression arg1, Expression arg2,

Expression arg3);

public static GotoExpression Break(LabelTarget target,

Expression value);

public static GotoExpression Break(LabelTarget target);

public static MethodCallExpression Call

(MethodInfo method, params Expression\[\] arguments);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1,

Expression arg2, Expression arg3, Expression arg4);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1);

public static MethodCallExpression Call

(Expression instance, MethodInfo method,

params Expression\[\] arguments);

public static MethodCallExpression Call

(Expression instance, MethodInfo method, Expression arg0,

Expression arg1, Expression arg2);

public static MethodCallExpression Call

(Expression instance, MethodInfo method, Expression arg0,

Expression arg1);

public static MethodCallExpression Call(MethodInfo method,

Expression arg0);

public static MethodCallExpression Call

(Expression instance, String methodName, Type\[\] typeArguments,

params Expression\[\] arguments);

public static MethodCallExpression Call

(Type type, String methodName, Type\[\] typeArguments,

params Expression\[\] arguments);

public static MethodCallExpression Call

(Expression instance, MethodInfo method,

IEnumerable&lt;Expression&gt; arguments);

public static MethodCallExpression Call

(MethodInfo method,

IEnumerable&lt;Expression&gt; arguments);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1,

Expression arg2, Expression arg3);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1,

Expression arg2);

public static MethodCallExpression Call(Expression instance,

MethodInfo method);

public static CatchBlock Catch(ParameterExpression variable,

Expression body);

public static CatchBlock Catch(Type type, Expression body);

public static CatchBlock Catch

(ParameterExpression variable, Expression body,

Expression filter);

public static CatchBlock Catch(Type type, Expression body,

Expression filter);

public static BinaryExpression Coalesce(Expression left,

Expression right);

public static BinaryExpression Coalesce

(Expression left, Expression right,

LambdaExpression conversion);

public static ConditionalExpression Condition

(Expression test, Expression ifTrue, Expression ifFalse);

public static ConditionalExpression Condition

(Expression test, Expression ifTrue, Expression ifFalse,

Type type);

public static ConditionalExpression IfThen

(Expression test, Expression ifTrue)

public static ConditionalExpression IfThenElse

(Expression test, Expression ifTrue, Expression ifFalse);

public static ConstantExpression Constant(Object value);

public static ConstantExpression Constant(Object value,

Type type);

public static GotoExpression Continue(LabelTarget target);

public static UnaryExpression Convert

(Expression expression, Type type, MethodInfo method);

public static UnaryExpression Convert(Expression expression,

Type type);

public static UnaryExpression ConvertChecked

(Expression expression, Type type, MethodInfo method);

public static UnaryExpression ConvertChecked

(Expression expression, Type type);

public static CatchBlock CreateCatchBlock

(Type type, ParameterExpression variable, Expression body,

Expression filter);

public static DebugInfoExpression DebugInfo

(SymbolDocumentInfo document, Int32 startLine,

Int32 startColumn, Int32 endLine, Int32 endColumn);

public static DebugInfoExpression ClearDebugInfo

(SymbolDocumentInfo document)

public static SymbolDocumentInfo SymbolDocument(String fileName,

Guid language);

public static SymbolDocumentInfo SymbolDocument(String fileName);

public static SymbolDocumentInfo SymbolDocument

(String fileName, Guid language, Guid languageVendor);

public static SymbolDocumentInfo SymbolDocument

(String fileName, Guid language, Guid languageVendor,

Guid documentType);

public static UnaryExpression Decrement(Expression expression,

MethodInfo method);

public static UnaryExpression Decrement(Expression expression);

public static DefaultExpression Default(Type type);

public static DefaultExpression Empty();

public static BinaryExpression Divide(Expression left,

Expression right);

public static BinaryExpression Divide

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression DivideAssign(Expression left,

Expression right);

public static BinaryExpression DivideAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression DivideAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

­

public static ElementInit ElementInit

(MethodInfo addMethod,

IEnumerable&lt;Expression&gt; arguments);

public static ElementInit ElementInit

(MethodInfo addMethod, params Expression\[\] arguments);

public static BinaryExpression Equal(Expression left,

Expression right);

public static BinaryExpression Equal

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression ReferenceEqual(Expression left,

Expression right)

public static BinaryExpression NotEqual(Expression left,

Expression right);

public static BinaryExpression NotEqual

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression ReferenceNotEqual(Expression left,

Expression right)

public static BinaryExpression ExclusiveOr

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression ExclusiveOr(Expression left,

Expression right);

public static BinaryExpression ExclusiveOrAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression ExclusiveOrAssign

(Expression left, Expression right);

public static BinaryExpression ExclusiveOrAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static MemberExpression Field(Expression expression,

String fieldName);

public static MemberExpression Field

(Expression expression, Type type, String fieldName);

public static MemberExpression Field(Expression expression,

FieldInfo field);

public static GotoExpression Goto(LabelTarget target);

public static GotoExpression Goto(LabelTarget target,

Expression value);

public static BinaryExpression GreaterThan

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression GreaterThan(Expression left,

Expression right);

public static BinaryExpression GreaterThanOrEqual

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression GreaterThanOrEqual

(Expression left, Expression right);

public static UnaryExpression Increment(Expression expression,

MethodInfo method);

public static UnaryExpression Increment(Expression expression);

public static InvocationExpression Invoke

(Expression expression, params Expression\[\] arguments);

public static InvocationExpression Invoke

(Expression expression, IEnumerable&lt;Expression&gt; arguments);

public static UnaryExpression IsFalse(Expression expression) {

public static UnaryExpression IsFalse(Expression expression,

MethodInfo method) {

public static UnaryExpression IsTrue(Expression expression) {

public static UnaryExpression IsTrue(Expression expression,

MethodInfo method) {

public static LabelTarget Label(Type type, String name);

public static LabelTarget Label(Type type);

public static LabelTarget Label();

public static LabelTarget Label(String name);

public static LabelExpression Label(LabelTarget target);

public static LabelExpression Label(LabelTarget target,

Expression defaultValue);

public static LambdaExpression Lambda

(Expression body, params ParameterExpression\[\] parameters);

public static LambdaExpression Lambda

(Expression body,

IEnumerable&lt;ParameterExpression&gt; parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body, String name,

IEnumerable&lt;ParameterExpression&gt; parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body, params ParameterExpression\[\] parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body,

IEnumerable&lt;ParameterExpression&gt; parameters);

public static LambdaExpression Lambda

(Expression body, String name,

IEnumerable&lt;ParameterExpression&gt; parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body, String name,

IEnumerable&lt;ParameterExpression&gt; parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body,

params ParameterExpression\[\] parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body,

IEnumerable&lt;ParameterExpression&gt; parameters);

public static BinaryExpression LeftShift

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression LeftShift(Expression left,

Expression right);

public static BinaryExpression LeftShiftAssign(Expression left,

Expression right);

public static BinaryExpression LeftShiftAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression LeftShiftAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression LessThan

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression LessThan(Expression left,

Expression right);

public static BinaryExpression LessThanOrEqual

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression LessThanOrEqual(Expression left,

Expression right);

public static MemberListBinding ListBind

(MemberInfo member,

IEnumerable&lt;ElementInit&gt;

initializers);

public static MemberListBinding ListBind

(MemberInfo member, params ElementInit\[\] initializers);

public static MemberListBinding ListBind

(MethodInfo propertyAccessor,

IEnumerable&lt;ElementInit&gt;

initializers);

public static MemberListBinding ListBind

(MethodInfo propertyAccessor,

params ElementInit\[\] initializers);

public static ListInitExpression ListInit

(NewExpression newExpression,

params ElementInit\[\] initializers);

public static ListInitExpression ListInit

(NewExpression newExpression, MethodInfo addMethod,

params Expression\[\] initializers);

public static ListInitExpression ListInit

(NewExpression newExpression,

IEnumerable&lt;ElementInit&gt; initializers);

public static ListInitExpression ListInit

(NewExpression newExpression,

IEnumerable&lt;Expression&gt; initializers);

public static ListInitExpression ListInit

(NewExpression newExpression,

params Expression\[\] initializers);

public static ListInitExpression ListInit

(NewExpression newExpression, MethodInfo addMethod,

IEnumerable&lt;Expression&gt; initializers);

public static LoopExpression Loop(Expression body);

public static LoopExpression Loop

(Expression body, LabelTarget break, LabelTarget continue);

public static LoopExpression Loop(Expression body,

LabelTarget break);

public static BinaryExpression MakeBinary

(ExpressionType binaryType, Expression left,

Expression right);

public static BinaryExpression MakeBinary

(ExpressionType binaryType, Expression left, Expression right,

Boolean liftToNull, MethodInfo method);

public static BinaryExpression MakeBinary

(ExpressionType binaryType, Expression left, Expression right,

Boolean liftToNull, MethodInfo method,

LambdaExpression conversion);

public static GotoExpression MakeGoto

(GotoExpressionKind kind, LabelTarget target,

Expression value);

public static IndexExpression MakeIndex

(Expression instance, PropertyInfo indexer,

IEnumerable&lt;Expression&gt; arguments);

public static MemberExpression MakeMemberAccess

(Expression expression, MemberInfo member);

public static UnaryExpression MakeUnary

(ExpressionType unaryType, Expression operand, Type type);

public static UnaryExpression MakeUnary

(ExpressionType unaryType, Expression operand, Type type,

MethodInfo method);

public static MemberMemberBinding MemberBind

(MemberInfo member,

IEnumerable&lt;MemberBinding&gt; bindings);

public static MemberMemberBinding MemberBind

(MemberInfo member, params MemberBinding\[\] bindings);

public static MemberMemberBinding MemberBind

(MethodInfo propertyAccessor,

IEnumerable&lt;MemberBinding&gt; bindings);

public static MemberMemberBinding MemberBind

(MethodInfo propertyAccessor,

params MemberBinding\[\] bindings);

public static MemberInitExpression MemberInit

(NewExpression newExpression,

params MemberBinding\[\] bindings);

public static MemberInitExpression MemberInit

(NewExpression newExpression,

IEnumerable&lt;MemberBinding&gt; bindings);

public static BinaryExpression Modulo

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Modulo(Expression left,

Expression right);

public static BinaryExpression ModuloAssign(Expression left,

Expression right);

public static BinaryExpression ModuloAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression ModuloAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression Multiply(Expression left,

Expression right);

public static BinaryExpression Multiply

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression MultiplyAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression MultiplyAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression MultiplyAssign

(Expression left, Expression right);

public static BinaryExpression MultiplyAssignChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression MultiplyAssignChecked

(Expression left, Expression right);

public static BinaryExpression MultiplyChecked

(Expression left, Expression right);

public static BinaryExpression MultiplyChecked

(Expression left, Expression right, MethodInfo method);

public static NamedArgumentInfo NamedArg(String name);

public static UnaryExpression Negate(Expression expression);

public static UnaryExpression Negate(Expression expression,

MethodInfo method);

public static UnaryExpression NegateChecked(Expression expression,

MethodInfo method);

public static UnaryExpression NegateChecked

(Expression expression);

public static NewExpression New

(ConstructorInfo constructor,

IEnumerable&lt;Expression&gt; arguments,

params MemberInfo\[\] members);

public static NewExpression New(ConstructorInfo constructor);

public static NewExpression New(Type type);

public static NewExpression New(ConstructorInfo constructor,

params Expression\[\] arguments);

public static NewExpression New

(ConstructorInfo constructor,

IEnumerable&lt;Expression&gt; arguments);

public static NewExpression New

(ConstructorInfo constructor,

IEnumerable&lt;Expression&gt; arguments,

IEnumerable&lt;System.Reflection.MemberInfo&gt; members);

public static NewArrayExpression NewArrayBounds

(Type type,

IEnumerable&lt;Expression&gt; bounds);

public static NewArrayExpression NewArrayBounds

(Type type, params Expression\[\] bounds);

public static NewArrayExpression NewArrayInit

(Type type,

IEnumerable&lt;Expression&gt; initializers);

public static NewArrayExpression NewArrayInit

(Type type, params Expression\[\] initializers);

public static UnaryExpression Not(Expression expression);

public static UnaryExpression Not(Expression expression,

MethodInfo method);

­­­­­

public static UnaryExpression OnesComplement

(Expression expression)

public static UnaryExpression OnesComplement

(Expression expression, MethodInfo method)

public static BinaryExpression Or

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Or(Expression left,

Expression right);

public static BinaryExpression OrAssign(Expression left,

Expression right);

public static BinaryExpression OrAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression OrAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression OrElse(Expression left,

Expression right);

public static BinaryExpression OrElse

(Expression left, Expression right, MethodInfo method);

public static ParameterExpression Parameter

(Type type, String name);

public static ParameterExpression Parameter(Type type)

public static ParameterExpression Variable

(Type type, String name);

public static ParameterExpression Variable(Type type)

public static PositionalArgumentInfo PositionalArg

(Int32 position);

public static UnaryExpression PostDecrementAssign

(Expression expression);

public static UnaryExpression PostDecrementAssign

(Expression expression, MethodInfo method);

public static UnaryExpression PostIncrementAssign

(Expression expression);

public static UnaryExpression PostIncrementAssign

(Expression expression, MethodInfo method);

public static BinaryExpression Power

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Power(Expression left,

Expression right);

public static BinaryExpression PowerAssign(Expression left,

Expression right);

public static BinaryExpression PowerAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression PowerAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static UnaryExpression PreDecrementAssign

(Expression expression);

public static UnaryExpression PreDecrementAssign

(Expression expression, MethodInfo method);

public static UnaryExpression PreIncrementAssign

(Expression expression, MethodInfo method);

public static UnaryExpression PreIncrementAssign

(Expression expression);

public static MemberExpression Property(Expression expression,

PropertyInfo property);

public static MemberExpression Property

(Expression expression, MethodInfo propertyAccessor);

public static IndexExpression Property

(Expression instance, PropertyInfo indexer,

IEnumerable&lt;Expression&gt; arguments);

public static IndexExpression Property

(Expression instance, PropertyInfo indexer,

params Expression\[\] arguments);

public static IndexExpression Property

(Expression instance, String propertyName,

params Expression\[\] arguments);

public static MemberExpression Property(Expression expression,

String propertyName);

public static MemberExpression Property

(Expression expression, Type type, String propertyName);

public static MemberExpression PropertyOrField

(Expression expression, String propertyOrFieldName);

public static UnaryExpression Quote(Expression expression);

public static UnaryExpression Rethrow();

public static UnaryExpression Rethrow(Type type);

public static GotoExpression Return(LabelTarget target,

Expression value);

public static GotoExpression Return(LabelTarget target);

public static BinaryExpression RightShift

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression RightShift(Expression left,

Expression right);

public static BinaryExpression RightShiftAssign(Expression left,

Expression right);

public static BinaryExpression RightShiftAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression RightShiftAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static RuntimeVariablesExpression RuntimeVariables

(IEnumerable&lt;ParameterExpression&gt;

variables);

public static RuntimeVariablesExpression RuntimeVariables

(params ParameterExpression\[\] variables);

public static BinaryExpression Subtract

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Subtract(Expression left,

Expression right);

public static BinaryExpression SubtractAssign(Expression left,

Expression right);

public static BinaryExpression SubtractAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression SubtractAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression SubtractAssignChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression SubtractAssignChecked

(Expression left, Expression right);

public static BinaryExpression SubtractChecked

(Expression left, Expression right);

public static BinaryExpression SubtractChecked

(Expression left, Expression right, MethodInfo method);

public static SwitchExpression Switch(Expression value,

params SwitchCase\[\] cases);

public static SwitchExpression Switch

(Expression switchValue, Expression defaultBody,

params SwitchCase\[\] cases)

public static SwitchExpression Switch

(Expression switchValue, Expression defaultBody,

MethodInfo comparison, params SwitchCase\[\] cases)

public static SwitchExpression Switch

(Type type, Expression switchValue, Expression defaultBody,

MethodInfo comparison, params SwitchCase\[\] cases)

public static SwitchExpression Switch

(Expression switchValue, Expression defaultBody,

MethodInfo comparison, IEnumerable&lt;SwitchCase&gt; cases)

public static SwitchExpression Switch

(Type type, Expression switchValue, Expression defaultBody,

MethodInfo comparison, IEnumerable&lt;SwitchCase&gt; cases)

public static SwitchCase SwitchCase

(Expression body, params Expression\[\] testValues)

public static SwitchCase SwitchCase

(Expression body, IEnumerable&lt;Expression&gt; testValues)

public static UnaryExpression Throw(Expression value, Type type);

public static UnaryExpression Throw(Expression value);

public static TryExpression TryCatch

(Expression body, params CatchBlock\[\] handlers);

public static TryExpression TryCatchFinally

(Expression body, Expression finally,

params CatchBlock\[\] handlers);

public static TryExpression TryFault(Expression body,

Expression fault);

public static TryExpression TryFinally(Expression body,

Expression finally);

public static TryExpression MakeTry

(Type type, Expression body, Expression finally,

Expression fault,

IEnumerable&lt;CatchBlock&gt; handlers);

public static UnaryExpression TypeAs(Expression expression,

Type type);

public static TypeBinaryExpression TypeEqual

(Expression expression, Type type);

public static TypeBinaryExpression TypeIs(Expression expression,

Type type);

public static UnaryExpression UnaryPlus(Expression expression);

public static UnaryExpression UnaryPlus(Expression expression,

MethodInfo method);

public static UnaryExpression Unbox(Expression expression,

Type type);

<h3 id="nodetype-property">4.3.2 NodeType Property</h3>

This property returns the kind of expression. The Expression node's type may be identified by a single ExpressionType value; for example, ConstantExpression always has the value Constant. Some Expression node types have many possible sub kinds; for example, BinaryExpression may have the Add or Multiply.

This is virtual for subclasses of Expression that can return their node kind without having to store it in a field. For example, BlockExpression can save a word of memory, but BinaryExpression needs a field to store one of many applicable node kinds. Derivations of Expression that are not in the common set of nodes in .NET should return node kind Extension.

The value returned by this property should never change for a given object.

Signature:

Public virtual ExpressionType NodeType { get; }

<h3 id="type-property">4.3.3 Type Property</h3>

With the call that unbound trees are truly lang-specific and can't be shared, IsBound is cut, and Type must never be null again.

This property returns the System.Type object representing the result type of the expression this Expression object represents. Type can be null when the node is unbound. When the node is dynamic, the type is not necessarily System.Object; it may have a known expected result type.

This is virtual for subclasses of Expression that can (or need to) return their node's Type by computing it. For example, a non-void BlockExpression can return the Type of its last expression. Not requiring a Type backing field saves a lot of memory usage in ETs.

The value returned by this property should never change for a given object.

Signature:

public Type Type { get; }

<h3 id="canreduce-property">4.3.4 CanReduce Property</h3>

This property returns whether the Reduce method returns a different but semantically equivalent ET. By default, this property returns false.

In the typical case, the resulting ET contains all common ET nodes suitable for passing to any common compilation or ET processing code. Sometimes the result is only partially reduced, and when walking the resulting ET, you'll need to further reduce some nodes.

The value returned by this property should never change for a given object.

Signature:

public virtual Boolean CanReduce { get; }

<h3 id="reduce-method">4.3.5 Reduce Method</h3>

This method returns a semantically equivalent ET representing the same expression. By default, this method returns the object on which it was invoked.

Typically the result comprises only common ET types, ET nodes suitable for passing to any compilation or ET processing code. Usually the result is only partially reduced (that is, only the root node). You'll probably need to further reduce some nodes.

Signature:

public virtual Expression Reduce();

<h3 id="reduceandcheck-method">4.3.6 ReduceAndCheck Method</h3>

This method returns a semantically equivalent ET representing the same expression by calling Reduce and then checking the result. The result is guaranteed to not be the same object and to have the same (or a reference assignable) Type property. If the result does not check positively, this method throws an exception.

Signature:

public virtual Expression ReduceAndCheck();

<h3 id="reduceextensions-method">4.3.7 ReduceExtensions Method</h3>

This method returns a semantically equivalent ET representing the same expression by calling Reduce repeatedly until the result is a common ET node. The result may be reducible (for example, if it is a BinaryExpression with node kind AddAssign or a ForEachExpression).

This is the standard way for compilers like the expression compiler to reduce nodes to the core set in .NET.

Signature:

public virtual Expression ReduceExtensions();

<h3 id="debugview-property">4.3.8 DebugView Property</h3>

Only available privately for debugging support in CLR 4.0, may be productized in v-next+1, but this is public in the codeplex sources.

This property walks the expression tree and renders "pretty printing" of the tree for debugging purposes. When there's more experience with what is maximally useful, and in what format customers would like to see the pretty printing, we could bake these into a future version of .NET.

Signatures:

private string DebugView {get {}}

<h3 id="dumpexpression-method-codeplex-only">4.3.9 DumpExpression Method (Codeplex only)</h3>

Only available on Codeplex.com builds, may be productized in v-next+1.

This method walks the expression tree and renders "pretty printing" of the tree for debugging purposes. When there's more experience with what is maximally useful, and in what format customers would like to see the pretty printing, we could bake these into a future version of .NET.

Signatures:

public void DumpExpression(string descr, TextWriter writer) {

<h3 id="getfunctype-method">4.3.10 GetFuncType Method</h3>

This helper method creates Type objects for delegate types with non-void return values. This method constructs the Func&lt;...&gt; types from the generic System.Linq.Func delegates using the supplied types.

Signature:

public static Type GetFuncType(params Type\[\] typeArgs);

TypeArgs must contain at least one argument and at most 17 elements. If the elements of typeArgs represent the types T1…Tn, the resulting Type object represents the constructed delegate type System.Linq.Func&lt;T1,…,Tn&gt;. The last argument must be the return type.

<h3 id="trygetfunctype-method">4.3.11 TryGetFuncType Method</h3>

This helper method is just like GetFuncType, but it does not throw if given more than 17 type arguments, returning False instead. It also returns false if there are any ByRef parameters.

Signature:

public static bool TryGetFuncType(Type\[\] typeArgs,

out Type funcType);

<h3 id="getactiontype-method">4.3.12 GetActionType Method</h3>

This helper method creates Type objects for delegate types with void return type. This method constructs the Action&lt;...&gt; types from the generic System.Linq.Action delegates using the supplied types.

Signature:

public static Type GetActionType(params Type\[\] typeArgs);

TypeArgs must contain at least one argument and at most 16 elements. If the elements of typeArgs represent the types T1…Tn, the resulting Type object represents the constructed delegate type System.Linq.Action&lt;T1,…,Tn&gt;.

<h3 id="trygetactiontype-method">4.3.13 TryGetActionType Method</h3>

This helper method is just like GetActionType, but it does not throw if given more than 16 type arguments, returning False instead. It also returns false if there are any ByRef parameters.

Signature:

public static bool TryGetActionType(Type\[\] typeArgs,

out Type actionType)

<h3 id="getdelegatetype-method">4.3.14 GetDelegateType Method</h3>

This helper method creates Type objects for delegate types. It determines whether to return a Func or Action based on whether the last argument, the return type, is void. If possible, this returns an instantiated Func or Action type, as GetFuncType or GetActionType would. If necessary, this generates a new delegate type.

If invoked on the same type sequence as seen previously in an App Domain, this may return the same delegate object returned the first time, but there is no guarantee on this behavior. In general, if all of your parameter and return types are built into mscorlib or system.core (and therefore known not to be collectible), then this method caches the returned delegate type. If caching is important to the performance of your compiler, you should implement your own caching (re-using the code on codeplex.com and removing our limitations if you like).

This method is useful when calling the Lambda factories, and it is what the Lambda factories call when you do not supply the delegate type.

Signature:

public static Type GetDelegateType(params Type\[\] typeArgs)

<h3 id="visitchildren-method">4.3.15 VisitChildren Method</h3>

This virtual method is for sub classes of Expression that are not common node kinds. These nodes should have node kind Extension, and they should override this method. If you derive from Expression and do not override this method, then your extension will not work well with visitors. The default VisitChildren will fully reduce your extension node which is sub optimal in meta-programming scenarios.

Signature:

protected virtual Expression VisitChildren

(ExpressionVisitor visitor);

<h3 id="accept-method">4.3.16 Accept Method</h3>

This method invokes the visitor's specific method for visiting nodes of this node's type; for example, MethodCallExpression overrides this to invoke ExpressionVisitor.VisitMethodCall. By default, this method calls ExpressionVisitor.VisitExtension.

It is rare to need to override this method, and it is available for subclasses only for marginal completeness. For example, if you had several customer node types and your own customer visitor, you could more directly dispatch to your visitor's VisitMumble methods for each Mumble extension node type.

Signature:

protected internal virtual Expression Accept

(ExpressionVisitor visitor)

<h2 id="expressiontype-enum">4.4 ExpressionType Enum</h2>

This enum has value to mark the kind of node an ET node is. Some of the ET node types are re-used for several expressions due to their "shape". For example, BinaryExpression has certain properties common to expressions such as addition, multiplication, and even assignment. When re-using BinaryExpression, the factory methods use unique values from this enum to distinguish the operation.

The following sub sections' text that describes the static node semantics for the enum members comes from the v1 spec ... it may have been edited for clarification.

<h3 id="type-summary">4.4.1 Type Summary</h3>

public enum ExpressionType {

Add,

AddChecked,

And,

AndAlso,

ArrayLength,

ArrayIndex,

Call,

Coalesce,

Conditional,

Constant,

Convert,

ConvertChecked,

Divide,

Equal,

ExclusiveOr,

GreaterThan,

GreaterThanOrEqual,

Invoke,

Lambda,

LeftShift,

LessThan,

LessThanOrEqual,

ListInit,

MemberAccess,

MemberInit,

Modulo,

Multiply,

MultiplyChecked,

Negate,

UnaryPlus,

NegateChecked,

New,

NewArrayInit,

NewArrayBounds,

Not,

NotEqual,

Or,

OrElse,

Parameter,

Power,

Quote,

RightShift,

Subtract,

SubtractChecked,

TypeAs,

TypeIs,

Assign,

Block,

DebugInfo,

Decrement,

Dynamic,

Default,

Extension,

Goto,

Increment,

Index,

Label,

RuntimeVariables,

Loop,

Switch,

Throw,

Try,

Unbox,

AddAssign,

AndAssign,

DivideAssign,

ExclusiveOrAssign,

LeftShiftAssign,

ModuloAssign,

MultiplyAssign,

OrAssign,

PowerAssign,

RightShiftAssign,

SubtractAssign,

AddAssignChecked,

MultiplyAssignChecked,

SubtractAssignChecked,

PreIncrementAssign,

PreDecrementAssign,

PostIncrementAssign,

PostDecrementAssign,

TypeEqual,

OnesComplement,

IsTrue,

IsFalse

<h3 id="add">4.4.2 Add</h3>

Use Add in BinaryExpression to represent arithmetic addition without overflow checking. Given an Add node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is (e1) + (e2), including order of evaluation.

Use this in a DynamicExpression to represent a binary operator for asking the first object to add the second object to itself. Neither object is modified.

<h3 id="addchecked">4.4.3 AddChecked</h3>

Use AddChecked in BinaryExpresion to represent arithmetic addition with overflow checking. Given an AddChecked node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is checked(unchecked(e1) + unchecked(e2)), including order of evaluation.

There is no use for this node kind in DynamicExpressions.

<h3 id="and">4.4.4 And</h3>

Use And in BinaryExpression to represent a bitwise or logical And operation. The semantics depends on whether your Left and Right expression are ints or bool. In the case where the operands are of type bool?, three-valued logic applies:

| **AND** | False | null  | True  |     | **OR** | False | null | True |
|---------|-------|-------|-------|-----|--------|-------|------|------|
| False   | False | False | False |     | False  | False | null | True |
| null    | False | null  | null  |     | null   | null  | null | True |
| True    | False | null  | True  |     | True   | True  | True | True |

Order of evaluation is Left then Right.

Use this in a DynamicExpression to represent a binary operator for asking the first object to perform a bitwise AND of its contents with the second object. Neither object is modified. Logical AND is "open coded" in ETs for operations on IDynamicMetaObjectProviders for more control over what a "true" or "false" value is.

<h3 id="andalso">4.4.5 AndAlso</h3>

Use AndAlso in BinaryExpression to represent a conditional And operator, evaluating the right operand only if necessary. Given an AndAlso node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is

> ((e1) == false) ? false : (e1) & (e2)

except that e1 is evaluated only once, and e2 may not be evaluated at all. Three valued logic applies to the AndAlso operator over nullable Booleans (see And enum member). In the case where both operands are non-nullable, the semantics is equivalent to (e1) && (e2).

There are some additional complexities with AndAlso/OrElse, in particular combining overloaded operators and nullables. Here is the comment from BinaryExpression.cs:

``` csharp
// For a userdefined type T which has op_False defined and L, R are
// nullable, (L AndAlso R) is computed as:
//
// L.HasValue
//     ? T.op_False(L.GetValueOrDefault())
//         ? L
//         : R.HasValue 
//             ? (T?)(T.op_BitwiseAnd(L.GetValueOrDefault(), 
//                                    R.GetValueOrDefault()))
//             : null
//     : null
```

There is no use for this node kind in DynamicExpressions since both operands would always be evaluated before passing them to the dynamic call site. However, you can produce a dynamic AndAlso by combining IsFalse and And with pseudo-code like the following (ignore mulitiple evaluations of sub expressions):

> DynamicExpr(IsFalse, e1) ? false : Dynamic(And, e1, e2)

<h3 id="arraylength">4.4.6 ArrayLength</h3>

Use ArrayLength with UnaryExpression to represent taking the length of a one-dimensional array.

There is no use for this node kind in DynamicExpressions.

<h3 id="arrayindex">4.4.7 ArrayIndex</h3>

The ArrayIndex node kind and Expression factories will be obsolete in lieu the more general IndexExpression node.

Use ArrayIndex in BinaryExpression to represent indexing into a one-diemnsional array. Given an ArrayIndex node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is (e1)\[e2\].

There is no use for this node kind in DynamicExpressions since a GetIndexBinder represents this operation.

<h3 id="call">4.4.8 Call</h3>

MethodCallExpressions use the Call node kind. It represents calling a method. Given a Call node, exp, let e0 be the C\# equivalent of exp.object, let e1…en be the comma-separated list of C\# expressions equivalent to the corresponding nodes in exp.Arguments and let m be the C\# identifier denoting exp.Method. Furthermore let r1…rn be ref, out, or empty in accordance with possible modifiers on the corresponding parameter in exp.Method. Then the C\# equivalent of exp is

> (e0).m(r1 e1…rn en)

If Method denotes a static method, with T being the C\# equivalent of the type it belongs to, the C\# equivalent of exp is

> T.m(r1 e1…rn en)

In the case of ref or out parameters on non-addressable expressions, the semantics is to create a local variable holding the value of the expression, passing that local instead of the expression itself as an argument to the call.

There is no use for this node kind in DynamicExpressions because they use InvokeMemberBinder objects to indicate the semantics, and they don't use node kinds.

<h3 id="coalesce">4.4.9 Coalesce</h3>

Use Coalesce in BinaryExpression to represent a null coalescing expression. This operation is one that evaluates its first operand and conditionally evaluates any successive operands, returning the value of the first operand that has a non-null result. Given a Coalesce node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then exp is similar to the C\# expression, (e1) ?? (e2), including order of evaluation.

The difference in semantics is that a Coalesce node kind has a conversion lambda you can specify that executes last and converts either e1 or e2 to the node's Type. This conversion aspect is required in the node's semantics so that languages or ET producers can specify the exact overloaded conversion method or a custom method.

There is no use for this node kind in DynamicExpressions since dynamic languages typically perform these semantics naturally with their 'or' operator. If they don’t, then they can trivially open code the semantics with dynamic objects to appropriately avoid evaluating some operands.

<h3 id="conditional">4.4.10 Conditional</h3>

ConditionalExpressions use the Conditional node kind. It represents an if-then-else for value. Given a Conditional node created without a specified Type property (call the node "exp"), let e1, e2, and e3 be the C\# equivalent of exp.Test, exp.IfTrue, and exp.IfFalse, respectively. Then the C\# equivalent of exp is (e1) ? (e2) : (e3), including order of evaluation.

If you do supply the Type property when constructing the node, and it is void, then the sub expression types do not have to match, and any resulting value is "converted to void" or squelched. If you specify Type explicitly, then e2 and e3 must be reference-assignable to the type represented by the node's Type property.

There is no use for this node kind in DynamicExpressions since dynamic languages typically perform these semantics naturally with their 'if-then-else' or 'and'/'or' operators. If they don’t, then they can trivially open code the semantics with dynamic objects to appropriately avoid evaluating some operands.

<h3 id="constant">4.4.11 Constant</h3>

Constant is the node kind for a ConstantExpression node. It represents an expression that has a constant value. A Constant node, exp, may have any exp.Value, and the value may not have any syntactic representation in any programming language. The result of evaluating exp is exp.Value.

There is no use for this node kind in DynamicExpression.

<h3 id="convert">4.4.12 Convert</h3>

Use Convert in a UnaryExpression node to represent an explicit numeric or enumeration conversion. The semantics is to silently overflow if the converted value does not fit the target type. Given a Convert node, exp, let e be the C\# equivalent of exp.Operand, and let T be a type expression in C\# for the type represented by exp.Type. The C\# equivalent of exp is "(T)(e)". T is static compile-time information, so it is not evaluated at run time.

There is no use for this node kind in DynamicExpressions since a ConvertBinder represents this operation.

<h3 id="convertchecked">4.4.13 ConvertChecked</h3>

Use ConvertChecked in a UnaryExpression node to represent an explicit numeric or enumeration conversion that throws an exception if the converted value does not fit the target type. Given a ConvertChecked node, exp, let e be the C\# equivalent of exp.Operand, and let T be a type expression in C\# for the type represented by exp.Type. The C\# equivalent of exp is "checked ((T)unchecked(e))". T is static compile-time information, so it is not evaluated at run time.

There is no use for this node kind in DynamicExpressions since a ConvertBinder represents this operation.

<h3 id="divide">4.4.14 Divide</h3>

Use Divide in a BinaryExpression node to represent arithmetic division. Given a Divide node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is (e1) / (e2), including evaluation order. This is equivalent to the IL 'div' instruction (truncating integer division), so for example, -10/3 is -3.

Use this in a DynamicExpression to represent a binary operator for asking the first object to divide itself by the second object. Neither object is modified. If the objects are numbers, the expectation is that the result is an integer, rational, or floating point number.

<h3 id="equal">4.4.15 Equal</h3>

Use Equal in BinaryExpression nodes to represent a comparison for equality. Given an Equal node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# expression, (e1) == (e2), including order of evaluation is similar to the Equal node kind. The only difference is that reference types always compare a pointer equality.

Use this in a DynamicExpression to ask the first object to return whether it is equal to the second object. The equality test is a deep structural equality, not object identity or first level aggregation equality. The expectation is that this operator is a comparison returning a Boolean value, but the DLR does not enforce that.

<h3 id="exclusiveor">4.4.16 ExclusiveOr</h3>

Use ExclusiveOr in BinaryExpression nodes to represent a bitwise xor operation. Given an ExclusiveOr node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is (e1) ^ (e2), including evaluation order.

Use this in a DynamicExpression to represent a binary operator for asking the first object to perform a bitwise XOR of its contents with the second object. Neither object is modified.

<h3 id="greaterthan">4.4.17 GreaterThan</h3>

Use GreaterThan in BinaryExpression nodes to represent a numeric comparison. Given a GreaterThan node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is (e1) &gt; (e2), including evaluation order.

Use this in DynamicExpression to ask the first object to return whether it is greater than the second object. The expectation is that this operator is a comparison returning a Boolean value, but it could be a composition, I/O, or any kind of operator returning any type of value.

<h3 id="greaterthanorequal">4.4.18 GreaterThanOrEqual</h3>

Use GreaterThanOrEqual in BinaryExpression nodes to represent a numeric comparison. Given a GreaterThanOrEqual node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is (e1) &gt;= (e2), including evaluation order.

Use this in DynamicExpression to ask the first object to return whether it is greater than or equal to the second object. The equality test is a deep structural equality, not object identity or first level aggregation equality. The expectation is that this operator is a comparison returning a Boolean value, but it could be a composition, I/O, or any kind of operator returning any type of value.

<h3 id="invoke">4.4.19 Invoke</h3>

InvocationExpression nodes use this node kind. It represents invoking a delegate or lambda expression on a list of argument expressions. Given an Invoke node, exp, let e0 be the C\# equivalent of exp.Expression, and let e1…en be the comma-separated list of C\# expressions equivalent to the corresponding nodes in exp.Arguments. Then the C\# equivalent of exp is (e0)(e1…en), including evaluation order.

If e0 evaluates to a value of type Expression&lt;T&gt;, the DLR compiles the lambda and then invokes it. C\# does not allow this, but ETs do.

There is no use for this node kind in DynamicExpressions because they use InvokeBinder objects to indicate the semantics, and they don't use node kinds.

<h3 id="lambda">4.4.20 Lambda</h3>

LambdaExpressions use the Lambda node kind. They represent a lambda expression with a delegate type. Given a Lambda node, exp, let e be the C\# equivalent of exp.Body, and let p1…pn be the comma separated list of C\# parameters corresponding to each of the elements in exp.Parameters. Finally, let T be a type expression in C\# for the type represented by exp.Type. Then the C\# equivalent of exp is primarily "((T)((p1…pn) =&gt; e))", but there are some flags and features ET Lambda nodes support that C\# does not.

There is no use for this node kind in DynamicExpressions.

<h3 id="leftshift">4.4.21 LeftShift</h3>

Use LeftShift in BinaryExpression nodes to represent a bitwise left shift operation. Given a LeftShift node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) &lt;&lt; (e2)", including evaluation order.

Use this node kind in DynamicExpressions to ask the first object to shift its contents left by the number of positions indicated by the second object. Neither object is modified. Any vacant locations created on the right side of object one are filled by a default value appropriate to the first object and language that owns the object and shift semantics.

<h3 id="lessthan">4.4.22 LessThan</h3>

Use LessThan in BinaryExpression nodes to represent a numeric comparison. Given a LessThan node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) &lt; (e2)", including evaluation order.

Use this in DynamicExpression to ask the first object to return whether it is less than the second object. The expectation is that this operator is a comparison returning a Boolean value, but it could be a composition, I/O, or any kind of operator returning any type of value.

<h3 id="lessthanorequal">4.4.23 LessThanOrEqual</h3>

Use LessThanOrEqual in BinaryExpression nodes to represent a numeric comparison. Given a LessThanOrEqual node exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) &lt;= (e2)", including evaluation order.

Use this in DynamicExpression to ask the first object to return whether it is less than or equal to the second object. The expectation is that this operator is a comparison returning a Boolean value, but it could be a composition, I/O, or any kind of operator returning any type of value.

<h3 id="listinit">4.4.24 ListInit</h3>

The ListInitExpression uses the ListInit node kind to represent creating a new collection object and initializing it from a list of elements. Given a ListInit node, exp, let c be the C\# equivalent of exp.NewExpression, and let e1…en be the comma-separated list of C\# expressions equivalent to the corresponding nodes in exp.Expressions. Then the C\# equivalent of exp is "c{e1…en}"

There is no use for this node kind in DynamicExpressions since a CreateInstanceBinder represents this operation, combined with other expressions.

<h3 id="memberaccess">4.4.25 MemberAccess</h3>

A MemberExpression node uses MemberAccess node kind. It represents reading from a field or property, but as the Left expression of a BinaryExpression node with node kind Assign, this node kind represents a storable location or l-value. Given a MemberAccess node, exp, let e be the C\# equivalent of exp.expression, and m be the C\# identifier denoting exp.Member. Then the C\# equivalent of exp is "(e).m". If Member denotes a static member, with T being the C\# equivalent of the type it belongs to, the C\# equivalent of exp is "T.m".

<h3 id="memberinit">4.4.26 MemberInit</h3>

MemberInitExpression uses the MemberInit node kind. It represents creating a new object and initializing some of its members. Given a MemberInit node, exp, let c be the C\# equivalent of exp.NewExpression, and let b1…bn be the comma-separated list of C\# bindings equivalent to the corresponding nodes in exp.Bindings. Then the C\# equivalent of exp is c{b1…bn}, including evaluation order.

There is no use for this node kind in DynamicExpressions since a CreateInstanceBinder represents this operation, combined with other expressions.

<h3 id="modulo">4.4.27 Modulo</h3>

Use Modulo in BinaryExpression nodes to represent computing an arithmetic remainder. Given a Modulo node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is (e1) % (e2). This is equivalent to the IL 'rem' instruction, so for example, -10 mod 3 is -1, and 10 mod -3 is 1.

Use this node kind in Dynamic Expression to ask the first object to divide itself by the second object. If the objects are numbers, the expectation is that the result is an integer remainder resulting from TruncateDivide'ing the first object by the second. Neither object is modified.

<h3 id="multiply">4.4.28 Multiply</h3>

Use Multiply in BinaryExpression nodes to represent arithmetic multiplication without overflow checking. Given a Multiply node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) \* (e2)".

Use this in a DynamicExpression to represent a binary operator for asking the first object to multiply itself by the second object. Neither object is modified. If the objects are numbers, the expectation is that the result is a number.

<h3 id="mulitplychecked">4.4.29 MulitplyChecked</h3>

Use MultiplyChecked in BinaryExpression nodes to represent arithmetic multiplication with overflow checking. Given a MultiplyChecked node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "checked(unchecked(e1) \* unchecked(e2))".

There is no use for this node kind in DynamicExpressions.

<h3 id="negate">4.4.30 Negate</h3>

Use Negate in UnaryExpression nodes to represent arithmetic negation. Given a Negate node, exp, let e be the C\# equivalent of exp.Operand. Then the C\# equivalent of exp is "-(e)".

Use this in DynamicExpressions to request an object to produce its negated value.

<h3 id="unaryplus">4.4.31 UnaryPlus</h3>

Use UnaryPlus in a UnaryExpression node to represent keeping the sign for a numeric argument, which has no effect. In general this node kind is a no-op, but .NET does allow user-defined implementations for user types, which can do anything. Given a UnaryPlus node, exp, let e be the C\# equivalent of exp.Operand. Then the C\# equivalent of exp is "+(e)".

Use this in DynamicExpressions to request an object to produce its value with the same sign or whatever the object defines that it does for this operation.

<h3 id="negatechecked">4.4.32 NegateChecked</h3>

Use NegateChecked in a UnaryExpression node to represent arithmetic negation that has overflow checking.

There is no use for this in DynamicExpressions.

<h3 id="new">4.4.33 New</h3>

The NewExpression uses the New node kind. It represents calling a constructor to create a new object. Given a New node, exp, let T be the C\# name of the declaring type of exp.Constructor, and let e1…en be the comma-separated list of C\# expressions equivalent to the corresponding nodes in exp.Arguments. Then the C\# equivalent of exp is "new T(e1…en)".

There is no use for this node kind in DynamicExpressions since a CreateInstanceBinder represents this operation, combined with other expressions possibly.

<h3 id="newarrayinit">4.4.34 NewArrayInit</h3>

The NewArrayExpression node uses the NewArrayInit node kind. It represents creating a new one-dimensional array by specifying a list of elements. Given a NewArrayInit node, exp, let T denote the C\# name for the element type of the array type represented by exp.Type, and let e1…en be the comma-separated list of C\# expressions equivalent to the corresponding nodes in exp.Expressions. Then the C\# equivalent of exp is "new T\[\]{e1…en}".

There is no use for this with DynamicExpression .

<h3 id="newarraybounds">4.4.35 NewArrayBounds</h3>

Use NewArrayBounds in a NewArrayExpression node to represent creating a new array by specifying its bounds for each dimension. Given a NewArrayBounds node, exp, let T denote the C\# name for the element type of the array type represented by exp.Type, and let e1…en be the comma-separated list of C\# expressions equivalent to the corresponding nodes in exp.Expressions. Then the C\# equivalent of exp is "new T\[e1…en\]".

There is no use for this node kind in DynamicExpression.

<h3 id="not">4.4.36 Not</h3>

Use the Not node kind in UnaryExpression nodes to represent bitwise complement or logical negation. Given a Not node, exp, let e be the C\# equivalent of exp.Operand. If e has integral type, the C\# equivalent of exp is "\~(e)". If e has type bool, the C\# equivalent of exp is "!(e)".

Use this in DynamicExpressions to ask an object to return its logical negation. Use OnesComplement in a DynamicExpression to ask an object to returns its bitwise negation.

<h3 id="notequal">4.4.37 NotEqual</h3>

Use NotEqual in BinaryExpressions to represent a comparison for operands to not be equal. Given a NotEqual node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) != (e2)".

Use this in DynamicExpressions to ask an object to return whether it is not equal to the second object.

<h3 id="or">4.4.38 Or</h3>

Use Or in BinaryExpression nodes to represent a bitwise or logical Or operation. Given an Or node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) \| (e2)".

In the case where the operands are of type bool?, three-valued logic applies:

| **AND** | False | null  | True  |     | **OR** | False | null | True |
|---------|-------|-------|-------|-----|--------|-------|------|------|
| False   | False | False | False |     | False  | False | null | True |
| null    | False | null  | null  |     | null   | null  | null | True |
| True    | False | null  | True  |     | True   | True  | True | True |

Use this in a DynamicExpression to represent a binary operator for asking the first object to perform a bitwise OR of its contents with the second object. Neither object is modified. Logical OR is "open coded" in ETs for operations on IDynamicMetaObjectProviders for more control over what a "true" or "false" value is.

<h3 id="orelse">4.4.39 OrElse</h3>

Use OrElse in a BinaryExpression node to represent a conditional or operator, evaluating the right operand only if necessary. Given an OrElse node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "((e1) == true) ? true : (e1) \| (e2)", except that e1 evaluates only once, and e2 may not evaluate at all.

Three valued logic applies to the OrElse operator over nullable booleans. In the case where both operands are non-nullable, the semantics is equivalent to "(e1) \|\| (e2)".

There are some additional complexities with AndAlso/OrElse, in particular combining overloaded operators and nullables. Here is the comment from BinaryExpression.cs:

``` csharp
// For a userdefined type T which has op_True defined and L, R are
// nullable, (L OrElse R)  is computed as:
//
// L.HasValue
//     ? T.op_True(L.GetValueOrDefault())
//         ? L
//         : R.HasValue 
//             ? (T?)(T.op_BitwiseOr(L.GetValueOrDefault(), 
//                                   R.GetValueOrDefault()))
//             : null
//     : null
//
// This is the same behavior as VB. It's combining the normal pattern 
// for short-circuiting operators, with the normal pattern for lifted 
// operations: if either of the operands is null, the result is also 
// null.
```

There is no use for this node kind in DynamicExpressions since both operands would always be evaluated before passing them to the dynamic call site.

<h3 id="parameter">4.4.40 Parameter</h3>

The ParameterExpression uses the Parameter node kind. It represents a reference to a variable via an identifier defined in the containing context.

Variables must be listed using ParameterExpressions as parameters for LambdaExpression or as lexical variables in a BlockExpression to (in effect) define them in some sub tree. To reference a variable, you alias the ParameterExpression object used to define the variable. Note, while Parameter node references are what determine variable binding, you can declare the same Parameter object in nested BlockExpressions. The ET compiler resovles the references to the tightest containing Block that declares the Parameter.

The name on Parameter is purely for debugging or pretty printing purposes and has no semantics to it.

There is no use for this node kind in DynamicExpressions themselves. Of course, when binders produce implementing expressions for dynamic operations, they may use ParameterExpressions.

<h3 id="power">4.4.41 Power</h3>

Use the Power node kind in BinaryExpression nodes to represent an exponentiation operation. The semantics is to invoke the System.Math.Pow function on the operands.

Use this in DynamicExpressions to request that the first object raise itself to the power of the second object. For numeric objects, you should expect a numeric result, ignoring NaN and overflow issues. Otherwise, the semantics is whatever the object implements. Neither object should be modified.

<h3 id="quote">4.4.42 Quote</h3>

Use Quote in UnaryExpressions to represents an expression that has a "constant" value of type Expression. Unlike a Constant node, the Quote node specially handles contained ParameterExpression nodes. If a contained ParameterExpression node declares a local that would be closed over in the resulting expression, then Quote replaces the ParameterExpression in its reference locations. At run time when the Quote node is evaluated, it substitutes the closure variable references for the ParameterExpression reference nodes, and then returns the quoted expression.

It is rare that an ET producer would need to use this factory or node kind. It exists with special semantics for use in LINQ v1 language features (see below). In v-next+1 we'll consider a complete and explicit quasi-quoting model.

There is no use for Quote in DynamicExpressions.

Here's an example that works:

``` csharp
ParameterExpression x = Expression.Parameter(typeof(int), "x");
ParameterExpression y = Expression.Parameter(typeof(int), "y");
Func<int, Expression> f = Expression.Lambda<Func<int, Expression>>(
    Expression.Quote(Expression.Lambda<Func<int, int>>(
        // This x is a reference from the inner lambda to the declared x in
        // the outer lambda, but y is declared and referenced inside Quote.
        Expression.Add(x, y), y)),
    // This use of the ParameterExpression declares the outer lambda's x.
    x).Compile();
Func<int, int> z = ((Expression<Func<int, int>>)f(123)).Compile();
int a = z(111);
int b = z(222);
int c = z(333);
Console.WriteLine("a: {0}, b: {1}, c: {2}", a, b, c);
// a: 234, b: 345, c: 456
```

Note, the next two examples throw errors in ETs v2 in the Quote factory since it only accepts nodes with Type Expession&lt;T&gt; (that is, lambdas).

Here's an example from v1 that does NOT work as you might expect:

``` csharp
ParameterExpression x = Expression.Parameter(typeof(int), "x");
Func<int, Expression> f 
   = Expression.Lambda<Func<int, Expression>>(Expression.Quote(x), x)
        .Compile();
Expression a = f(123);
Debug.Assert(a == x);   // This assert is true but unexpected.
```

In this case, Quote returned the ParameterExpression object (hence, a == x) since the variable didn't appear to come from a closed over reference.

Here's another example from v1 that does NOT work as you might expect:

``` csharp
ParameterExpression x = Expression.Parameter(typeof(int), "x");
Func<int, Expression> f = Expression.Lambda<Func<int, Expression>>(
    Expression.Invoke(Expression.Lambda<Func<Expression>>(Expression
                                                             .Quote(x))),
    // Here x is declared in an outer lambda (and closed over), BUT
    // Quote tries to cast its result to the node type (not its Type 
    // property), assuming it is a lambda/delegate type.
    x
).Compile();
// Throws InvalidCastException: Unable to cast object of type
// 'MemberExpression' to type
// 'ParameterExpression'.
Expression a = f(123);
```

LINQ tries to convert the quoted expression as its actual type in case it gets passed to a function, for example, but the actual type becomes the closure variable reference, which is a MemberExpression.

<h3 id="rightshift">4.4.43 RightShift</h3>

Use RightShift in BianryExpression nodes to represent a bitwise right shift operation. Given a RightShift node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) &gt;&gt; (e2)".

Use this node kind in DynamicExpressions to ask the first object to shift its contents right by the number of positions indicated by the second object. Neither object is modified. Any vacant locations created on the left side of object one are filled by a default value appropriate to the first object and language that owns the object and shift semantics.

<h3 id="subtract">4.4.44 Subtract</h3>

Use Subtract in BinaryExpression nodes to represent arithmetic subtraction without overflow checking. Given a Subtract node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "(e1) - (e2)".

Use this in a DynamicExpression to represent a binary operator for asking the first object to add the second object to itself. Neither object is modified.

<h3 id="subtrtactchecked">4.4.45 SubtrtactChecked</h3>

Use SubtractChecked in a BinaryExpression node to represent arithmetic subtraction with overflow checking. Given a SubtractChecked node, exp, let e1 and e2 be the C\# equivalent of exp.Left and exp.Right. Then the C\# equivalent of exp is "checked(unchecked(e1) - unchecked(e2))".

There is no use for this node kind in DynamicExpressions.

<h3 id="typeas">4.4.46 TypeAs</h3>

Use TypeAs in a UnaryExpression node to represent an explicit reference or boxing conversion supplying the null value if the conversion fails. Given a TypeAs node, exp, let e be the C\# equivalent of exp.Operand, and let T be a type expression in C\# for the type represented by exp.Type. If T is a reference type or nullable type, the C\# equivalent of exp is "(e) as T".

There is no use for this in DynamicExpressions. They have a Convert protocol, but due to how dynamic operations work, the invoking language has control over whether a null or an exception comes from the conversation in exceptional situations.

<h3 id="typeis">4.4.47 TypeIs</h3>

TypeBinaryExpressions usesTypeIs to represent a sub type test. The semantics is the same as the CLR's IsInst instruction, which is very close to the C\# 'is' operator.

<h3 id="typeequal">4.4.48 TypeEqual</h3>

TypeBinaryExpressions uses TypeEqual to represent testing whether an object exactly has the specified type. The C\# similar expression would be "x.GetType() == T".

<h3 id="assign">4.4.49 Assign</h3>

Use Assign in BianryExpression nodes to represent evaluating the right expression and assigning it to a location indicated by the left expression. The left expression must be one of the node types ParameterExpression, MemberExpression, or IndexExpression. The result is the value of the right expression, but it has the type of the left expression.

The order of evaluation is first any sub expressions in the left expression followed by the right expression. For example, if the left expression is “e1.foo” or “e1\[e2, e3\]”, then e1 is evaluated first in both cases, then e2 and e3 in the second case. The root semantics of the left expression (member fetch, index, variable fetch) are not evaluated but instead used to emit instructions for storing into the specified location.

There is no use for Assign with DynamicExpressions since the left would always be evaluated to produce an object. The DynamicMetaObject protocol has operations for setting members and indexed locations of dynamic objects.

<h3 id="block">4.4.50 Block</h3>

The BlockExpression uses the Block node kind. It represents a sequence of expressions. The semantics are to execute each expression in order, squelching the result of each except the last expression. The value of the Block node is the result of the last expression in the body.

Each block also has a collection, possibly empty, of variables whose scope is the body of the block. Ignoring closures, the variables' lifetimes are limited to the block. There is no initialization of the variables beyond any guarantees of .NET. If you care about definite assignment or order of initialization, then the first expressions in the block should assign the variables. Your compiler should emit any errors in your language should the program not adhere to your language's semantics in these regards.

Some languages have strong lexical semantics for unique binding of variables. For example, in Common Lisp or Scheme, each iteration around a loop or any basic block of code creates unique bindings for variables introduced in those basic blocks. Thus, returning a lambda would create unique closures environments for each iteration. Some languages, such as Python and F\#, move all variables to the scope of their containing function. ETs v2 supports both models, depending on where you create the BlockExpression in the ET and list variables.

For the stronger lexical model, for example with the loop, place the BlockExpression inside the loop body and list variables there. Putting the Block Expression outside the loop or at the start of the function ensures the variables have one binding across iterations of the loop. These effects are usually only observable when closing over lexical environments, and ETs v2 guarantees the unique binding semantics per loop iteration in this case. If you do not close over any of the Block's variables, and you do not explicitly assign to them before referencing them, then the behavior or observability of unique bindings is undefined. See section for an example.

**Post CLR 4.0 (remove IsByRef error check):** If any of the ParameterExpressions representing the local variables has IsByRef set to True, then you must initialize them with an AssignRef BinaryExpression node. Otherwise, if the Body of the Block references the variable, or the variable appears as the Left expression of an Assign BinaryExpression node, there will be compile time error (or possibly an error when the code executes).

There is no use for this node kind in DynamicExpressions themselves.

<h3 id="debuginfo">4.4.51 DebugInfo</h3>

DebugInfoExpression use this node kind. It represents a point in an ET where there is debugging information (a la .NET sequence points). To clear the sequence point information, use a DebugInfo node with IsClear set to True.

<h3 id="decrement">4.4.52 Decrement</h3>

Use Decrement in UnaryExpression nodes to represent functional decrement of the operand expression by one unit. The operand should not be modified by the operation. The methodinfo represents the implementing method for subtracting one unit from the operand. If the methodinfo is null, and the operand is numeric, the semantics is to subtract one.

In DynamicExpressions the expected semantics are to ask the object to return a unit decremented value of itself. The object is not modified.

<h3 id="dynamic">4.4.53 Dynamic</h3>

The DynamicExpression node uses this node kind. It represents an operation that must be bound at runtime of the expression tree. The semantics is to create a DLR CallSite for caching of implementations of the operation given the different kinds of objects passed to the CallSite during the program's execution. The Dynamic node has a DLR CallSiteBinder that determines the exact semantics of the operation given the runtime operands. The binder encapsulates the language semantics for the creator of the node, as well as any payload meta data that informs the binder how to compute an implementation of the operation at runtime.

For more on expected semantics, see other node kinds' statements as to their semantics in DynamicExpression nodes. You might also see the sub classes of DynamicMetaDataBinder and language documentation on their dynamic semantics. See the documents at on the [DLR Codeplex](http://www.codeplex.com/dlr/Wiki/View.aspx?title=Docs%20and%20specs&referringTitle=Home) site, sites-binders-dynobj-interop.doc and library-authors-introduction.doc.

<h3 id="default">4.4.54 Default</h3>

DefaultExpressions use this node kind. If DefaultExpression.Type represents the void type, the node represents a no-op empty expression. If Type is some other type, then node represents a constant returning default(T) for the type.

<h3 id="extension">4.4.55 Extension</h3>

Use the Extension node kind for any node type that is a user-derived type of Expression. You should not use the common node kinds for

<h3 id="goto">4.4.56 Goto</h3>

GotoExpressions use this node kind. It represents an unstructured flow of control to a labeled location in the ET. The Goto refers to a LabelTarget that a LabelExpression must refer to somewhere in the ET, and it is the LabelExpression that sets the target location for the flow of control. The LabelExpression must be lexically in the same LambdaExpression.Body as the GotoExpression. There is no non-local exit support in ETs v2.

The semantics of the LabelTarget chosen as the destination is lexically scoped in a sense. If all LabelTargets in the LambdaExpression are unique, then the LabelTarget in the GotoExpression simply must be found within the LambdaExpression containing the Goto. If the same LabelTarget is used multiple times within a LambdaExpression, then the GotoExpression targets the first matching LabelTarget found while searching up the ET to the Lambda root. This is a convenience for re-writers or tree builders that re-use sub trees that contain LabelExpressions and GotoExpressions so that the sub trees behave as expected unto themselves.

The Goto can optionally deliver a value to the location, as expressed by a non-null Expression property. If this property is non-null, then the expression Type property must represent a type that is reference assignable to the type represented by Target.Type. However, if Target.Type is void, the GotoExpression.Expression.Type can represent anything since the ET compiler will automatically convert the result to void or squelch the value.

We limit Goto lexically within a function. In addition to the simple ability to jump from inner basic blocks to outer basic blocks, we allow jumping into the following:

- BlockExpressions

- ConditionalExpressions

- LoopExpressions

- SwitchExpressions

- TryExpression body from a contained CatchBlock

We don't support jumping from the outside into the following possibly surprising basic blocks (you can't jump into various other expressions such as arguments, which probably aren't surprising):

- TryExpressions

- CatchBlocks

- Finally blocks

We don't support jumping from (that is, leaving) the following:

- finally blocks

- LambdaExpressions

- GeneratorExpressions

~~CUT Beta2 BUG SEMANTICS: ETs v2 limit Goto lexically within a function. ETs allow jumping into and out of the following:~~

- ~~BlockExpressions~~

- ~~ConditionalExpressions~~

- ~~LoopExpressions~~

- ~~SwitchExpressions~~

- ~~TryExpressions (under certain situations)~~

- ~~LabelExpressions~~

~~ETs allow some jumps relative to TryExpressions:~~

- ~~jumping out of TryExpression’s body~~

- ~~jumping out of a CatchBlock’s body~~

- ~~jumping into TryExpression’s body from one of its own CatchBlocks~~

~~The above constitutes what ETs v2 allow. Just by way of examples, we do not allow jumping into the middle of argument expressions, such as those in binary operands, method calls, invocations, indexing, instance creation, etc. We do not allow jumping into or out of GeneratorExpressions. You could however jump within a BlockExpression used as an argument expression.~~

<h3 id="increment">4.4.57 Increment</h3>

Use Increment in UnaryExpression nodes to represent functional increment of the operand expression by one unit. The operand should not be modified by the operation. The methodinfo represents the implementing method for adding one unit to the operand. If the methodinfo is null, and the operand is numeric, the semantics is to add one.

In DynamicExpressions the expected semantics are to ask the object to return a unit incremented value of itself. The object is not modified.

<h3 id="index">4.4.58 Index</h3>

IndexExpressions use this node kind. It represents an indexing operation or a property invocation that takes arguments.

<h3 id="label">4.4.59 Label</h3>

LabelExpressions use this node kind. It represents an unstructured flow of control target location.

A LabelTarget object identifies the Label's location. GotoExpressions refer to the same target object to designate that they jump to this LabelExpression location in the ET.

The target has a Type property because Goto's can transfer control to a location with a value. The type allows factory methods and the ET compiler to verify static typing intent within the ET. See section for more information.

A Goto can optionally deliver a value to the LabelExpression's location. In case execution flows through the label in a structured way (not via a jump), it has a DefaltValue expression that provides the result of the LabelExpression. The label's location is AFTER the DefaultValue expression. If an unstructured flow of control lands at this LabelExpression's location, the GotoExpression provides the result for the LabelExpression.

The LabelExpression.Type property is the same value as its Target.Type. Any GotoExpression that references the same Target must have a Type property that represents a type that is reference assignable to the type represented by Target.Type. There are two exceptions, see the factory method documentation for an explanation.

<h3 id="runtimevariables">4.4.60 RuntimeVariables</h3>

RuntimeVariablesExpression uses this node kind. It represents an expression that returns an IRuntimeVariables for a list of provided ParameterExpressions. It effectively lifts these variables to a closure for getting/setting them. Languages with constructs for accessing lexical variables can use this expression to access these closure variables.

<h3 id="loop">4.4.61 Loop</h3>

LoopExpression uses this node kind. It represents an expression that executes its body expression infinitely until a sub expression of the body exits the loop via a GotoExpression. Loop nodes have a Break property with a LabelTarget. When control transfers to this label with a value, the value becomes the result of the LoopExpression. The Break label can be null, in which case the Loop's Type property represents the void type.

<h3 id="switch">4.4.62 Switch</h3>

SwitchExpression uses this node kind. At a high level, this node's semantics is to evaluate the SwitchValue expression, then to evaluate each SwitchCase's TestValues in order. For each test value, if the SwitchCase.Comparison (invoked on the SwitchValue and TestValue) return True, then the corresponding SwitchCAse.Body executes. Each case is considered in the order it appears in the node. If no case fires, then the DefaultBody executes. The value resulting from the SwitchExpression is the last expression executed, which is typically the last expression of the selected case body.

If you want the effect of case fall through, then you can use GotoExpression and construct the target case as follows. The case's body can be a BlockExpression, and the first expression in it can be a LabelExpression with a null Expression property. The expression compiler detects patterns for eliminating the goto's.

If DefaultBody is null, the Type property must represent void type.

The Comparison MethodInfo must result in a Boolean value. If Comparison is null, then the test is performed as it would be with an Equal node. The comparison function gets invoked with the switch value as the first argument and each case's test value in turn as the second argument. Each test value is considered in the order it appears in the case.

The TestValue must have an integer type for the compilation of a Switch node to use .NET's IL level switch. If the SwitchValue type is a string, and all cases are string constants, the node compiles into a dictionary lookup followed by an IL level switch, as an optimization.

All test values in all cases must have exactly the same type.

All the case body expressions must have the same Type property as the Switch's Type property. There is one exception. If the Switch has a void type, then the case bodies can be any type, and the semantics is to automatically convert to void or squelch any result value.

As a sidenote, you can construct an if-then-else or Lisp-style 'cond' construct. If SwitchValue is True, then the case test values can be any expression that returns True. It is not an exact match semantically given various semantics regarding what is a non-false value, but the effect is very close.

The Type property must match every case's body's type, unless the Type property is void. If it is void, then the case bodies can be of any type, and any results are automatically "converted to void" or squelched.

<h3 id="throw-1">4.4.63 Throw</h3>

Use this node kind in UnaryExpression nodes to represent a dynamic flow of control to a matching catch block. These semantics are equivalent to C\#'s throw with the following exception: you can use Throw expressions in Filter block since IL supports this.

<h3 id="try">4.4.64 Try</h3>

TryExpression uses this node kind. It represents try-catch control flow, including finally and fault blocks. First the Body executes. Under normal execution, control flows from the body to the Finally expression, if any. The value of the TryExpresson is the Body expression's value. If an object is thrown (directly or indirectly) from within the Body, and there's no dynamically intervening appropriate catch handler, the CatchBlock' s are considered in order to find one with a Test type that is assignable from the thrown object. If there is such a CatchBlock, execution flows to its Body and then to the Finally expression, if any. The CatchBlock's body produces the value for the TryExpression in this case.

If non-null, the Finally expression always executes regardless of dynamic flow of control to or through the TryExpression.

If Fault is non-null, then Finally is null, and Handlers is empty. The Fault expresson executes if an object is thrown from the Body, directly or indirectly. Control flows from the Fault expression to some appropriate catch handler, or possibly an process unhandled throw error block. If execution flows from the Body expression to the Finally expression, if any, and no objects are thrown, then the Fault expression does NOT execute.

Try is legal in some places where it would not be allowed in IL, such as when the stack is non-empty. For example: Call(e0, e1, try { … } catch { …}, e3)

<h3 id="unbox">4.4.65 Unbox</h3>

Use the Unbox node kind in UnaryExpression nodes to represent an explicit unboxing operation. The semantics is to create an interior pointer to the boxed value that is tagged with the type represented by the Unbox node's Type property. The Unbox node's Type property represents the boxed value's value type. The operand expression's Type property must represent an interface type or type Object; a value type would not be boxed for any reason other than to be passed or assigned to an interface type or type Object. If the operand's Type property does not represent the type Object, then the Unbox node's Type property must represent a type that implements the operand's interface type.

This node maps to the IL unbox and unbox.any instructions.

**Design Rationale ...**

We will need this in v-next+1 if we look toward IL coverage in the ET model, but we added Unbox in .NET 4.0 to support DLR languages, such as IronPython. These languages want to present a programming model as exemplified here (without the commented out line):

> r = Rect()
>
> \#\# s = StrongBox\[Rect\](r)
>
> r.Intersect(r2)

The problem is that a dynamic language like IronPython has to box r to have actual pointers to objects. When the Intersect call happens, the CLR box gets unpacked, and the destructive Intersect call modifies a copy of r’s contents. If IronPython programmers explicitly StrongBox the rect, as in the commented out line, then the IronPython programmer gets the expected behavior of modifying r’s contents. This is not how dynamic language programmers want to think about using values like rects, and for the language to manage this boxing and unboxing correctly everywhere would be a lot of work to get right.

Note, the C\# code does the right thing:

> Rect r = new Rect()
>
> //Expr.Call(Expr.Convert(r, typeof(Rect)),
>
> // “Intersect”, new\[\] {r2})
>
> Expr&lt;Funct&lt;&gt;&gt; e = () =&gt; ((Rect) r).Intersect(r2)

C\# emits IL to pass the address to the r value in the CLR box object, so it works as expected. We've simply enabled dynamic languages using the DLR to do the same implementation.

<h3 id="addassign">4.4.66 AddAssign</h3>

Use this AddAssign node kind in BinaryExpression nodes to represent an Add compound assignment operation, without overflow checking. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the AddAssign node is the result of performing the Add opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL addition. Otherwise, the node searches for and applies a user-defined op\_Addition method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to add the second object to itself, modifying the first object's value in place.

<h3 id="addassignchecked">4.4.67 AddAssignChecked</h3>

Use the AddAssignChecked node kind in BinaryExpression nodes to represent an Add compound assignment operation, with overflow checking. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL addition. Otherwise, the node searches for and applies a user-defined op\_Addition method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

There is no use for this in a DynamicExpression.

<h3 id="divideassign">4.4.68 DivideAssign</h3>

Use the DivideAssign node kind in BinaryExpression nodes to represent a Divide compound assignment operation, with overflow checking. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the DivideAssign node is the result of performing the Divide opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL div. Otherwise, the node searches for and applies a user-defined op\_Division method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to divide itself by the second object, modifying the first object's value in place.

<h3 id="exclusiveorassign">4.4.69 ExclusiveOrAssign</h3>

Use the ExclusiveOrAssign node kind in BinaryExpression nodes to represent an ExclusiveOr compound assignment operation. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the ExclusiveOrAssign node is the result of performing the ExclusiveOr opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same integer or boolean type, the node has the semantics of IL xor. Otherwise, the node searches for and applies a user-defined op\_ExclusiveOr method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to ExlusiveOr the second object to itself, modifying the first object's value in place.

<h3 id="leftshiftassign">4.4.70 LeftShiftAssign</h3>

Use the LeftShiftAssign node kind in BinaryExpression nodes to represent a LeftShift compound assignment operation. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the LeftShiftAssign node is the result of performing the LeftShift opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the type integer, the node has the semantics of IL LeftShift. Otherwise, the node searches for and applies a user-defined op\_LeftShift method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to shift contents (most likely bits) left the number of times representd by the second object, modifying the first object's value in place.

<h3 id="moduloassign">4.4.71 ModuloAssign</h3>

Use the ModuloAssign node kind in BinaryExpression nodes to represent an Modulo compound assignment operation. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the ModuloAssign node is the result of performing the Modulo opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL rem. For example, -10 mod 3 is -1, and 10 mod -3 is 1. Otherwise, the node searches for and applies a user-defined op\_Modulus method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this node kind in Dynamic Expression to ask the first object to divide itself by the second object. If the objects are numbers, the expectation is that the result is an integer remainder resulting from TruncateDivide'ing the first object by the second. The first object is modified in place.

<h3 id="multiplyassign">4.4.72 MultiplyAssign</h3>

Use the MultipleAssign node kind in BinaryExpression nodes to represent an Multiply compound assignment operation, without overflow checking. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the MultiplyAssign node is the result of performing the Multiply opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL multiplication. Otherwise, the node searches for and applies a user-defined op\_Multiply method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to multiply the second object to itself, modifying the first object's value in place. If the objects are numbers, the expectation is that the result is a number.

<h3 id="mulitplyassignchecked">4.4.73 MulitplyAssignChecked</h3>

Use the MultiplyAssignChecked node kind in BinaryExpression nodes to represent an Multiply compound assignment operation, with overflow checking. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the MultiplyAssign node is the result of performing the Multiply opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL multiplication. Otherwise, the node searches for and applies a user-defined op\_Multiply method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

There is no use for this node kind a DynamicExpression.

<h3 id="orassign">4.4.74 OrAssign</h3>

Use the OrAssign node kind in BinaryExpression nodes to represent an Or compound assignment operation. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the OrAssign node is the result of performing the Or opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same integer or boolean type, the node has the semantics of IL or. Otherwise, the node searches for and applies a user-defined op\_BitwiseOr method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to Or the second object to itself, modifying the first object's value in place.

<h3 id="powerassign">4.4.75 PowerAssign</h3>

Use the PowerAssign node kind in BinaryExpression nodes to represent an exponentiation compound assignment operation. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the PowerAssign node is the result of performing the exponentiation opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, the semantics is to invoke the System.Math.Pow function on the operands.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in DynamicExpressions to request that the first object raise itself to the power of the second object. For numeric objects, you should expect a numeric result, ignoring NaN and overflow issues. Otherwise, the semantics is whatever the object implements. Neither object should be modified.

<h3 id="rightshiftassign">4.4.76 RightShiftAssign</h3>

Use the RightShfitAssign node kind in BinaryExpression nodes to represent a RightShift compound assignment operation. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the RightShiftAssign node is the result of performing the RightShift opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the type integer, the node has the semantics of IL RightShift. Otherwise, the node searches for and applies a user-defined op\_RightShift method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to shift contents (most likely bits) right the number of times representd by the second object, modifying the first object's value in place.

<h3 id="subtractassign">4.4.77 SubtractAssign</h3>

Use SubtractAssign node kind in BinaryExpression nodes to represent an Subtract compound assignment operation, without overflow checking. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the SubtractAssign node is the result of performing the Subtract opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL subtraction. Otherwise, the node searches for and applies a user-defined op\_Subtraction method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

Use this in a DynamicExpression to represent a binary operator for asking the first object to subtract the second object from itself, modifying the first object's value in place.

<h3 id="subtractassignchecked">4.4.78 SubtractAssignChecked</h3>

Use SubtractAssignCheced node kind in BinaryExpression nodes to represent an Subtract compound assignment operation, with overflow checking. The Left expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the SubtractAssign node is the result of performing the Subtract opreration on the operands.

The node's methodinfo performs only the basic binary operation (Add, Subtract, etc.). The resulting value is stored in the location represented by Left. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL subtraction. Otherwise, the node searches for and applies a user-defined op\_Subtraction method.

If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location.

There is no use for this node kind a DynamicExpression.

<h3 id="preincrementassign">4.4.79 PreIncrementAssign</h3>

Use the PreIncrementAssign node kind in UnaryExpression nodes to represent incrementing a value in-place by one unit. The result is stored to the operand's location. The operand expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the PreIncrementAssign node is the result of performing the increment opreration on the operand.

The node's methodinfo performs only the basic unary operation (Add, Subtract, etc.). If methodinfo is null, and the operand's Type property represents a numeric type, the node has the semantics of IL addition of one, with the result stored in the location indicated by the operand. Otherwise, the node searches for and applies a user-defined op\_Increment method.

There is no use for this in a DynamicExpression.

<h3 id="predecrementassign">4.4.80 PreDecrementAssign</h3>

Use the PreDecrementAssign node kind in UnaryExpression nodes to represent decrementing a value in-place by one unit. The result is stored to the operand's location. The operand expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the PreDecrementAssign node is the result of performing the decrement opreration on the operand.

The node's methodinfo performs only the basic unary operation (Add, Subtract, etc.). If methodinfo is null, and the operand's Type property represents a numeric type, the node has the semantics of IL subtraction of one, with the result stored in the location indicated by the operand. Otherwise, the node searches for and applies a user-defined op\_Decrement method.

There is no use for this in a DynamicExpression.

<h3 id="postincrementassign">4.4.81 PostIncrementAssign</h3>

Use the PostIncrementAssign node kind in UnaryExpression nodes to represent incrementing a value in-place by one unit. The result is stored to the operand's location. The operand expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the PostIncrementAssign node is the the operand's value before performing the operation.

The node's methodinfo performs only the basic unary operation (Add, Subtract, etc.). If methodinfo is null, and the operand's Type property represents a numeric type, the node has the semantics of IL addition of one, with the result stored in the location indicated by the operand. Otherwise, the node searches for and applies a user-defined op\_Increment method.

There is no use for this in a DynamicExpression.

<h3 id="postdecrementassign">4.4.82 PostDecrementAssign</h3>

Use the PreDecrementAssign node kind in UnaryExpression nodes to represent decrementing a value in-place by one unit. The result is stored to the operand's location. The operand expression must be one of the node types: ParameterExpression, MemberExpression, or IndexExpression. The result of the PreDecrementAssign node is the result of the operand before performing the operation.

The node's methodinfo performs only the basic unary operation (Add, Subtract, etc.). If methodinfo is null, and the operand's Type property represents a numeric type, the node has the semantics of IL subtraction of one, with the result stored in the location indicated by the operand. Otherwise, the node searches for and applies a user-defined op\_Decrement method.

There is no use for this in a DynamicExpression.

<h3 id="onescomplement">4.4.83 OnesComplement</h3>

Use OnesComplement in UnaryExpression nodes to represent bitwise negation.

For legacy purposes, the Not node kind represents bitwise and logical negation. The OneComplement node kind makes DLR interoperability intent more clear in the DynamicMetaObject protocol, and for meta-programming scenarios with the static nodes.

Use OnesComplement in a DynamicExpression to ask an object to returns its bitwise negation. Use Not in DynamicExpressions to ask an object to return its logical negation.

<h3 id="istrue">4.4.84 IsTrue</h3>

Use the IsTrue node kind in UnaryExpression nodes to represent an expression whose value is true if the argument expression represents a true value. The semantics is the same as an expression usable in C\# statements such as 'if', 'while', 'for', etc.: if the argument value is not implicitly convertible to bool, then it must implement the op\_true operator method.

Use this operator in DynamicExpressions (actually need two) to create ETs for conditional-or. For example, you could have a DynamicExpression using IsTrue to test if a left operand is true. If it is, return the operand value, but if it is not IsTrue, use another DynamicExpression to Or the left with the right operand to produce the result.

<h3 id="isfalse">4.4.85 IsFalse</h3>

Use the IsFalse node kind in UnaryExpression nodes to represent an expression whose value is true if the argument expression represents a false value. The semantics is the same as an expression usable in C\# statements such as 'if', 'while', 'for', etc.: if the argument value is not implicitly convertible to bool, then it must implement the op\_false operator method.

Use this operator in DynamicExpressions (actually need two) to create ETs for conditional-and. For example, you could have a DynamicExpression using IsFalse to test if a left operand is false. If it is, return the operand value, but if it is not IsFalse, use another DynamicExpression to And the left with the right operand to produce the result.

<h3 id="assignref-post-clr-4.0">4.4.86 AssignRef (POST CLR 4.0)</h3>

Use AssignRef in BinaryExpressions to represent assigning an indirect reference to the location represented by the Right Expression. The Right expression must be one of the node types ParameterExpression, MemberExpression, or IndexExpression. The Left Expression must be a ParameterExpression with IsByRef set to True.

The order of evaluation is first any sub expressions in the Right expression. For example, if the Right expression is “e1.foo” or “e1\[e2, e3\]”, then e1 is evaluated first in both cases, then e2 and e3 in the second case. The root semantics of the Right expression (member fetch, index, variable fetch) are not evaluated but instead used to emit instructions for creating a ByRef local variable that refers to the storage location.

After an AssignRef initializes a variable represented by ParameterExpression, then the ParameterExpression can be used as the Left Expression in an Assign node. The semantics is to store the value resulting from the Assign node's Right expression to the location referenced by the IsByRef ParameterExpression.

There is no use for AssignRef with DynamicExpressions.

**Design Rationale ...**

We considered this in ETs v2 because we know we would need it post CLR 4.0 for IL completeness, and we could make use of it in rewriting ETs for compilation purposes. For example, to allow nesting of gotos/try-catch's, you could rewrite this:

class Point {

public double X, Y;

}

Point Bar() { … }

void Foo(ref double x, …) { … }

void Main() {

// NOTE: not currently legal in expression trees

// because it combines “ref” with a nested try statement

Foo(ref Bar().X, try { … } finally { … }, …);

into this:

// Using AssignRef this is legal, and preserves argument evaluation order

double& arg0 = Bar().X;

arg2 = try { … } finally { … };

…

argN = …

Foo(arg0, arg1, …, argN);

Having AssignRef is not required to make this work, but it makes the ET transformation simpler. You need the temporary variables though since IL won't allow entering the try block with values on the IL stack.

<h2 id="defaultexpression-class">4.5 DefaultExpression Class</h2>

This class represents empty expressions and default values of types. It has two factories. One takes no arguments and returns an empty or no-op Expression whose type is Void. The other factory takes a type, and the expression compiles to a constant returning default(T) for the type.

These are useful for filling in required expressions that simply complete other expressions, such as the alternative expression of an 'if' or the last expression of a block whose type is Void.

<h3 id="class-summary-1">4.5.1 Class Summary</h3>

public sealed class DefaultExpression : Expression {}

<h3 id="factory-methods">4.5.2 Factory Methods</h3>

Expression has the following factory methods for DefaultExpressions:

public static DefaultExpression Default(Type type);

public static DefaultExpression Empty();

If type is void, Default returns the same node that calling Empty returns.

<h2 id="binaryexpression-class">4.6 BinaryExpression Class</h2>

This class represents a many kinds of operations such as basic arithmetic, bit manipulations, logical comparisons, assignment, and so on. With only a couple of exceptions, all the operations fit the shape of having left and right operands as their primary inputs.

The exceptions to the shape design are Coalesce and ...Assign node kinds. They take an optional conversion lambda you can specify that executes last and converts the node's intermediate result value to the node's Type or the Left.Type. This conversion aspect is required in the node's semantics so that languages or ET producers can specify the exact overloaded conversion method or a custom method.

Operations on numeric types do not implicitly expand to 32bit integers.

These nodes have many node kinds for the various operations they can support. See section for more details on the semantics of various node kinds used with BinaryExpressions.

<h3 id="class-summary-2">4.6.1 Class Summary</h3>

public class BinaryExpression : Expression {

public LambdaExpression Conversion { get; }

public Boolean IsLifted { get; }

public Boolean IsLiftedToNull { get; }

public Expression Left { get; }

public MethodInfo Method { get; }

public Expression Right { get; }

public BinaryExpression Update

(Expression left, LambdaExpression conversion, Expression right)

<h3 id="conversion-property">4.6.2 Conversion Property</h3>

This property returns the expression that models the type conversion function used by a Coalesce node kind, and by OPAssign nodes where OP is Add, Multiply, etc. If the node kind is not Coalese or one of the OPAssign, this returns null.

Signature:

public LambdaExpression Conversion { get; }

<h3 id="islifted-property">4.6.3 IsLifted Property</h3>

This property returns true if the node represents an operator call that takes non-nullable parameters, but the calls passes nullable arguments.

Signature:

public Boolean IsLifted { get; }

<h3 id="isliftedtonull-property">4.6.4 IsLiftedToNull Property</h3>

This property returns true if the node represents a call to an operator that returns a nullable type. If a nullable argument evaluates to null (Nothing in Visual Basic), the operator returns a null reference (Nothing in Visual Basic).

Signature:

public Boolean IsLiftedToNull { get; }

<h3 id="method-property">4.6.5 Method Property</h3>

This property returns the MethodInfo associated the operation to further specify its semantics. The value may be null if the operation represents a CLI predefined operator.

Signature:

public MethodInfo Method { get; }

<h3 id="left-property">4.6.6 Left Property</h3>

This property returns the expression for the first argument to the operation.

Signature:

public Expression Left { get; }

<h3 id="right-property">4.6.7 Right Property</h3>

This property returns the expression for the second argument to the operation.

Signature:

public Expression Right { get; }

<h3 id="update-method">4.6.8 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public BinaryExpression Update

(Expression left, LambdaExpression conversion, Expression right)

<h3 id="arithmetic-shift-and-bit-operations-factory-methods">4.6.9 Arithmetic, Shift, and Bit Operations Factory Methods</h3>

Expression has the following factory methods for BinaryExpressions representing arithemetic, shift, and bit operations:

public static BinaryExpression Add

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Add(Expression left,

Expression right);

public static BinaryExpression AddAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AddAssign(Expression left,

Expression right);

public static BinaryExpression AddAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression AddAssignChecked(Expression left,

Expression right);

public static BinaryExpression AddAssignChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AddAssignChecked

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression AddChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AddChecked(Expression left,

Expression right);

public static BinaryExpression And(Expression left,

Expression right);

public static BinaryExpression And

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression AndAssign(Expression left,

Expression right);

public static BinaryExpression AndAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Divide(Expression left,

Expression right);

public static BinaryExpression Divide

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression DivideAssign(Expression left,

Expression right);

public static BinaryExpression DivideAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression DivideAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

­

public static BinaryExpression ExclusiveOr

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression ExclusiveOr(Expression left,

Expression right);

public static BinaryExpression ExclusiveOrAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression ExclusiveOrAssign

(Expression left, Expression right);

public static BinaryExpression ExclusiveOrAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression LeftShift

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression LeftShift(Expression left,

Expression right);

public static BinaryExpression LeftShiftAssign(Expression left,

Expression right);

public static BinaryExpression LeftShiftAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression LeftShiftAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression Modulo

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Modulo(Expression left,

Expression right);

public static BinaryExpression ModuloAssign(Expression left,

Expression right);

public static BinaryExpression ModuloAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression ModuloAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression Multiply(Expression left,

Expression right);

public static BinaryExpression Multiply

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression MultiplyAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression MultiplyAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression MultiplyAssign

(Expression left, Expression right);

public static BinaryExpression MultiplyAssignChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression MultiplyAssignChecked

(Expression left, Expression right);

public static BinaryExpression MultiplyChecked

(Expression left, Expression right);

public static BinaryExpression MultiplyChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Or

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Or(Expression left,

Expression right);

public static BinaryExpression OrAssign(Expression left,

Expression right);

public static BinaryExpression OrAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression OrAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression Power

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Power(Expression left,

Expression right);

public static BinaryExpression \`ign(Expression left,

Expression right);

public static BinaryExpression PowerAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression PowerAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression RightShift

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression RightShift(Expression left,

Expression right);

public static BinaryExpression RightShiftAssign(Expression left,

Expression right);

public static BinaryExpression RightShiftAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression RightShiftAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression Subtract

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression Subtract(Expression left,

Expression right);

public static BinaryExpression SubtractAssign(Expression left,

Expression right);

public static BinaryExpression SubtractAssign

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression SubtractAssign

(Expression left, Expression right, MethodInfo method,

LambdaExpression conversion)

public static BinaryExpression SubtractAssignChecked

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression SubtractAssignChecked

(Expression left, Expression right);

public static BinaryExpression SubtractChecked

(Expression left, Expression right);

public static BinaryExpression SubtractChecked

(Expression left, Expression right, MethodInfo method);

The following is from the v1 spec ... except liftToNull semantics which now describes the code that shipped ... and comments regarding ...Assign factory methods

Left and right parameters must be non-null.

To determine an implementation of the node, if a non-null method is supplied, that becomes the implementing method for this node. It must represent a non-void static method with two arguments, or an exception occurs. Otherwise, if left.Type and right.Type are both numeric or both boolean types for which the corresponding operator is predefined in CLI, the factories set the implementing method is null. In the case of Power, if left.Type and right.Type both represent double, the implementing method is System.Math.Pow. Otherwise, if either of left.Type and right.Type contains a user definition of the corresponding binary operator (in the form of a static non-void op\_... method with two arguments), the MethodInfo representing that becomes the implementing method. Otherwise, an exception occurs.

If the implementing method is non-null, then:

- if left.Type and right.Type are assignable to the corresponding argument types of the implementing method, the node is non-lifted, and the node type is the return type of the implementing method.

- If the following is true, then the BinaryExpression's Type is lifted to the corresponding nullable type:

- left.Type and right.Type are both value types and both nullable

- the corresponding non-nullable types are equal to the corresponding argument types of the implementing method

- the return type of the implementing method is non-nullable value type

- you supply liftToNull as true

> If the method return type is not bool, and both arguments are nullable, then the result will always be a nullable type regardless of the liftToNull argument.

- Otherwise an exception occurs.

If the implementing method is null, then left.Type and right.Type are numeric or boolean types for which the corresponding operator is predefined in C\#. Furthermore

- If both left.Type and right.Type are non-nullable, the node is non-lifted, and the node type is the result type of the C\# predefined operator.

- If both left.Type and right.Type are nullable, the node is lifted, and the node type is the nullable type corresponding to the result type of the C\# predefined operator.

- Otherwise an exception occurs.

The ...Assign factories require the left argument expression to be one of the node types ParameterExpression, MemberExpression, or IndexExpression. They use the method argument only for the basic binary operation (Add, Subtract, etc.) and then assign the result to the location specified by the left argument. If methodinfo is null, and the Left.Type and Right.Type properties represent the same numeric type, the node has the semantics of IL addition. Otherwise, the node searches for and applies a user-defined op\_Addition method. If the node's conversion lambda is non-null, then the semantics is to pass the result of the basic binary operation to the lambda. The result of the conversion lambda is then stored in the Left location. If the conversion lambda is non-null, and the Left.Type and Right.Type properties represent numeric types, then the factories throw and exception.

The resulting BinaryExpression has:

- Node kind set to the ExpressionType member with the same name as the factory method

- Left and Right set to left and right, respectively

- Type set to the node type as described above

- Method set to the implementing method

- If the node is lifted, IsLifted and IsLiftedToNull are true, otherwise they are false.

- Conversion set to null for all node kinds except the ...Assign node kinds, in which case Conversion is set to the supplied MethodInfo.

<h3 id="obsolete-array-index-single-dimension-factory">4.6.10 Obsolete Array Index (Single-dimension) Factory</h3>

The ArrayIndex factories will be obsolete in lieu of the more general IndexExpression factory methods.

Expression has the following factory methods for BinaryExpressions representing single-dimension array element fetching:

public static BinaryExpression ArrayIndex(Expression array,

Expression index);

The following is derived from the v1 spec ...

Array and index must be non-null. array.Type must represent an array type with rank 1, and index.Type must represent the int type. The resulting BinaryExpression has:

- NodeType ArrayIndex.

- Left and Right properties equal to array and index, respectively.

- Type representing the element type of array.Type.

- Method and Conversion are null.

- Both IsLifted and IsLiftedToNull are false.

<h3 id="assignment-factory-method">4.6.11 Assignment Factory Method</h3>

Expression has the following factory methods for BinaryExpressions representing assignment operations:

public static BinaryExpression Assign(Expression left,

Expression right);

Left must be one of the node types ParameterExpression, MemberExpression, or IndexExpression. Right.Type must be reference assignable to Left.Type. The resulting node has node kind Assign. If the left type represents a property or indexed property, it must have a setter.

<h3 id="coalesce-operator-factory-methods">4.6.12 Coalesce Operator Factory Methods</h3>

Expression has the following factory methods for BinaryExpressions representing coalesce operations (that is, what 'or' returns in a dynamic language):

public static BinaryExpression Coalesce(Expression left,

Expression right);

public static BinaryExpression Coalesce

(Expression left, Expression right,

LambdaExpression conversion);

The following is from the v1 spec ...

Left and right must be non-null. left.Type must represent a reference type or a nullable value type. If left.Type is nullable, and right.Type is implicitly convertible to the non-nullable version of left.Type then the result type is the non-nullable version of left.Type. Otherwise if right.Type is implicitly convertible to left.Type then the result type is left.Type. Otherwise if the non-nullable version of left.Type is implicitly convertible to right.Type then the result type is right.Type. Otherwise an exception is thrown.

If conversion is non-null, it must have conversion.Type equal to a delegate type. The return type of the delegate type (conversion.Type) must NOT be void, and must be equal to right.Type. The delegate type must have exactly one parameter, and the type of this parameter must be assignable from the erased or unerased version of left.Type.

The resulting BinaryExpression has Left and Right properties equal to left and right, respectively, Conversion equal to conversion, and Type equal to the result type. Method is null, and both IsLifted and IsLiftedToNull are false.

<h3 id="conditional-and-and-or-operator-factory-methods">4.6.13 Conditional And and Or Operator Factory Methods</h3>

Expression has the following factory methods for BinaryExpressions representing conditional 'and' and 'or' operations (referred to sometimes as "short-circuiting"):

public static BinaryExpression AndAlso(Expression left,

Expression right);

public static BinaryExpression AndAlso

(Expression left, Expression right, MethodInfo method);

public static BinaryExpression OrElse(Expression left,

Expression right);

public static BinaryExpression OrElse

(Expression left, Expression right, MethodInfo method);

The following is from the v1 spec ... except liftToNull semantics which now describes the code that shipped ...

Left and right must be non-null. If a non-null method is supplied, that becomes the implementing method for this node. It must represent a non-void static method with two arguments, or an exception occurs.

Otherwise, if either of left.Type and right.Type contains a user definition of the corresponding binary operator (in the form of a static non-void op\_... method with two arguments), the MethodInfo representing that becomes the implementing method. Otherwise, if left.Type and right.Type are both numeric or boolean types for which the corresponding operator is predefined in C\#, the implementing method is null. Otherwise, an exception occurs.

If the implementing method is non-null, then:

- if left.Type and right.Type are assignable to the corresponding argument types of the implementing method, the node is non-lifted, and the node type is the return type of the implementing method.

- If the following is true, then the BinaryExpression's Type is lifted to the corresponding nullable type:

- left.Type and right.Type are both value types and both nullable

- the corresponding non-nullable types are equal to the corresponding argument types of the implementing method

- the return type of the implementing method is non-nullable value type

- you supply liftToNull as true

> If the method return type is not bool, and both arguments are nullable, then the result will always be a nullable regardless of the “liftToNull” flag.

- Otherwise an exception occurs.

If the implementing method is null, then left.Type and right.Type are the same boolean type. Furthermore

- If both left.Type and right.Type are non-nullable, the node is non-lifted, and the node type is the result type of the C\# predefined operator.

- If both left.Type and right.Type are nullable, the node is lifted, and the node type is the nullable type corresponding to the result type of the C\# predefined operator.

- Otherwise an exception occurs.

The resulting BinaryExpression has Left and Right properties equal to left and right, respectively, Type equal to the node type and Method equal to the implementing method. If the node is lifted, IsLifted and IsLiftedToNull are true, otherwise they are false. Conversion is equal to null.

<h3 id="comparison-operators-factory-methods">4.6.14 Comparison Operators Factory Methods</h3>

Expression has the following factory methods for BinaryExpressions representing comparison operations:

public static BinaryExpression Equal(Expression left,

Expression right);

public static BinaryExpression Equal

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression ReferenceEqual(Expression left,

Expression right)

public static BinaryExpression NotEqual(Expression left,

Expression right);

public static BinaryExpression NotEqual

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression ReferenceNotEqual(Expression left,

Expression right)

public static BinaryExpression GreaterThan

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression GreaterThan(Expression left,

Expression right);

public static BinaryExpression GreaterThanOrEqual

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression GreaterThanOrEqual(Expression left,

Expression right);

public static BinaryExpression LessThan

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression LessThan(Expression left,

Expression right);

public static BinaryExpression LessThanOrEqual

(Expression left, Expression right, Boolean liftToNull,

MethodInfo method);

public static BinaryExpression LessThanOrEqual(Expression left,

Expression right);

The following is from the v1 spec ... except liftToNull semantics which now describes the code that shipped ...

Left and right must be non-null. If a non-null method is supplied, that becomes the implementing method for this node. It must represent a non-void static method with two arguments, or an exception occurs.

Otherwise, if either of left.Type and right.Type contains a user definition of the corresponding binary operator (in the form of a static non-void op\_... method with two arguments, using the CLS name), the MethodInfo representing that becomes the implementing method. Otherwise, if the corresponding operator is predefined in C\# for left.Type and right.Type, the implementing method is null. Otherwise, an exception occurs.

If the implementing method is non-null, then:

- if left.Type and right.Type are assignable to the corresponding argument types of the implementing method, the node is non-lifted and the node type is the return type of the implementing method.

- If the following is true, then the BinaryExpression's Type is lifted to bool?:

- left.Type and right.Type are both value types and both nullable

- the corresponding non-nullable types are equal to the corresponding argument types of the implementing method

- the return type of the implementing method is bool

- you supply liftToNull as true

> If liftToNull is false, the Type property represents bool. If the method return type is not bool, and both arguments are nullable, then the result will always be a nullable regardless of the “liftToNull” flag. This accommodates languages like VB that return null if an argument is null and languages like C\# that return false if an argument is null.

- Otherwise an exception occurs.

ReferenceEqual and ReferenceNotEqual are provided as convenience factories that ensure pointer comparisons without having to wrap each operand in Convert to Object expressions. The factories also aid readability of code. If either operand is a value type, then the factories throw an exception.

The resulting BinaryExpression has:

- Left and Right set to left and right, respectively

- Type set as described above

- Method set to the implementing method

- If the node is lifted, IsLifted is true and IsLiftedToNull is equal to the liftToNull argument; otherwise both are false.

- Conversion set to null.

<h3 id="general-factory-methods">4.6.15 General Factory Methods</h3>

Expression has the following general factory methods for BinaryExpressions:

public static BinaryExpression MakeBinary

(ExpressionType binaryType, Expression left, Expression right);

public static BinaryExpression MakeBinary

(ExpressionType binaryType, Expression left, Expression right,

Boolean liftToNull, MethodInfo method);

public static BinaryExpression MakeBinary

(ExpressionType binaryType, Expression left, Expression right,

Boolean liftToNull, MethodInfo method,

LambdaExpression conversion);

The following is from the v1 spec ...

Based on the value of binaryType, MakeBinary will return the result of calling the corresponding factory method above with the same parameters. If binaryType is not appropriate for any of the above factory methods, MakeBinary throws an ArgumentException.

All the requirements and guarantees of the called factory method apply.

<h2 id="typebinaryexpression-class">4.7 TypeBinaryExpression Class</h2>

This class represents type tests. It can have node kinds TypeIs or TypeEqual. The former has the semantics of the IsInst CLR instruction (example below of how that is different than C\#'s semantics), and it tests the Expression value for having a sub type of the TypeOperand value. TypeEqual tests for an exact type match in essence; for example, a boxed int will equal Int32 and Nullable&lt;Int32&gt; because both are valid types for a boxed int.

Example distinction between TypeIs node kind and C\#'s 'is' operator:

> using System;
>
> using System.Linq;
>
> using System.Linq.Expressions;
>
> class Test {
>
> public static void Main() {
>
> Func&lt;bool&gt; func1
>
> // C\# compiles the 'is' here directly instead of using
>
> // an ET and Expression.Compile().
>
> = () =&gt; new\[\] { DayOfWeek.Friday } is int\[\];
>
> Expression&lt;Func&lt;bool&gt;&gt; expr1
>
> // C\# emits a TypeIs node for 'is' here, which is a bug
>
> // in 2008.
>
> = () =&gt; new\[\] { DayOfWeek.Friday } is int\[\];
>
> Console.WriteLine(func1());
>
> Console.WriteLine(expr1.Compile()());
>
> // When the above code is compiled and run:
>
> // Expected: prints False and False
>
> // Actual: prints False and True

The issue is that C\# appropriate regards enums and integers as mutually type-distinct, though there are explicit conversions between them. However, the CLR compares an array with elements of the same underlying type as type equal, and arrays of enums in the CLR are just arrays of ints (in this case).

<h3 id="class-summary-3">4.7.1 Class Summary</h3>

public sealed class TypeBinaryExpression : Expression {

public Expression Expression { get; }

public Type TypeOperand { get; }

public TypeBinaryExpression Update(Expression expression)

<h3 id="expression-property">4.7.2 Expression Property</h3>

This property returns the expression that produces a value for checking if its type is the type represented by the Type property.

Signature:

public Expression Expression { get; }

<h3 id="typeoperand-property">4.7.3 TypeOperand Property</h3>

This property returns the type to test whether Expression results in a value of this type.

Signature:

public Type TypeOperand { get; }

<h3 id="update-method-1">4.7.4 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public TypeBinaryExpression Update(Expression expression)

<h3 id="factory-methods-1">4.7.5 Factory Methods</h3>

Expression has the following factory methods for TypeBinaryExpressions:

public static TypeBinaryExpression TypeEqual

(Expression expression, Type type);

public static TypeBinaryExpression TypeIs(Expression expression,

Type type);

The following is from the v1 spec ...

Expression and type must be non-null. The resulting TypeBinaryExpression has:

- Node kind TypeIs or TypeEqual, as appropriate

- Type set to bool

- Expression and TypeOperand properties equal to expression and type arguments.

<h2 id="unaryexpression-class">4.8 UnaryExpression Class</h2>

This class represents a many kinds of operations such as basic arithmetic, side-effecting arithemetic, control flow, and so on. With only a couple of exceptions, all the operations fit the shape of having a single operand as their primary input.

These nodes have many node kinds for the various operations they can support. See section for more details on the semantics of various node kinds used with UnaryExpressions.

<h3 id="class-summary-4">4.8.1 Class Summary</h3>

public sealed class UnaryExpression : Expression {

public Boolean IsLifted { get; }

public Boolean IsLiftedToNull { get; }

public MethodInfo Method { get; }

public Expression Operand { get; }

public UnaryExpression Update(Expression operand)

<h3 id="islifted-property-1">4.8.2 IsLifted Property</h3>

This property returns true if the node represents an operator call that takes non-nullable parameters, but the calls passes nullable arguments.

Signature:

public Boolean IsLifted { get; }

<h3 id="isliftedtonull-property-1">4.8.3 IsLiftedToNull Property</h3>

This property returns true if the node represents a call to an operator that returns a nullable type. If a nullable argument evaluates to null (Nothing in Visual Basic), the operator returns a null reference (Nothing in Visual Basic).

Signature:

public Boolean IsLiftedToNull { get; }

<h3 id="method-property-1">4.8.4 Method Property</h3>

This property returns the MethodInfo associated the operation to further specify its semantics. The value may be null if the Type property represents a numeric or boolean type.

Signature:

public MethodInfo Method { get; }

<h3 id="operand-property">4.8.5 Operand Property</h3>

This property returns the expression that models the single argument of the operation. The property's value may be null (when the node kind is Throw).

V1 guaranteed this was never null, but we allow null for the Throw node kind (at least).

Signature:

public Expression Operand { get; }

TypeAs and Convert node kinds use the Expression.Type property as the implicit operand for those operations.

<h3 id="update-method-2">4.8.6 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public UnaryExpression Update(Expression operand)

<h3 id="arraylength-factory-method">4.8.7 ArrayLength Factory Method</h3>

public static UnaryExpression ArrayLength(Expression array);

The following is derived from the v1 spec ...

Array must be non-null, and array.Type must represent an array type.

The resulting UnaryExpression has

- Node kind ArrayLength

- Operand set to expression

- Type set to the type int

- Method set to null

- IsLifted and IsLiftedToNull set to false

<h3 id="conversion-factory-methods">4.8.8 Conversion Factory Methods</h3>

Expression has the following factory methods for UnaryExpressions representing conversion operations:

public static UnaryExpression Convert

(Expression expression, Type type, MethodInfo method);

public static UnaryExpression Convert(Expression expression,

Type type);

public static UnaryExpression ConvertChecked

(Expression expression, Type type, MethodInfo method);

public static UnaryExpression ConvertChecked(Expression expression,

Type type);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

Expression and type must be non-null. If a non-null method is supplied, that becomes the implementing method for this node. It must represent a non-void static method with one argument, or an exception occurs.

If method is null, the factories essentially prefer primitive type convert first, then implicit conversions (order determined by order reflection gives them and searching the list for the first one), then explicit conversions last. If either expression.Type or type contains a user definition of the implicit or explicit conversion operator (in the form of a static non-void op\_Implicit or op\_Explicit method with one argument), the MethodInfo representing that becomes the implementing method. ~~If more than one such method exists, an exception occurs~~. Otherwise,

- If both expression.Type and type represent value types, and both are numeric, Boolean, nullable, or non-nullable enumeration types, the implementing method is null.

- If either of expression.Type or type is a reference type, and an explicit boxing, unboxing or reference conversion exists from expression.Type to type, the implementing method is null.

- Otherwise, an exception occurs.

If the implementing method is non-null, then

- if expression.Type is assignable to the argument type of the implementing method, and the return type of the implementing method is assignable to type the node is non-lifted.

- If either or both of expression.Type or type is a nullable value type, and the corresponding non-nullable value types are equal to the argument type and the return type of the implementing method, respectively, then the node is lifted.

- Otherwise an exception occurs.

If the implementing method is null, then expression.Type and type are both numeric or boolean types. Furthermore

- If type is bool or bool? then expression.Type must also be either bool or bool?, or an exception occurs.

- If both expression.Type and type are non-nullable, the node is non-lifted.

- Otherwise the node is lifted.

Note, the Convert factories throw an exception if you try to convert to void. To convert to void so that you squelch the result of an expression (that is, execute it only for side-effects), you must finesse this with:

Expression.Block(typeof(void), expr)

The resulting UnaryExpression has NodeType Convert or ConvertChecked, respectively, Operand equal to expression, Type equal to type and Method equal to the implementing method. If the node is lifted, IsLifted is true, otherwise it is false. IsLiftedToNull is always false.

<h3 id="functional-increment-and-decrement-factory-methods">4.8.9 Functional Increment and Decrement Factory Methods</h3>

Expression has the following factory methods for UnaryExpressions representing functional increment and decrement operations:

public static UnaryExpression Decrement(Expression expression,

MethodInfo method);

public static UnaryExpression Decrement(Expression expression);

public static UnaryExpression Increment(Expression expression,

MethodInfo method);

public static UnaryExpression Increment(Expression expression);

If method is null, and the expression's Type property represents a numeric type, then the node uses .NET primitives for adding or subtracting one. If the expression's Type is not numeric, then the factory searches for a user defined op\_Decrement or op\_Increment implementation on the type.

<h3 id="side-effecting-pre--and-post--increment-and-decrement-factory-methods">4.8.10 Side-effecting Pre- and Post- Increment and Decrement Factory Methods</h3>

Expression has the following factory methods for UnaryExpressions representing side-effecting pre- and post- increment and decrement operations:

public static UnaryExpression PostDecrementAssign

(Expression expression);

public static UnaryExpression PostDecrementAssign

(Expression expression, MethodInfo method);

public static UnaryExpression PostIncrementAssign

(Expression expression);

public static UnaryExpression PostIncrementAssign

(Expression expression, MethodInfo method);

public static UnaryExpression PreDecrementAssign

(Expression expression);

public static UnaryExpression PreDecrementAssign

(Expression expression, MethodInfo method);

public static UnaryExpression PreIncrementAssign

(Expression expression, MethodInfo method);

public static UnaryExpression PreIncrementAssign

(Expression expression);

See section for more information.

<h3 id="numeric-negation-and-plus-factory-methods">4.8.11 Numeric Negation and Plus Factory Methods</h3>

Expression has the following factory methods for UnaryExpressions representing numeric negation and "plus" (CLR op\_UnaryPlus methods) operations:

public static UnaryExpression Negate(Expression expression);

public static UnaryExpression Negate(Expression expression,

MethodInfo method);

public static UnaryExpression NegateChecked(Expression expression,

MethodInfo method);

public static UnaryExpression NegateChecked

(Expression expression);

public static UnaryExpression UnaryPlus(Expression expression);

public static UnaryExpression UnaryPlus(Expression expression,

MethodInfo method);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

Expression must be non-null. If a non-null method is supplied, that becomes the implementing method for this node. It must represent a non-void static method with one argument, or an exception occurs.

Otherwise, if expression.Type contains a user definition of the unary plus or minus operator respectively (in the form of a static one-argument non-void op\_UnaryPlus or op\_UnaryNegation method), the MethodInfo representing that becomes the implementing method. Otherwise, if expression.Type is a numeric type, the implementing method is null. Otherwise, an exception occurs.

If the implementing method is non-null, then

- if expression.Type is assignable to the argument type of the implementing method, the node is non-lifted, and the node type is the return type of the implementing method.

- If the following is true, then the UnaryExpression's Type is lifted:

- expression.Type is a nullable value types

- the corresponding non-nullable type is equal to the corresponding argument type of the implementing method

- the return type of the implementing method is non-nullable value type

> Also, the factories then lift the node type to the corresponding nullable type for the method's return type.

- Otherwise an exception occurs.

If the implementing method is null, then expression.Type is a numeric type. The node type is then expression.Type. Furthermore

- if expression.Type is non-nullable, the node is non-lifted.

- Otherwise the node is lifted.

The resulting UnaryExpression has:

- node kind UnaryPlus or Negate

- Operand set to expression

- Type set as described above

- Method set to the implementing method

If the node is lifted as described above, IsLifted and IsLiftedToNull are true; otherwise, they are false.

<h3 id="logical-and-bit-negation-factory-methods">4.8.12 Logical and Bit Negation Factory Methods</h3>

Expression has the following factory methods for UnaryExpressions representing logical and bit negation operations:

public static UnaryExpression Not(Expression expression);

public static UnaryExpression Not(Expression expression,

MethodInfo method);

public static UnaryExpression OnesComplement

(Expression expression)

public static UnaryExpression OnesComplement

(Expression expression, MethodInfo method)

The following is derived from the v1 spec ... with updates for correctness or new behaviors

Expression must be non-null. If a non-null method is supplied, that becomes the implementing method for this node. The method must represent a non-void static method with one argument, or an exception occurs.

Otherwise, if expression.Type contains a user definition of a unary not operator (in the form of a static non-void op\_LogicalNot or op\_OnesComplement method with one argument), the MethodInfo representing that method becomes the implementing method. The factories look first for logical then for bitwise op\_... methods. Otherwise, if expression.Type is a numeric or boolean type, the implementing method is null. Otherwise, an exception occurs.

For legacy purposes, the Not factory still uses the Not node kind for a bitwise negation. The OneComplement factory uses the OnesComplement node kind. We introduced the new node kind for DLR interoperability in the DynamicMetaObject protocol, and for meta-programming scenarios where the node's intent is readily manifest.

If the implementing method is non-null, then

- if expression.Type is assignable to the argument type of the implementing method, the node is non-lifted, and the node type is the return type of the implementing method.

- If the following is true, then the UnaryExpression's Type is lifted:

- expression.Type is a nullable value types

- the corresponding non-nullable type is equal to the corresponding argument type of the implementing method

- the return type of the implementing method is non-nullable value type

> Also, the factories then lift the node type to the corresponding nullable type for the method's return type.

- Otherwise an exception occurs.

If the implementing method is null, then expression.Type may be numeric, Boolean, or user-defined type. The node type is then expression.Type. If the type is numeric or Boolean, then:

- if expression.Type is non-nullable, the node is non-lifted.

- Otherwise the node is lifted.

The resulting UnaryExpression has:

- node kind Not if you called Not, OnesComplement if you called OnesComplement

- Operand set to expression

- Type set as described above

- Method set to the implementing method

If the node is lifted as described above, IsLifted and IsLiftedToNull are true; otherwise, they are false.

<h3 id="istrue-and-isfalse-factories">4.8.13 IsTrue and IsFalse Factories</h3>

Expression has the following factory methods for UnaryExpressions representing testing if a value is a true value or a false value:

public static UnaryExpression IsFalse(Expression expression) {

public static UnaryExpression IsFalse(Expression expression,

MethodInfo method) {

public static UnaryExpression IsTrue(Expression expression) {

public static UnaryExpression IsTrue(Expression expression,

MethodInfo method) {

Expression must be non-null. If a non-null method is supplied, that becomes the implementing method for this node. The method must represent a non-void static method with one argument, or an exception occurs.

Otherwise, if expression.Type contains a user definition of a unary op\_false or op\_true operator, respectively, the MethodInfo representing that method becomes the implementing method. The user-defined type must also implement the op\_BitwiseAnd operator method if it implements op\_False, and op\_BitwiseOr if it implements op\_True. Otherwise, if expression.Type implicitly converts to boolean, the implementing method is null. Otherwise, an exception occurs.

If the implementing method is non-null, then

- if expression.Type is assignable to the argument type of the implementing method, the node is non-lifted, and the node type is the return type of the implementing method.

- If the following is true, then the UnaryExpression's Type is lifted:

- expression.Type is a nullable value types

- the corresponding non-nullable type is equal to the corresponding argument type of the implementing method

- the return type of the implementing method is non-nullable value type

> Also, the factories then lift the node type to the corresponding nullable type for the method's return type.

- Otherwise an exception occurs.

If the implementing method is null, then expression.Type may be implicitly convertible to Boolean, or user-defined type. The node type is then expression.Type.

The resulting UnaryExpression has:

- node kind IsTrue or IsFalse

- Operand set to expression

- Type set as described above

- Method set to the implementing method

If the node is lifted as described above, IsLifted and IsLiftedToNull are true; otherwise, they are false.

<h3 id="quote-factory-method">4.8.14 Quote Factory Method</h3>

Expression has the following factory methods for UnaryExpressions representing a specialized quoting operation for LINQ expressions:

public static UnaryExpression Quote(Expression expression);

The following is derived from the v1 spec ... with a correction to the constraint on the expression parameter and usage.

It would be rare that an ET producer would need to use this factory or node kind. It exists with special semantics for use in LINQ v1 language features. In v-next+1 we'll consider a complete quasi-quoting model. See section for more information.

Expression must be non-null. Expression.Type must be Expression&lt;T&gt;, where T is a delegate type.

The resulting UnaryExpression has:

- node kind Quote

- Operand set to expression

- Type set to expression.Type.

- Method as null

- IsLifted and IsLiftedToNull as false

<h3 id="throw-flow-control-factory-mehtods">4.8.15 Throw Flow Control Factory Mehtods</h3>

Expression has the following factory methods for UnaryExpressions representing dynamic flow control:

public static UnaryExpression Rethrow();

public static UnaryExpression Rethrow(Type type);

public static UnaryExpression Throw(Expression value, Type type);

public static UnaryExpression Throw(Expression value);

Passing null for the value is equivalent to calling the Rethrow factory. This is for convenience.

The factories take a type even though of course a throw never returns a result. Allowing the type to be other than void means you can use these expressions in value positions where the sub expression's type must match the containing expression's type. For example, you could construct "condition ? foo : throw(e)". Otherwise, you'd have to awkwardly construct a Block to hold the Throw so that you could fake matching the type.

Value.Type must represent a type that is not a value type.

These factories result in nodes with node kind Throw.

<h3 id="reference-conversion-factory-methods">4.8.16 Reference Conversion Factory Methods</h3>

Expression has the following factory methods for UnaryExpressions representing reference conversion operations:

public static UnaryExpression TypeAs(Expression expression,

Type type);

Expression and type must be non-null, and type must represent a reference type or nullable type.

The resulting UnaryExpression has:

- node kind TypeAs

- Operand set to expression

- Type set to type

- Method is null

- IsLifted and IsLiftedToNull are false

<h3 id="unboxing-as-pointer-to-boxs-value-factory-method">4.8.17 Unboxing (as Pointer to Box's Value) Factory Method</h3>

Expression has the following factory methods for UnaryExpressions representing unboxing (to an interior pointer) operations:

public static UnaryExpression Unbox(Expression expression,

Type type);

The resulting interior pointer from this node has a the type represented by the type argument, which becomes the Unbox node's Type property. The type argument must represent the boxed value's value type. The operand expression's Type property must represent an interface type or type Object; a value type would not be boxed for any reason other than to be passed or assigned to an interface type or type Object. If the operand's Type property does not represent the type Object, then the Unbox node's Type property must represent a type that implements the operand's interface type.

The resulting node has:

- Node kind Unbox

- Operand set to expression

- Type set to type

- Method set to null

- IsLifted and IsLiftedToNull set to false

<h3 id="general-factory-methods-1">4.8.18 General Factory Methods</h3>

Expression has the following general factory methods for UnaryExpressions:

public static UnaryExpression MakeUnary

(ExpressionType unaryType, Expression operand, Type type);

public static UnaryExpression MakeUnary

(ExpressionType unaryType, Expression operand, Type type,

MethodInfo method);

The following is derived from the v1 spec ...

Based on the value of unaryType, MakeUnary returns the result of calling the corresponding factory method above with operand, type, and method (where applicable). If unaryType does not correspond to one of these factory methods, an ArgumentException will be thrown.

All the requirements and guarantees of the called factory method apply.

<h3 id="newdelegate-expression-v-next1">4.8.19 NewDelegate Expression (V-next+1)</h3>

Cut from .NET 4.0, planned for v-next+1.

We plan to add support for creating delegate values in v-next+1. For example, if you want to represent this expression:

> new MyDelegate(obj.MyMethod);

You would need to generate a call to Delegate.CreateDelegate:

    Expression.Convert

(Expression.Call

(typeof(Delegate)

.GetMethod("CreateDelegate",

new\[\] { typeof(Type), typeof(object),

typeof(MethodInfo) }),

                  Expression.Constant(typeof(MyDelegate)),

                  Expression.Constant(myObj, typeof(object)),

                  Expression.Constant(myObj.GetType()

.GetMethod("MyMethod"))),

            typeof(MyDelegate))

Creating an ET like the following would be much easier:

       Expression.NewDelegate

(typeof(MyDelegate),

           Expression.Constant(myObj, typeof(object)),

            myObj.GetType().GetMethod("MyMethod"))

Compilation would need to finesse the MethodInfo pointer to an IntPtr for the delegate type's constructor methods (using the efficient ldftn IL instruction).

<h2 id="blockexpression-class">4.9 BlockExpression Class</h2>

This class represents a sequence of expressions. Each expression executes in order, squelching the result of each except the last expression. The value of the Block node is the result of the last expression in the body, ignoring any exits via GotoExpression. The BlockExpression uses the Block node kind.

If the BlockExpression.Type represents a type that is void, then the result of the last expression is automatically "converted to void" or squelched. If the Block's type is other than void, then the last expression's Type property must represent a type that is reference assignable to the block's type.

Each block also has a collection of variables whose scope is the body of the block. Ignoring closures, the variables' lifetimes are limited to the block.

The automatic conversion to void for the last expression also provides a simple work around for converting to void. The Convert factories throw an exception if you try to convert to void. To convert to void so that you squelch the result of an expression (that is, execute it only for side-effects), you can use: BlockExpression: Expression.Block(typeof(void), expr).

Variables must be listed using ParameterExpressions as lexical variables in a BlockExpression to (in effect) define them in some sub tree. To reference a variable, you alias the ParameterExpression object used to define the variable. Note, while Parameter node references are what determine variable binding, you can declare the same Parameter object in nested BlockExpressions. The ET compiler resovles the references to the tightest containing Block that declares the Parameter.

See section for more details on the semantics of BlockExpressions, especially for unique binding semantics and guarantees (for example, within a loop).

<h3 id="class-summary-5">4.9.1 Class Summary</h3>

public sealed class BlockExpression : Expression {

public ReadOnlyCollection&lt;Expression&gt;

Expressions { get; }

public Expression Result { get; }

public ReadOnlyCollection&lt;ParameterExpression&gt; Variables { get; }

public BlockExpression Update

(IEnumerable&lt;ParameterExpression&gt; variables,

IEnumerable&lt;Expression&gt; expressions)

<h3 id="expressions-property">4.9.2 Expressions Property</h3>

This property returns the collection of expressions that form the body of the block.

Signature:

public ReadOnlyCollection&lt;Expression&gt;

Expressions { get; }

<h3 id="result-property">4.9.3 Result Property</h3>

This property is a convenience for accessing the last expression in this block's Expressions collection.

Signature:

public Expression Result { get; }

<h3 id="variables-property">4.9.4 Variables Property</h3>

This property returns the collection of variables scoped to this BlockExpression.

Signature:

public ReadOnlyCollection&lt;ParameterExpression&gt;Variables { get; }

<h3 id="update-method-3">4.9.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public BlockExpression Update

(IEnumerable&lt;ParameterExpression&gt; variables,

IEnumerable&lt;Expression&gt; expressions)

<h3 id="factory-methods-2">4.9.6 Factory Methods</h3>

The Expression class has the following factory methods for BlockExpression:

public static BlockExpression Block

(IEnumerable&lt;ParameterExpression&gt; variables,

params Expression\[\] expressions);

public static BlockExpression Block

(Type type,

IEnumerable&lt;ParameterExpression&gt; variables,

params Expression\[\] expressions);

public static BlockExpression Block

(IEnumerable&lt;ParameterExpression&gt; variables,

IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(Type type,

IEnumerable&lt;ParameterExpression&gt; variables,

IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(Type type,

IEnumerable&lt;Expression&gt; expressions);

public static BlockExpression Block

(Expression arg0, Expression arg1, Expression arg2,

Expression arg3, Expression arg4);

public static BlockExpression Block

(params Expression\[\] expressions);

public static BlockExpression Block

(Type type, params Expression\[\] expressions);

public static BlockExpression Block(Expression arg0,

Expression arg1);

public static BlockExpression Block

(Expression arg0, Expression arg1, Expression arg2);

public static BlockExpression Block

(Expression arg0, Expression arg1, Expression arg2,

Expression arg3);

When type is supplied, if it is void, then the last expression's result in the block does not have to be assignable to type, and the result is automatically "converted to void" or squelched. If type is supplied as non-void, then the last expression's Type must represent a type that is reference-assignable to the block's type. When type is not supplied, the block's Type property is set to the last expression's Type property.

No variables element may be null. If any element is duplicated, the factory throws an exception.

**Post CLR 4.0 (remove IsByRef error check):** If any of the ParameterExpressions representing the local variables has IsByRef set to True, then you must initialize them with an AssignRef BinaryExpression node. Otherwise, if the Body of the Block references the variable, or the variable appears as the Left expression of an Assign BinaryExpression node, there will be compile time error (or possibly an error when the code executes).

<h2 id="constantexpression-class">4.10 ConstantExpression Class</h2>

This class models a literal constant in code. Its value is the Value object. Its node kind is Constant. A ConstantExpression may have any Value, and the value may not have any syntactic representation in any programming language.

Note, when using this node, if the Value is not serializable (more specifically, compilable to IL), then the ET containing this node will not be serializable.

<h3 id="class-summary-6">4.10.1 Class Summary</h3>

public class ConstantExpression : Expression {

public Object Value { get; }

<h3 id="value-property">4.10.2 Value Property</h3>

This property returns the expression that models the value of the constant expression.

<h3 id="factory-methods-3">4.10.3 Factory Methods</h3>

Expression has the following factory methods for ConstantExpressions:

public static ConstantExpression Constant(Object value);

public static ConstantExpression Constant(Object value, Type type);

If type is supplied, it must be non-null. If value is not null, type must be assignable from the actual run-time type of value.

The resulting ConstantExpression has:

- Node kind Constant

- Value set to value

- Type set to type if supplied. If value is not null, then Type is value's type. If value is null, Type is Object.

<h2 id="conditionalexpression-class">4.11 ConditionalExpression Class</h2>

This class represents an if-then-else for value. The node kind is Conditional. See section for more details on the semantics.

<h3 id="class-summary-7">4.11.1 Class Summary</h3>

public sealed class ConditionalExpression : Expression {

public Expression IfFalse { get; }

public Expression IfTrue { get; }

public Expression Test { get; }

public ConditionalExpression Update

(Expression test, Expression ifTrue, Expression ifFalse)

<h3 id="iffalse-property">4.11.2 IfFalse Property</h3>

This property returns the expression to evaluate if the Test expression results in false.

Signature:

public Expression IfFalse { get; }

<h3 id="iftrue-property">4.11.3 IfTrue Property</h3>

This property returns the expression to evaluate if the Test expression results in false.

Signature:

public Expression IfTrue { get; }

<h3 id="test-property">4.11.4 Test Property</h3>

This property returns the expression to evaluate as the Test of the condition, determining which sub expression control flows to next.

Signature:

public Expression Test { get; }

<h3 id="update-method-4">4.11.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public ConditionalExpression Update

(Expression test, Expression ifTrue, Expression ifFalse)

<h3 id="factory-methods-4">4.11.6 Factory Methods</h3>

Expression has the following factory methods for ConditionalExpressions:

public static ConditionalExpression Condition

(Expression test, Expression ifTrue, Expression ifFalse);

public static ConditionalExpression Condition

(Expression test, Expression ifTrue, Expression ifFalse,

Type type);

public static ConditionalExpression IfThen

(Expression test, Expression ifTrue)

public static ConditionalExpression IfThenElse

(Expression test, Expression ifTrue, Expression ifFalse);

Test, ifTrue, and ifFalse must all be non-null. Test.Type must represent the bool type. If you do not supply the type argument, then IfTrue.Type and IfFalse.Type must represent the same type. If you do supply type, and it is not void, then IfTrue.Type and IfFalse.Type must both be reference-assignable to the supplied type. If type is void, then the sub expression types do not have to match, and any resulting value is "converted to void" or squelched.

The resulting ConditionalExpression has:

- Test, IfTrue, and IfFalse properties set to test, ifTrue and ifFalse, respectively

- Node kind Conditional

- Type set to ifTrue.Type if type is not supplied

<h2 id="dynamicexpression-class">4.12 DynamicExpression Class</h2>

This class represents a dynamic operation that must be bound at runtime of the expression tree. The semantics is to create a DLR CallSite for caching of implementations of the operation given the different kinds of objects passed to the CallSite during the program's execution. The Dynamic node has a DLR CallSiteBinder that determines the exact semantics of the operation given the runtime operands. The binder encapsulates the language semantics for the creator of the node, as well as any payload meta data that informs the binder how to compute an implementation of the operation at runtime.

For more on expected semantics, see other node kinds' statements as to their semantics in DynamicExpression nodes. You might also see the sub classes of DynamicMetaDataBinder and language documentation on their dynamic semantics. See the documents at on the [DLR Codeplex](http://www.codeplex.com/dlr/Wiki/View.aspx?title=Docs%20and%20specs&referringTitle=Home) site, sites-binders-dynobj-interop.doc and library-authors-introduction.doc.

To enable ET consumers to meta-program with ETs, languages should provide and document factories for their binders. For example, if you are rewriting a static operation into a dynamic one, or breaking a dynamic operation into two sub operations, you'll need to call the Expression.Dynamic factory. You'll need a CallSiteBinder from the language that captures the semantics you want, and you'll need to supply the binder with the appropriate payload meta data it needs to compute how to perform the operation at runtime.

<h3 id="class-summary-8">4.12.1 Class Summary</h3>

public class DynamicExpression : Expression {

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

public CallSiteBinder Binder { get; }

public Type DelegateType { get; }

public DynamicExpression Update(IEnumerable&lt;Expression&gt; arguments)

<h3 id="arguments-property">4.12.2 Arguments Property</h3>

This property returns the argument expressions that are the operands to the operation.

Signature:

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

<h3 id="binder-property">4.12.3 Binder Property</h3>

This property returns the binder that the DLR calls at runtime to compute an implementation of the operation given the runtime types of the operands. The binder may include private payload meta data that further refines it computation (for example, whether an integer operation should throw on overflow or implicitly roll over to infinite precision integers.

Signature:

public CallSiteBinder Binder { get; }

<h3 id="delegatetype">4.12.4 DelegateType</h3>

This property returns the delegate used to construct the CallSite&lt;T&gt; that manages the caching of dispatch rules for this operation. The first argument of the delegate type's Invoke method must be of type CallSite.

Signature:

public Type DelegateType { get; }

<h3 id="update-method-5">4.12.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public DynamicExpression Update(IEnumerable&lt;Expression&gt; arguments)

<h3 id="factories">4.12.6 Factories</h3>

The Expression class has the following factories for creating DynamicExpressions:

public static DynamicExpression Dynamic

(CallSiteBinder binder, Type returnType, Expression arg0,

Expression arg1);

public static DynamicExpression Dynamic

(CallSiteBinder binder, Type returnType, Expression arg0,

Expression arg1, Expression arg2);

public static DynamicExpression Dynamic

(CallSiteBinder binder, Type returnType, Expression arg0,

Expression arg1, Expression arg2, Expression arg3);

public static DynamicExpression Dynamic

(CallSiteBinder binder, Type returnType,

IEnumerable&lt;Expression&gt; arguments);

public static DynamicExpression Dynamic

(CallSiteBinder binder, Type returnType,

params Expression\[\] arguments);

public static DynamicExpression Dynamic

(CallSiteBinder binder, Type returnType, Expression arg0);

public static DynamicExpression MakeDynamic

(Type delegateType, CallSiteBinder binder,

IEnumerable&lt;Expression&gt; arguments);

public static DynamicExpression MakeDynamic

(Type delegateType, CallSiteBinder binder,

params Expression\[\] arguments);

public static DynamicExpression MakeDynamic

(Type delegateType, CallSiteBinder binder, Expression arg0)

public static DynamicExpression MakeDynamic

(Type delegateType, CallSiteBinder binder, Expression arg0,

Expression arg1)

public static DynamicExpression MakeDynamic

(Type delegateType, CallSiteBinder binder, Expression arg0,

Expression arg1, Expression arg2)

public static DynamicExpression MakeDynamic

(Type delegateType, CallSiteBinder binder, Expression arg0,

Expression arg1, Expression arg2, Expression arg3)

If delegateType is supplied, the first argument to the delegate's Invoke method must be of type CallSite, and the parameter count must equal the number of arguments expressions supplied plus one.

<h2 id="callinfo-class">4.13 CallInfo Class</h2>

This class represents argument information at DynamicExpression call sites. The information is in DLR binder objects (for example, InvokeMemberBinder, CreateInstanceBinder, GetIndexBinder).

This class is in the System.Dynamic namespace since it is only used in DynamicExpression binders.

<h3 id="class-summary-9">4.13.1 Class Summary</h3>

public sealed class CallInfo {

public int ArgumentCount { get; }

public ReadOnlyCollection&lt;string&gt; ArgumentNames {get; }

<h3 id="argumentcount-property">4.13.2 ArgumentCount Property</h3>

This property returns the total number of argument expressions. For example "foo(1, 2, bar=3, baz=4)" has a count of four.

Signature:

public int ArgumentCount { get; }

<h3 id="argumentnames-property">4.13.3 ArgumentNames Property</h3>

This property return the names used for any named arguments. If there are N names, then they are the names used in the last N argument expressions. For example "foo(1, 2, bar=3, baz=4)" has a collection of "bar" and "baz".

Signature:

public ReadOnlyCollection&lt;string&gt; ArgumentNames {get; }

<h3 id="factory-methods-5">4.13.4 Factory Methods</h3>

The Expression class has the following factory methods for creating CallInfos:

public static CallInfo CallInfo(int argCount,

params string\[\] argNames)

public static CallInfo CallInfo(int argCount,

IEnumerable&lt;string&gt; argNames)

<h2 id="debuginfoexpression-class">4.14 DebugInfoExpression Class</h2>

This class represents a point in an ET where there is debugging information (a la .NET sequence points). To clear the sequence point information, use an instance of this class with IsClear set to True.

<h3 id="class-summary-10">4.14.1 Class Summary</h3>

public sealed class DebugInfoExpression : Expression {

public SymbolDocumentInfo Document { get; }

public virtual Int32 EndColumn { get; }

public virtual Int32 EndLine { get; }

public virtual Int32 StartColumn { get; }

public virtual Int32 StartLine { get; }

public virtual bool IsClear { get; }

<h3 id="document-property">4.14.2 Document Property</h3>

This property returns an object that contains information about the source code file for this sequence point.

<h3 id="startline-property">4.14.3 StartLine Property</h3>

This property returns the starting line number in the file for this sequence point. This value is one-based, and it is an inclusive bound.

<h3 id="startcolumn-property">4.14.4 StartColumn Property</h3>

This property returns the starting column for this sequence point. This value is one-based, and it is an inclusive bound. (The underlying call in the .NET BCL is documented incorrectly.)

<h3 id="endline-property">4.14.5 EndLine Property</h3>

This property returns the ending line number in the file for this sequence point. This value is one-based and must be greater than or equal to the start line.

<h3 id="endcolumn-property">4.14.6 EndColumn Property</h3>

This property returns the ending column for this sequence point. This value is one-based, and it is an exlusive bound. (The underlying call in the .NET BCL is documented incorrectly.) EndColumn should be greater than the StartColumn if StartLine equals EndLine.

The .NET 4.0 and Codeplex v1 DLR code only checks that the EndColumn is greater than or equal to the StartColumn when they are on the same line. The underlying .NET BCL call does no checking, just stored the data.

<h3 id="isclear-property">4.14.7 IsClear Property</h3>

This property returns False if the node sets debugging sequence point information, and it returns True if it clears the debugging information.

Signature:

public bool IsClear { get; }

<h3 id="factory-methods-6">4.14.8 Factory Methods</h3>

The Expression class has the following factory methods for creating DebugInfoExpressions:

public static DebugInfoExpression DebugInfo

(SymbolDocumentInfo document, Int32 startLine,

Int32 startColumn, Int32 endLine, Int32 endColumn);

public static DebugInfoExpression ClearDebugInfo

(SymbolDocumentInfo document)

<h2 id="symboldocumentinfo-class">4.15 SymbolDocumentInfo Class</h2>

This class represents document information for debugging sequence point data. See DebugInfoExpression and <http://msdn.microsoft.com/en-us/library/system.diagnostics.symbolstore.isymboldocument.aspx> .

<h3 id="class-summary-11">4.15.1 Class Summary</h3>

public class SymbolDocumentInfo {

public Guid DocumentType { get; }

public String FileName { get; }

public Guid Language { get; }

public Guid LanguageVendor { get; }

<h3 id="documenttype-property">4.15.2 DocumentType Property</h3>

This property returns the documents unique type identifier. It defaults to the guid for a text file.

Signature:

public Guid DocumentType { get; }

<h3 id="filename-property">4.15.3 FileName Property</h3>

This property returns the source file name.

Signature:

public String FileName { get; }

<h3 id="language-property">4.15.4 Language Property</h3>

This property returns the language's unique identifier, if any.

Signature:

public Guid Language { get; }

<h3 id="languagevendor-property">4.15.5 LanguageVendor Property</h3>

This property returns the language vendor's unique identifier, if any.

Signature:

public Guid LanguageVendor { get; }

<h3 id="factory-methods-7">4.15.6 Factory Methods</h3>

The Expression class has the following factory methods for creating SymbolDocumentInfos:

public static SymbolDocumentInfo SymbolDocument(String fileName,

Guid language);

public static SymbolDocumentInfo SymbolDocument(String fileName);

public static SymbolDocumentInfo SymbolDocument

(String fileName, Guid language, Guid languageVendor);

public static SymbolDocumentInfo SymbolDocument

(String fileName, Guid language, Guid languageVendor,

Guid documentType);

If documentType is not supplied, it defaults to the guid indicating text. If language or languageVendor are not supplied, they default to Guid.Empty.

See MSDN for more information on the members/parameters: and <http://msdn.microsoft.com/en-us/library/system.diagnostics.symbolstore.isymboldocument.aspx> .

<h2 id="tryexpression-class">4.16 TryExpression Class</h2>

This class represents represents try-catch control flow, including finally and fault blocks. TryExpression uses this node kind.

First the Body executes. Under normal execution, control flows from the body to the Finally expression, if any. The value of the TryExpresson is the Body expression's value. If an object is thrown (directly or indirectly) from within the Body, and there's no dynamically intervening appropriate catch handler, the CatchBlocks are considered in order to find one with a Test type that is assignable from the thrown object. If there is such a CatchBlock, execution flows to its Body and then to the Finally expression, if any. The CatchBlock's body produces the value for the TryExpression in this case.

If non-null, the Finally expression always executes regardless of dynamic flow of control to or through the TryExpression.

If Fault is non-null, then Finally is null, and Handlers is empty. The Fault expresson executes if an object is thrown from the Body, directly or indirectly. Control flows from the Fault expression to some appropriate catch handler, or possibly an process unhandled throw error block. If execution flows from the Body expression to the Finally expression, if any, and no objects are thrown, then the Fault expression does NOT execute.

<h3 id="class-summary-12">4.16.1 Class Summary</h3>

public sealed class TryExpression : Expression {

public Expression Body { get; }

public Expression Fault { get; }

public Expression Finally { get; }

public ReadOnlyCollection&lt;CatchBlock&gt;

Handlers { get; }

public TryExpression Update(Expression body,

IEnumerable&lt;CatchBlock&gt; handlers,

Expression @finally, Expression fault)

<h3 id="body-property">4.16.2 Body Property</h3>

This property returns the expression to execute upon entering the TryExpression. This expression provides the value for the TryExpression, unless dynamic flow of control lands in a CatchBlock.

Signature:

public Expression Body { get; }

<h3 id="fault-property">4.16.3 Fault Property</h3>

This property returns the Expresson that executes if an object is thrown from the Body, directly or indirectly. Control flows from the Fault to other code that catches the object. Note, if Fault is non-null, then Finally is null, and Handlers is empty.

Signature:

public Expression Fault { get; }

<h3 id="finally-property">4.16.4 Finally Property</h3>

This property returns the Expression that executes either when the body completes or when a selected CatchBlock completes. The Finally expresson executes regardless of whether an object is thrown while the Body executes.

Signature:

public Expression Finally { get; }

<h3 id="handlers-property">4.16.5 Handlers Property</h3>

This property returns the collection of CatchBlocks to consider executing if an object is thrown while Body executes.

Signature:

public ReadOnlyCollection&lt;CatchBlock&gt;

Handlers { get; }

<h3 id="update-method-6">4.16.6 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public TryExpression Update(Expression body,

IEnumerable&lt;CatchBlock&gt; handlers,

Expression @finally, Expression fault)

<h3 id="factory-methods-8">4.16.7 Factory Methods</h3>

These methods create expressions that represent try-catch control flow constructs.

Signatures:

public static TryExpression TryCatch

(Expression body, params CatchBlock\[\] handlers);

public static TryExpression TryCatchFinally

(Expression body, Expression finally,

params CatchBlock\[\] handlers);

public static TryExpression TryFault(Expression body,

Expression fault);

public static TryExpression TryFinally(Expression body,

Expression finally);

public static TryExpression MakeTry

(Type type, Expression body, Expression finally,

Expression fault, IEnumerable&lt;CatchBlock&gt; handlers);

Body represents the instructions to execute under the try Expression.

Handlers is a collection of CatchBlocks, where control flows if one of their exception Test types is assignable from a thrown exception from within the body. The cases and tests within cases are executed and checked in the order they appear in the lists.

Finally is the expression to execute before flow of control exits the TryExpression, regardless of non-local exits.

Fault is the expression to execute when an exception occurs. If fault is supplied, there can be no finally supplied nor any cases. If there is no exception thrown, the fault expression does not execution, while a finally expression always executes.

Note: handlers can catch non-exception types, and Throw can throw them as well.

If type is not supplied, or supplied as non-void, then Body.Type and each CatchBlock.Type must be reference-assignable to the type. If type is not supplied, then the TryExpression.Type is Body.Type. When type is supplied as void, the types of sub expressions do not matter, and the any resulting value is "converted to void" or squelched. If the Body.Type is void, then every CatchBlock.Type must be void or have a null CatchBlock.Body.

<h2 id="catchblock-class">4.17 CatchBlock Class</h2>

This class represents represents catch clauses for a TryExpression. These are not expressions because they cannot occur anywhere in an ET. They can only appear within a TryExpression. See the Handlers property of TryExpression for more information.

Note, the Variable effectively creates a lexical scope for the catch block, so re-using a ParameterExpression here that you've used in a containing BlockExpression or LambdaExpression will shadow those variables in the outer lexical scopes.

<h3 id="class-summary-13">4.17.1 Class Summary</h3>

public sealed class CatchBlock {

public Expression Body { get; }

public Expression Filter { get; }

public Type Test { get; }

public ParameterExpression Variable { get; }

public CatchBlock Update(ParameterExpression variable,

Expression filter, Expression body)

<h3 id="body-property-1">4.17.2 Body Property</h3>

The property returns the expression to execute if exeution transfers to this CatchBlock, which depends on Test and Fitler.

Signature:

public Expression Body { get; }

<h3 id="filter-property">4.17.3 Filter Property</h3>

This property returns the Filter expression that (if non-null) must evaluate to true for this CatchBlock to be chosen when looking for a catch handler.

Signature:

public Expression Filter { get; }

<h3 id="test-property-1">4.17.4 Test Property</h3>

This property returns the type of thrown object this CatchBlock handles.

Signature:

public Type Test { get; }

<h3 id="variable-property">4.17.5 Variable Property</h3>

This property returns the ParameterExpression that represents the variable that will be bound to the thrown object for purposes of executing the Filter expression and Body expressions.

Signature:

public ParameterExpression Variable { get; }

<h3 id="update-method-7">4.17.6 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public CatchBlock Update(ParameterExpression variable,

Expression filter, Expression body)

<h3 id="factories-1">4.17.7 Factories</h3>

The Expression class has the following factories for creating CatchBlocks:

public static CatchBlock Catch(ParameterExpression variable,

Expression body);

public static CatchBlock Catch(Type type, Expression body);

public static CatchBlock Catch

(ParameterExpression variable, Expression body,

Expression filter);

public static CatchBlock Catch(Type type, Expression body,

Expression filter);

public static CatchBlock MakeCatchBlock

(Type type, ParameterExpression variable, Expression body,

Expression filter)

Then type is not supplied, the CatchBlock handles objects thrown of type represented by variable.Type.

Variable must not be IsByRef. Its Type property must be the same as that supplied in the type parameter.

If filter is supplied, its Type property must represent Boolean.

<h2 id="methodcallexpression-class">4.18 MethodCallExpression Class</h2>

This class represents static and instance method calls. Its node kind is Call.

If you want to invoke a callable object, such as applying a delegate or LambdaExpression to a list of arguments, then you should use an InvocationExpression.

There is a factory method that uses this node type to represent fetching an element from a multi-dimensional array (still has node kind Call). It will be obsolete in a future version. You should use IndexExpression to get and set array elements.

See section for more details on the semantics of the Call node kind.

<h3 id="class-summary-14">4.18.1 Class Summary</h3>

public class MethodCallExpression : Expression {

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

public MethodInfo Method { get; }

public Expression Object { get; }

public MethodCallExpression Update(Expression @object,

IEnumerable&lt;Expression&gt; arguments)

<h3 id="arguments-property-1">4.18.2 Arguments Property</h3>

This property returns the read-only collection of argument expressions. If there are no arguments, this is an empty collection.

Signature:

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

<h3 id="method-property-2">4.18.3 Method Property</h3>

This property returns the MethodInfo representing the method to call, and it is guaranteed to be non-null.

If the MethodInfo represents a static method, then Object returns null.

To be obsolete in a future version, if the node represents fetching an element from a multi-dimensional array, then this property returns a MethodInfo for a public instance method named Get on the Object.Type type.

Signature:

public MethodInfo Method { get; }

<h3 id="object-property">4.18.4 Object Property</h3>

This property returns the expression that models the target object that is the self argument for the method call. If the call is to a static method, then this returns null.

To be obsolete in a future version, if the node represents fetching an element from a multi-dimensional array, then this property returns the array expression.

Signature:

public Expression Object { get; }

<h3 id="update-method-8">4.18.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public MethodCallExpression Update(Expression @object,

IEnumerable&lt;Expression&gt; arguments)

<h3 id="general-call-factories">4.18.6 General Call Factories</h3>

Expression has the following general call factory methods for MethodCallExpressions:

public static MethodCallExpression Call

(MethodInfo method, params Expression\[\] arguments);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1,

Expression arg2, Expression arg3, Expression arg4);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1);

public static MethodCallExpression Call

(Expression instance, MethodInfo method,

params Expression\[\] arguments);

public static MethodCallExpression Call

(Expression instance, MethodInfo method, Expression arg0,

Expression arg1, Expression arg2);

public static MethodCallExpression Call

(Expression instance, MethodInfo method, Expression arg0,

Expression arg1);

public static MethodCallExpression Call(MethodInfo method,

Expression arg0);

public static MethodCallExpression Call

(Expression instance, String methodName, Type\[\] typeArguments,

params Expression\[\] arguments);

public static MethodCallExpression Call

(Type type, String methodName, Type\[\] typeArguments,

params Expression\[\] arguments);

public static MethodCallExpression Call

(Expression instance, MethodInfo method,

IEnumerable&lt;Expression&gt; arguments);

public static MethodCallExpression Call

(MethodInfo method,

IEnumerable&lt;Expression&gt; arguments);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1,

Expression arg2, Expression arg3);

public static MethodCallExpression Call

(MethodInfo method, Expression arg0, Expression arg1,

Expression arg2);

public static MethodCallExpression Call(Expression instance,

MethodInfo method);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

Method must be non-null. If method is an instance method, instance must be supplied as non-null. Instance's Type property must be assignable to the declaring type of the member represented by method. If the method is a static method, instance must be null (breaking bug fix from v1); otherwise, the factories throw an ArgumentException.

If arguments is omitted or null, there are no arguments. If provided, arguments must have the same number of elements as the number of parameters for the method. Each of the elements of arguments must be non-null, and the types of the values they represent must be assignable to the type of the corresponding parameter of method. There is a special case the factory handles when an element of arguments has a Type property representing a type that is not assignable to the corresponding parameter type. If the parameter's type is a sub type of LambdaExpression, and the argument Expression object itself (that is, the Expression node) is of a type that is assignable to the parameter's type, then the argument Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

Two overloads take methodName and resolve the MethodInfo for you. Language implementers or DLR CallSiteBinder implementers should never use these overloads since they may not have the same semantics of method resolution as your language. When using these overloads, instance, type, and methodName must not be null. These overloads search instance.Type (or type if you used that overload) and base types for methodName, case-INsensitively. Type parameters must match typeArguments, and parameter types must match argument expression types. If no method or more than one compatible method is found, these overloads throw an exception. Otherwise these overloads invoke Call with the instance (if supplied), the MethodInfo for the found method, and arguments to return a result.

The resulting MethodCallExpression has the Object and Method properties equal to instance and method, respectively. The Arguments property has the same elements as arguments, except that some elements may be wrapped in Quote nodes as described above. The Type property represents the return type of the method denoted by method. The NodeType property is Call.

<h3 id="obsolete-multi-dimensional-array-index-factory">4.18.7 Obsolete Multi-dimensional Array Index Factory</h3>

The ArrayIndex factories will be obsolete in lieu of the more general IndexExpression factory methods.

public static MethodCallExpression ArrayIndex

(Expression array, params Expression\[\] indexes);

public static MethodCallExpression ArrayIndex

(Expression array,

IEnumerable&lt;Expression&gt; indexes);

A MethodCallExpression can represent fetching an array element from an array with rank greater than one. The value of the Method property must be a MethodInfo describing the public instance method named Get on a multi-dimensional array type.

The following is derived from the v1 spec ...

Array and indices must be non-null. Array.Type must represent an array type, and its rank must match the number of elements in indices. For each Expression, E, in indices, E.Type must represent the int type.

The MethodCallExpression's Object property is the array. The Arguments property is the result of ReadOnlyCollectionExtensions.ToReadOnlyCollection(indices). The Method property is the MethodInfo describing the public instance method named Get on the type represented by array.Type.

<h2 id="post-clr-4.0----complexmethodcallexpression-class">4.19 POST CLR 4.0 -- ComplexMethodCallExpression Class</h2>

We have this node type to describe function calls with unsupplied arguments, named argument values, etc.

See section 2.10 for more info.

<h3 id="class-summary-15">4.19.1 Class Summary</h3>

public sealed class ComplexMethodCallExpression : MethodCallExpression {

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

public Expression\[\] ArgumentEvaluationOrder { get; }

public ArgumentDescription\[\] ArgumentDescriptions { get; }

<h2 id="invocationexpression-class">4.20 InvocationExpression Class</h2>

This class represents invoking callable objects. These nodes use the Invoke node kind. See section for more details on the semantics.

<h3 id="class-summary-16">4.20.1 Class Summary</h3>

public sealed class InvocationExpression : Expression {

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

public Expression Expression { get; }

public InvocationExpression Update

(Expression expression, IEnumerable&lt;Expression&gt; arguments)

<h3 id="arguments-property-2">4.20.2 Arguments Property</h3>

This property returns the read-only collection of argument expressions that produce values to pass to the Expression object. This never returns null, using an empty collection for no arguments.

Signature:

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

<h3 id="expression-property-1">4.20.3 Expression Property</h3>

This property returns the expression that models the callable object that is invoked on the Arguments to produce the result of this node. The expression represents either a System.Delegate or a LambdaExpression.

Signature:

public Expression Expression { get; }

<h3 id="update-method-9">4.20.4 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public InvocationExpression Update

(Expression expression, IEnumerable&lt;Expression&gt; arguments)

<h3 id="factory-methods-9">4.20.5 Factory Methods</h3>

Expression has the following factory methods for InvocationExpressions:

public static InvocationExpression Invoke

(Expression expression, params Expression\[\] arguments);

public static InvocationExpression Invoke

(Expression expression,

IEnumerable&lt;Expression&gt; arguments);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

Expression must be non-null. Expression.Type must represent a delegate type D or a type Expression&lt;D&gt; where D is a delegate type. The list of parameters for D must have the same length as arguments, or 0 if arguments is null. Each of the elements of arguments must be non-null, and the types of the values they represent must be assignable to the type of the corresponding parameter of D. There is a special case the factory handles when an element of arguments has a Type property representing a type that is not assignable to the corresponding parameter type. If the parameter's type is a sub type of LambdaExpression, and the argument Expression object itself (that is, the Expression node) is of a type that is assignable to the parameter's type, then the argument Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

The resulting InvocationExpression has the Expression property equal to expression. The Arguments property has the same elements as arguments, except that some elements may be wrapped in Quote nodes as described above. The Type property represents the return type of D.

Implementation note: the expression compiler may choose to inline the invocation of a lambda expression, rather than create a separate CLR method.

<h2 id="indexexpression-class">4.21 IndexExpression Class</h2>

This class represents array access and indexed properties (property gets that take arguments). This node uses the Index node kind. An IndexExpression is allowed as an l-value, for example, with BinaryExpression and node kind Assign.

There is redundant modeling for accessing arrays (not assignment) due to ETs v1 backward compatibility. For LINQ features, VB and C\# will continue to emit ETs with BinaryExpression (node kind ArrayIndex) for single dimension arrays and MethodCallExpression (method info is Array.Get) for mulit-dimensional arrays. VB and C\# will emit IndexExpression for l-value locations when they extend their lambda support in a future version.

<h3 id="class-summary-17">4.21.1 Class Summary</h3>

public class IndexExpression : Expression {

public ReadOnlyCollection&lt;Expression&gt; Arguments { get; }

public PropertyInfo Indexer { get; }

public Expression Object { get; }

public IndexExpression Update(Expression @object,

IEnumerable&lt;Expression&gt; arguments)

<h3 id="arguments-property-3">4.21.2 Arguments Property</h3>

Signature:

public ReadOnlyCollection&lt;Expression&gt; Arguments { get; }

<h3 id="indexer-property">4.21.3 Indexer Property</h3>

Signature:

public PropertyInfo Indexer { get; }

<h3 id="object-property-1">4.21.4 Object Property</h3>

Signature:

public Expression Object { get; }

<h3 id="update-method-10">4.21.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public IndexExpression Update(Expression @object,

IEnumerable&lt;Expression&gt; arguments)

<h3 id="factory-methods-10">4.21.6 Factory Methods</h3>

The Expressions class has the following factory methods for creating IndexExpressions:

public static IndexExpression Property

(Expression instance, PropertyInfo indexer,

IEnumerable&lt;Expression&gt; arguments);

public static IndexExpression Property

(Expression instance, PropertyInfo indexer,

params Expression\[\] arguments);

public static IndexExpression Property

(Expression instance, String propertyName,

params Expression\[\] arguments);

public static IndexExpression ArrayAccess

(Expression array, IEnumerable&lt;Expression&gt; indexes);

public static IndexExpression ArrayAccess

(Expression array, params Expression\[\] indexes);

public static IndexExpression MakeIndex

(Expression instance, PropertyInfo indexer,

IEnumerable&lt;Expression&gt; arguments);

Array must be an expression with a Type property representing an array (array.Type.IsArray is true). Indexes must all have a Type property that represents int32.

Indexer must not be null. Its PropertyType must not be a ByRef type, and it must not represent the void type. If Indexer represents as static property, then instance must be null, and vice versa.

Arguments must not be null, and it must not be empty. Arguments must have the same number of elements as the number of parameters for the property. Each of the elements of arguments must be non-null, and the types of the values they represent must be assignable to the type of the corresponding parameter of property. There is a special case the factory handles when an element of arguments has a Type property representing a type that is not assignable to the corresponding parameter type. If the parameter's type is a sub type of LambdaExpression, and the argument Expression object itself (that is, the Expression node) is of a type that is assignable to the parameter's type, then the argument Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

One overload takes propertyName and resolves the PropertyInfo for you. Language implementers or DLR CallSiteBinder implementers should never use this overload since it may not have the same semantics of resolution as your language. Instance and propertyName must not be null. This overload searches instance.Type and base types for propertyName, case-INsensitively. Parameter types must be reference assignable from corresponding argument expression types. If no method or more than one compatible method is found, this overload throws an exception.

<h2 id="loopexpression-class">4.22 LoopExpression Class</h2>

This class represents an infinite loop, executing its Body repeatedly until a sub expression of the body exits the loop via a GotoExpression.

Loop nodes have a Break property with a LabelTarget. When control transfers to this label with a value, the value becomes the result of the LoopExpression. The Break label can be null, in which case the Loop's Type property represents the void type.

LoopExpression.Type is the same as LoopExpression.Break.Type if the Break label is non-null.

<h3 id="examples">4.22.1 Examples</h3>

<h4 id="while-less-than-ten">4.22.1.1 While Less Than Ten</h4>

   // Counts up to 10 and breaks. Roughly:

   //   int i = 0;

   //   while (true) {

   //     if (i &lt; 123) ++i; else break;

   //   }

   var i = Expression.Variable(typeof(int), "i");

   var b = Expression.Label();

   var lambda = Expression.Lambda&lt;Action&gt;(

       Expression.Block(

           new\[\] { i },

Expression.Assign(I, Expression.Constant(0)),

           Expression.Loop(

               Expression.IfThenElse(

                   Expression.LessThan(i, Expression.Constant(10)),

                   Expression.PreIncrementAssign(i),

                   Expression.Break(b)

               ),

               b

           )

       )

   );

<h4 id="foreach-over-ienumerable">4.22.1.2 ForEach over IEnumerable</h4>

Here's an example of using LoopExpression to produce a ForEach loop:

// variable = Expression.Variable(typeof(object), "value");

public static Expression ForEach

(ParameterExpression variable, Expression enumerable,

Expression body) {

ParameterExpression temp =

Expression.Variable(typeof(IEnumerator), "$enumerator");

@break = Expression.Label();

return Expression.Block(

new\[\] { temp, variable },

Expression.Assign(temp,

Expression.Call(

enumerable,

typeof(IEnumerable)

.GetMethod("GetEnumerator"))),

Expression.Loop(

Expression.Block(

Expression.Condition(

Expression.Call(

temp,

typeof(IEnumerator)

.GetMethod("MoveNext")),

Expression.Empty(),

Expression.Break(@break)),

Expression.Assign(

variable,

Expression.Convert(

Expression.Property(

temp,

typeof(IEnumerator)

.GetProperty("Current")),

variable.Type)),

body),

@break));

}

<h4 id="lexical-semantics-with-blocks-and-loops-involved">4.22.1.3 Lexical Semantics with Blocks and Loops Involved</h4>

Note, these examples do not use LoopExpression, but they apply to LoopExpression and show how LoopExpressions can be thought of as reducing to Lablels and Goto's. These examples illustrate an interesting property of ETs and how to create strong lexical scoping for variables used within an iteration. They also show an underlying .NET issue with initializing variable storage locations vs. leaking through previous contents.

The following MakeLoop and MakeLoop2 (loop with lambda) produce expressions that when executed would create the resulting string shown in the table:

<table>
<thead>
<tr class="header">
<th> </th>
<th>Plain loop</th>
<th>loop with lambda</th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td>Tree’s ToString</td>
<td>{<br />
    var str;<br />
    var count;<br />
    Start:;<br />
    {<br />
        var i;<br />
        (count += 1);<br />
        (i += 1);<br />
        (str = Concat(str, i.ToString(), "|"));<br />
    };<br />
    IIF((count &lt; 10), goto Start, );<br />
    str;<br />
}</td>
<td>{<br />
    var str;<br />
    var count;<br />
    Start:;<br />
    {<br />
        var i;<br />
        (count += 1);<br />
        (i += 1);<br />
        (str = Concat(str, i.ToString(), "|"));<br />
        () =&gt; i;<br />
    };<br />
    IIF((count &lt; 10), goto Start, );<br />
    str;<br />
}</td>
</tr>
<tr class="even">
<td>result</td>
<td>1|2|3|4|5|6|7|8|9|10|</td>
<td>1|1|1|1|1|1|1|1|1|1|</td>
</tr>
</tbody>
</table>

Things to note are that the variable "i" is not explicitly initialized in either case, and the lambda expression in MakeLoop2 creates a closure over "i".

private static Expression MakeLoop(){

    LabelTarget start = Expression.Label("Start");

    ParameterExpression i = Expression.Parameter(typeof(int), "i");

    ParameterExpression count = Expression.Parameter(typeof(int),

"count");

    ParameterExpression str = Expression.Parameter(typeof(String),

"str");

    return Expression.Block(new ParameterExpression\[\] { str, count },

        Expression.Label(start),

        Expression.Block(new ParameterExpression\[\] { i },

            Expression.AddAssign(count, Expression.Constant(1)),

            Expression.AddAssign(i, Expression.Constant(1)),

            Expression.Assign(

                str,

                Expression.Call(

                    typeof(String)

.GetMethod(

"Concat",

new Type\[\] { typeof(String),

typeof(String),

typeof(String) }),

                    str,

                    Expression.Call(i, "ToString", Type.EmptyTypes),

                    Expression.Constant("\|")

                    )

                )

            ),

        Expression.IfThen(

            Expression.LessThan(count, Expression.Constant(10)),

            Expression.Goto(start)

            ),

        str

        );

}

private static Expression MakeLoop2() {

    LabelTarget start = Expression.Label("Start");

    ParameterExpression i = Expression.Parameter(typeof(int), "i");

    ParameterExpression count = Expression.Parameter(typeof(int),

"count");

    ParameterExpression str = Expression.Parameter(typeof(String),

"str");

    return Expression.Block(new ParameterExpression\[\] { str, count },

        Expression.Label(start),

        Expression.Block(new ParameterExpression\[\] { i },

            Expression.AddAssign(count, Expression.Constant(1)),

            Expression.AddAssign(i, Expression.Constant(1)),

            Expression.Assign(

                str,

                Expression.Call(

                    typeof(String)

.GetMethod(

"Concat",

new Type\[\] { typeof(String),

typeof(String),

typeof(String) }),

                    str,

                    Expression.Call(i, "ToString", Type.EmptyTypes),

                    Expression.Constant("\|")

                    )

                ),

            Expression.Lambda(i)

            ),

        Expression.IfThen(

            Expression.LessThan(count, Expression.Constant(10)),

            Expression.Goto(start)

            ),

        str

        );

}

The lambda create a closure over "i", which create a unique lexical binding in each iteration of the loop. .NET leaks old values into the variable for each iteration when there is no closure, so you get a string with one to ten in it. When there is a closure, .NET re-initializes the memory as it should, so you get a string with all ones.

If you want to capture closures over unique bindings/values of "i" in an interation, ETs correctly compile with those semantics using Goto's or Loops (when the Block is within the iteration bounds). However, you need to correctly initialize the loop variable that you close over. The following MakeLoop3 is the same as MakeLoop2 except that it uses an extra variable, "i\_", that is outside of the Block that is inside the loop bounds. The extra variable counts one to ten and is the initialization value for "i" each time the code enters the inner Block that is within the loop bounds.

private static Expression MakeLoop3() {

LabelTarget start = Expression.Label("Start");

ParameterExpression i\_ = Expression.Parameter(typeof(int), "i\_");

ParameterExpression i = Expression.Parameter(typeof(int), "i");

ParameterExpression count = Expression.Parameter(typeof(int),

"count");

ParameterExpression str = Expression.Parameter(typeof(String),

"str");

return Expression.Block(new ParameterExpression\[\]

{ str, count, i\_ },

Expression.Label(start),

Expression.Block(new ParameterExpression\[\] { i },

Expression.Assign(i, i\_),

Expression.AddAssign(count, Expression.Constant(1)),

Expression.AddAssign(i, Expression.Constant(1)),

Expression.Assign(

str,

Expression.Call(

typeof(String)

.GetMethod(

"Concat",

new Type\[\] { typeof(String),

typeof(String),

typeof(String) }),

str,

Expression.Call(i, "ToString",

Type.EmptyTypes),

Expression.Constant("\|")

)

),

Expression.Lambda(i),

Expression.Assign(i\_, i)

),

Expression.IfThen(

Expression.LessThan(count, Expression.Constant(10)),

Expression.Goto(start)

),

str

);

}

You can invoke the MakeLoop\* functions in a console application's Main with the following:

Console.WriteLine(Expression.Lambda&lt;Func&lt;string&gt;&gt;(MakeLoop3())

.Compile()());

<h3 id="class-summary-18">4.22.2 Class Summary</h3>

public sealed class LoopExpression : Expression {

public Expression Body { get; }

public LabelTarget BreakLabel { get; }

public LabelTarget ContinueLabel { get; }

public LoopExpression Update(LabelTarget breakLabel,

LabelTarget continueLabel,

Expression body)

<h3 id="update-method-11">4.22.3 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public LoopExpression Update(LabelTarget breakLabel,

LabelTarget continueLabel,

Expression body)

<h3 id="factory-methods-11">4.22.4 Factory Methods</h3>

These methods create LoopExpressions. The semantics of a LoopExpression is to loop forever, so the body Expression must explicitly exit the loop with a GotoExpression.

Signatures:

public static LoopExpression Loop(Expression body);

public static LoopExpression Loop

(Expression body, LabelTarget break, LabelTarget continue);

public static LoopExpression Loop(Expression body,

LabelTarget break);

Body is the expression to repeatedly execute.

Break identifies a location you can use in a GotoExpression from inside of the Body expression so that the code will terminate the loop and continue execution after the loop. If the Goto has a value, then it is the value of the LoopExpression. If not supplied, Break is null.

Continue identifies a location you can use in a GotoExpression from inside of the Body expression so that the code will terminate the current iteration and continue execution at the start of body. If not supplied, Continue is null.

Expression.Type is Expression.Break.Type if Break is non-null; otherwise, it is the void type.

<h2 id="post-clr-4.0----forexpression-class">4.23 POST CLR 4.0 -- ForExpression Class</h2>

<h3 id="class-summary-19">4.23.1 Class Summary</h3>

public sealed class ForExpression : Expression {

public Expression Body { get; }

public Expression Increment { get; }

public LabelTarget BreakLabel { get; }

public LabelTarget ContinueLabel { get; }

public Expression Test { get; }

<h2 id="post-clr-4.0----foreachexpression-class">4.24 POST CLR 4.0 -- ForEachExpression Class</h2>

Here's an example of using LoopExpression to produce a ForEach loop:

// variable = Expression.Variable(typeof(object), "value");

public static Expression ForEach

(ParameterExpression variable, Expression enumerable,

Expression body) {

ParameterExpression temp =

Expression.Variable(typeof(IEnumerator), "$enumerator");

@break = Expression.Label();

return Expression.Block(

new\[\] { temp, variable },

Expression.Assign(temp,

Expression.Call(

enumerable,

typeof(IEnumerable)

.GetMethod("GetEnumerator"))),

Expression.Loop(

Expression.Block(

Expression.Condition(

Expression.Call(

temp,

typeof(IEnumerator)

.GetMethod("MoveNext")),

Expression.Empty(),

Expression.Break(@break)),

Expression.Assign(

variable,

Expression.Convert(

Expression.Property(

temp,

typeof(IEnumerator)

.GetProperty("Current")),

variable.Type)),

body),

@break));

}

<h3 id="class-summary-20">4.24.1 Class Summary</h3>

public sealed class ForEachExpression : Expression {

public Expression Body { get; }

public Expression Iterable { get; }

public LabelTarget BreakLabel { get; }

public LabelTarget ContinueLabel { get; }

<h2 id="post-clr-4.0----whileexpression-class">4.25 POST CLR 4.0 -- WhileExpression Class</h2>

SIDENOTE

IPy will need to build a PythonWhileExpression to handle the Python Else clause if IPy wants to support meta-programming via ETs that is closer to the programs users write. The IPy-specific ET node could reduce to a WhileExpr:

PythonWhile &lt;cond&gt;: &lt;whilebody&gt; else: &lt;elsebody&gt; --&gt;

While (&lt;cond&gt; ? true : Block { &lt;elsebody&gt;; false }) &lt;whilebody&gt;

<h3 id="class-summary-21">4.25.1 Class Summary</h3>

public sealed class WhileExpression : Expression {

public Expression Body { get; }

public LabelTarget BreakLabel { get; }

public LabelTarget ContinueLabel { get; }

public Expression Test { get; }

<h2 id="post-clr-4.0----repeatuntilexpression-class">4.26 POST CLR 4.0 -- RepeatUntilExpression Class</h2>

<h3 id="class-summary-22">4.26.1 Class Summary</h3>

public sealed class RepeatUntilExpression : Expression {

public Expression Body { get; }

public LabelTarget BreakLabel { get; }

public LabelTarget ContinueLabel { get; }

public Expression Test { get; }

<h2 id="gotoexpression-class">4.27 GotoExpression Class</h2>

This class represents an unstructured flow of control. It has node kind Goto and a sub kind that captures the intent of the goto (break, return, etc.) for meta-programming purposes.

The Goto refers to a LabelTarget that a LabelExpression must refer to somewhere in the ET, and it is the LabelExpression that sets the target location for the flow of control. The LabelExpression must be lexically in the same LambdaExpression.Body as the GotoExpression.

The semantics of the LabelTarget chosen as the destination is lexically scoped in a sense. If all LabelTargets in the LambdaExpression are unique, then the LabelTarget in the GotoExpression simply must be found within the LambdaExpression containing the Goto. If the same LabelTarget is used multiple times within a LambdaExpression, then the GotoExpression targets the first matching LabelTarget found while searching up the ET to the Lambda root. This is a convenience for re-writers or tree builders that re-use sub trees that contain LabelExpressions and GotoExpressions so that the sub trees behave as expected unto themselves.

The Goto can optionally deliver a value to the location, as expressed by a non-null Expression property. If this property is non-null, then the expression Type property must represent a type that is reference assignable to the type represented by Target.Type. However, if Target.Type is void, the GotoExpression.Expression.Type can represent anything since the ET compiler will automatically convert the result to void or squelch the value.

See section for more details on the semantics of GotoExpression, as well as the introductory section on iterations, lexical exits, and gotos ().

<h3 id="class-summary-23">4.27.1 Class Summary</h3>

public sealed class GotoExpression : Expression {

public GotoExpressionKind Kind { get; }

public LabelTarget Target { get; }

public Expression Value { get; }

public GotoExpression Update(LabelTarget target, Expression value)

<h3 id="kind-property">4.27.2 Kind Property</h3>

This property returns the kind of Goto node this is. This property has no semantic bearing and only exists for debugging or documentation as to the intent of the goto.

Signature:

public GotoExpressionKind Kind { get; }

<h3 id="target-property">4.27.3 Target Property</h3>

This property returns the LabelTarget to which some LabelExpression within the same LambdaExpression must refer. This identifies the target location of the goto. This property is never null.

Signature:

public LabelTarget Target { get; }

<h3 id="value-property-1">4.27.4 Value Property</h3>

This property returns the Expression that provides the value to carry to the target location when transferring control. This property may be null.

Signature:

public Expression Value { get; }

<h3 id="update-method-12">4.27.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public GotoExpression Update(LabelTarget target, Expression value)

<h3 id="factory-methods-12">4.27.6 Factory Methods</h3>

Expression has the following factory methods for GotoExpression:

public static GotoExpression Break(LabelTarget target);

public static GotoExpression Break(LabelTarget target,

Expression value);

public static GotoExpression Break(LabelTarget target,

Type type);

public static GotoExpression Break(LabelTarget target,

Expression value, Type type);

public static GotoExpression Continue(LabelTarget target);

public static GotoExpression Continue(LabelTarget target,

Type type);

public static GotoExpression Goto(LabelTarget target);

public static GotoExpression Goto(LabelTarget target,

Expression value);

public static GotoExpression Goto(LabelTarget target,

Type type);

public static GotoExpression Goto(LabelTarget target,

Expression value, Type type);

public static GotoExpression MakeGoto

(GotoExpressionKind kind, LabelTarget target,

Expression value);

public static GotoExpression Return(LabelTarget target,

Expression value);

public static GotoExpression Return(LabelTarget target);

public static GotoExpression Return(LabelTarget target,

Type type);

public static GotoExpression Return(LabelTarget target,

Expression value, Type type);

Target must be the target of some LabelExpression lexically within the same LambdaExpression.Body that contains the resulting GotoExpression. The factory does not confirm this, so if this is not true, you'll get a compile-time error.

Value, if supplied is an expression to execute, and its value is delivered to the target location (that is, the value remains on the IL stack upon transferring control to the new location). The Expression's Type property must represent a type that is reference-assignable to the type represented by the target's Type property.

There are two special cases the factory handles when value's Type property represents a type that is not assignable to the type represented by target.Type. First, if Target.Type is void, the GotoExpression.Expression.Type can represent anything since the ET compiler will automatically convert the result to void or squelch the value. Second, if target's type is a sub type of LambdaExpression, and the value Expression object itself (that is, the Expression node) is of a type that is assignable to the target's type, then the value Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

<h2 id="gotoexpressionkind-enum">4.28 GotoExpressionKind Enum</h2>

This enum represents the kinds of GotoExpressions the DLR supports. These values are for debugging or documentation convenience only for capturing the intended use of the goto.

<h3 id="type-summary-1">4.28.1 Type Summary</h3>

public enum GotoExpressionKind {

Goto,

Return,

Break,

Continue

<h3 id="members">4.28.2 Members</h3>

The Summary section shows the type as it is defined (to indicate values of members), and this section documents the intent of the members.

| Goto     | The node is a basic goto targeting a label. The Value is a void DefaultExpression.                                                                                                                     |
|----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Return   | The node represents a 'return' or exit from a lambda. The Value may be a void DefaultExpression or any expression.                                                                                     |
| Break    | The node represents a goto within a loop to the exit target of the loop. The Value may be void or any expression.                                                                                      |
| Continue | The node represents a goto the beginning of a loop to abort the current iteration and continue with the next iteration and bindings of the iteration variables. The Value is a void DefaultExpression. |

<h2 id="labelexpression-class">4.29 LabelExpression Class</h2>

This class represents an unstructured flow of control target location. It has node kind Label.

The expression points to a LabelTarget. GotoExpressions refer to the same target object to designate that they jump to this LabelExpression location in the ET. The LabelTarget enables the ET to be acyclic by avoiding having Goto nodes point directly to Label nodes. Rewriting ETs behaves better regarding with LabelExpression identity when the LabelTarget object is distinct due to changes not propagating as far and forcing more rewrites via GotoExpressions.

The target has a Type property because Goto's can transfer control to a location with a value. The type allows factory methods and the ET compiler to verify static typing intent within the ET. See section for more information.

A Goto can optionally deliver a value to the LabelExpression's location. In case execution flows through the label in a structured way (not via a jump), it has a DefaltValue expression that provides the result of the LabelExpression. The label's location is AFTER the DefaultValue expression. If an unstructured flow of control lands at this LabelExpression's location, the GotoExpression provides the result for the LabelExpression.

The LabelExpression.Type property gets its value from Target.Type. Any GotoExpression that references the same Target must have a Type property that represents a type that is reference assignable to the type represented by Target.Type. There are two exceptions, see the factory method documentation for an explanation.

See section for more details on the semantics of GotoExpression and LabelExpression, as well as the introductory section on iterations, lexical exits, and gotos ().

<h3 id="example">4.29.1 Example</h3>

This shows how to use a label to exit a lambda and using the label's default expression. Often the LabelExpression will be the Body of the lambda, making the LabelExpression's DefaultValue effectively serve as the lamba's Body. However, this example is the best rendering of the code snippet. You could of course have written the ET purely expression-based (no return's) where the lambda's Body was a ConditionExpression with a ConstantExpression for the consequence and alternative expressions.

  // Simple return example:

  //   (int x) =&gt; {

  //     if (x &lt; 0) return -1;

  //     return 1;

  //   }

  var x = Expression.Parameter(typeof(int), "x");

  // Get LabelTarget

  var r = Expression.Label(typeof(int));

  var lambda = Expression.Lambda&lt;Func&lt;int, int&gt;&gt;(

      Expression.Block(

          Expression.IfThen(

              Expression.LessThan(x, Expression.Constant(0)),

              Expression.Return(r, Expression.Constant(-1))),

  // Add LabelExpr to define label target location and add

// default value if fall through to label target.

          Expression.Label(r, Expression.Constant(1))),

      x

  );

<h3 id="class-summary-24">4.29.2 Class Summary</h3>

public sealed class LabelExpression : Expression {

public Expression DefaultValue { get; }

public LabelTarget Target { get; }

public LabelExpression Update(LabelTarget target,

Expression defaultValue)

<h3 id="defaultvalue-property">4.29.3 DefaultValue Property</h3>

This property returns the default expression that provides the result of the LabelExpressions. If a GotoExpression transfers control to the Target, then it provides an expression that provides the result for the LabelExpression. If this property is null, then this object's Type property represents the void type.

Signature:

public Expression DefaultValue { get; }

<h3 id="target-property-1">4.29.4 Target Property</h3>

This property returns the target identity for the LabelExpression. GotoExpressions refer to the same target object to designate that they jump to this LabelExpression location in the ET.

Signature:

public LabelTarget Target { get; }

<h3 id="update-method-13">4.29.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public LabelExpression Update(LabelTarget target,

Expression defaultValue)

<h3 id="factory-methods-13">4.29.6 Factory Methods</h3>

The Expression class has the following factory methods for creating LabelExpressions:

public static LabelExpression Label(LabelTarget target);

public static LabelExpression Label(LabelTarget target,

Expression defaultValue);

Target identifies the LabelExpression so that a GotoExpressions can refer to the same target object to designate that it jumps to this LabelExpression location in the ET.

DefaultValue, if supplied is an expression to execute, and its value is the result of the LabelExpression if execution reaches the label without jumping. The defaultValue's Type property must represent a type that is reference-assignable to the type represented by the target's Type property.

There are two special cases the factory handles when defaultValue's Type property represents a type that is not assignable to the type represented by target.Type. First, if Target.Type is void, the defaultValue.Type can represent anything since the ET compiler will automatically convert the result to void or squelch the value. Second, if target's type is a sub type of LambdaExpression, and the defaultValue Expression object itself (that is, the Expression node) is of a type that is assignable to the target's type, then the value Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

<h2 id="labeltarget-class">4.30 LabelTarget Class</h2>

This class represents the identity of a LabelExpression to which GotoExpressions refer that want to jump to that LabelExpression. The LabelTarget enables the ET to be acyclic by avoiding having Goto nodes point directly to Label nodes. Rewriting ETs behaves better regarding with LabelExpression identity when the LabelTarget object is distinct due to changes not propagating as far and forcing more rewrites via GotoExpressions.

The target has a Type property because Goto's can transfer control to a location with a value. The type allows factory methods and the ET compiler to verify static typing intent within the ET. See section for more information.

<h3 id="class-summary-25">4.30.1 Class Summary</h3>

public sealed class LabelTarget {

public String Name { get; }

public Type Type { get; }

<h3 id="name-property">4.30.2 Name Property</h3>

This property returns the name of the label. This is useful purely for debugging or pretty printing purposes since it has no semantic effects.

Signature:

public String Name { get; }

<h3 id="type-property-1">4.30.3 Type Property</h3>

This property returns the type expected of any value delivered to the label's location of control flow. Any GotoExpression.Expression that targets this LabelTarget must have a Type property that represents a type that is reference assignable to the type represented by this Type property. However, if Target.Type is void, the GotoExpression.Expression.Type can represent anything since the ET compiler will automatically convert the result to void or squelch the value.

Signature:

public Type Type { get; }

<h3 id="factory-methods-14">4.30.4 Factory Methods</h3>

The Expression class has the following factory methods for creating LabelTargets:

public static LabelTarget Label(Type type, String name);

public static LabelTarget Label(Type type);

public static LabelTarget Label();

public static LabelTarget Label(String name);

<h2 id="memberexpression-class">4.31 MemberExpression Class</h2>

This class represents accessing an instance member, property or field, of an object or a static member of a type. It can be used as the Left expression of a BinaryExpression with node kind Assign.

<h3 id="class-summary-26">4.31.1 Class Summary</h3>

public sealed class MemberExpression : Expression {

public Expression Expression { get; }

public MemberInfo Member { get; }

public MemberExpression Update(Expression expression)

<h3 id="expression-property-2">4.31.2 Expression Property</h3>

This property returns the Expression representing the object or type on which to access Member.

Signature:

public Expression Expression { get; }

<h3 id="member-property">4.31.3 Member Property</h3>

This property return the Expression representing the member to access on Expression. This is never null.

Signature:

public MemberInfo Member { get; }

<h3 id="update-method-14">4.31.4 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public MemberExpression Update(Expression expression)

<h3 id="factory-methods-15">4.31.5 Factory Methods</h3>

Expression has the following factory methods for MemberExpressions:

public static MemberExpression Field(Expression expression,

String fieldName);

public static MemberExpression Field

(Expression expression, Type type, String fieldName);

public static MemberExpression Field(Expression expression,

FieldInfo field);

public static MemberExpression MakeMemberAccess

(Expression expression, MemberInfo member);

public static MemberExpression Property(Expression expression,

PropertyInfo property);

public static MemberExpression Property

(Expression expression, MethodInfo propertyAccessor);

public static MemberExpression Property(Expression expression,

String propertyName);

public static MemberExpression Property

(Expression expression, Type type, String propertyName);

public static MemberExpression PropertyOrField

(Expression expression, String propertyOrFieldName);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

Field, property, and propertyAccessor must be non-null. PropertyAccessor must represent a property accessor method. If the member represented by field, property, or propertyAccessor are instance members, expression must be non-null. Expression's Type property must be assignable to the declaring type of the member represented by field, property, or propertyAccessor.

For the methods taking a string and performing name resolution, expression, fieldName, propertyName, and propertyOrFieldName must be non-null. These factory methods search through expression.Type and its base types for fields, properties, or both. The found members have the name fieldName, propertyName, or propertyOrFieldName, respectively. These factories prefer Public members over non-public. Additionally, given PropertyOrField, the factory prefers properties over fields. If there is not exactly one result found that matches, these factories throw an exception. Otherwise, these factories pass expression and the found fieldInfo or propertyInfo to the Field or Property factory method above.

For MakeMemberAccess, member must be non-null. Based on the type of member, this factory calls one of the other factory methods with expression and member as arguments. All the requirements and guarantees of the called factory method apply.

The returned MemberExpression has

- Node kind MemberAccess

- Expression property set to expression

- Member property set to field, property, or the property referred to by propertyAccessor

- Type set to the FieldType or PropertyType property

<h2 id="memberinitexpression-class">4.32 MemberInitExpression Class</h2>

This class represents instantiating a type and filling in members before returning the instance. Its node kind is MemberInit. For example,

> // new MyClass { Foo = “hello”, Bar = “world” } becomes:
>
> MyClass obj = new MyClass();
>
> obj.Foo = “hello”;
>
> obj.Bar = “world”;

This node reduces to a block that instantiates the type and sets the supplied members. This node type is a ETs v1 type that, even with blocks and assignments in ETs v2, is still useful for convenience and meta-programming uses.

This node uses sub types of MemberBinding as helper model classes for individual member values and what methods to use to add the element.

<h3 id="class-summary-27">4.32.1 Class Summary</h3>

public sealed class MemberInitExpression : Expression {

public ReadOnlyCollection&lt;MemberBinding&gt;

Bindings { get; }

public NewExpression NewExpression { get; }

public MemberInitExpression Update

(NewExpression newExpression,

IEnumerable&lt;MemberBinding&gt; bindings)

<h3 id="bindings-property">4.32.2 Bindings Property</h3>

This property returns the collection of MemberBindings that represent how to fill in the members of the new instance that this node returns.

Signature:

public ReadOnlyCollection&lt;MemberBinding&gt;

Bindings { get; }

<h3 id="newexpression-property">4.32.3 NewExpression Property</h3>

This property returns a NewExpression whose semantics is to create an instance of the type represented by the node's Type property. The resulting object returned when this expression executes has its instance members filled in as described by its MemberBindings.

Signature:

public NewExpression NewExpression { get; }

<h3 id="update-method-15">4.32.4 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public MemberInitExpression Update

(NewExpression newExpression,

IEnumerable&lt;MemberBinding&gt; bindings)

<h3 id="factory-methods-16">4.32.5 Factory Methods</h3>

Expression has the following factory methods for MemberInitExpressions:

public static MemberInitExpression MemberInit

(NewExpression newExpression, params MemberBinding\[\] bindings);

public static MemberInitExpression MemberInit

(NewExpression newExpression,

IEnumerable&lt;MemberBinding&gt; bindings);

The following is derived from the v1 spec ...

NewExpression and bindings must be non-null. For each element of bindings, the Member property must represent a member of the type represented by newExpression.Type.

The resulting MemberInitExpression has the NewExpression property equal to newExpression, and Bindings has the same elements as bindings. The Type property is equal to newExpression.Type.

<h2 id="memberbinding-class">4.33 MemberBinding Class</h2>

This class is the base class for representing member initialization expressions within MemberInitExpressions. This is a supporting type, not an Expression. Each subtype of this type (MemberListBinding, MemberAssignment, and MemberMemberBinding) has a unique kind enum value to support switching on the binding kinds in languages that do not have type switch expressions.

No one should derive from this class, and the constructor will be made obsolete in future versions.

<h3 id="class-summary-28">4.33.1 Class Summary</h3>

public abstract class MemberBinding {

// This constructor is now obsolete in spec. It will be obsolete in

// code in v-next+1, then removed in v-next+2.

protected MemberBinding(MemberBindingType type, MemberInfo member);

public MemberBindingType BindingType { get; }

public MemberInfo Member { get; }

<h3 id="memberbinding-constructor">4.33.2 MemberBinding Constructor</h3>

No one should derive from this class, and the constructor will be made obsolete in

v-next+1, then removed in v-next+2.

<h3 id="bindingtype-property">4.33.3 BindingType Property</h3>

This property returns the kind of binding this object represents, which is one to one with the concrete sub type.

public MemberBindingType BindingType { get; }

<h3 id="member-property-1">4.33.4 Member Property</h3>

This property returns the method that sets the member. It is never null.

Signature:

public MemberInfo Member { get; }

<h2 id="memberbindingtype-enum">4.34 MemberBindingType Enum</h2>

This enum provides member for tagging MemberBinding sub types. Each MemberBinding subtype has a unique MemberBindingType value to support switching on the binding kinds in languages that do not have type switch expressions.

<h3 id="type-summary-2">4.34.1 Type Summary</h3>

public enum MemberBindingType {

Assignment,

MemberBinding,

ListBinding

<h3 id="type-members">4.34.2 Type Members</h3>

The Summary section shows the type as it is defined (to indicate values of members), and this section documents the intent of the members.

<table>
<thead>
<tr class="header">
<th>Assignment</th>
<th><p>The MemberBinding is an instance of MemberAssignment. This models setting a member to a scalar value, for example, the setting of Foo in:</p>
<p>new Blah {Foo = 5, ...}</p></th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td>MemberBinding</td>
<td><p>The MemberBinding is an instance of MemberMemberBinding. This models setting a member to a new instance of a type with the supplied member values, for example, the setting of Foo in:</p>
<p>new Blah {Foo = {Bar = ..., Baz = ...}, ...}</p></td>
</tr>
<tr class="even">
<td>ListBinding</td>
<td><p>The MemberBinding is an instance of MemberListBinding. This models setting a member to a list of values, for example, the setting of Foo in:</p>
<p>new Blah {Foo = {1, 2, 3, ...}, ...}</p></td>
</tr>
</tbody>
</table>

<h2 id="memberlistbinding-class">4.35 MemberListBinding Class</h2>

This class represents the setting of an instance member to a list of elements. This is a supporting type used in MemberInitExpressions. For example:

New Foo { bar = {1, 2, 3}}

<h3 id="class-summary-29">4.35.1 Class Summary</h3>

public sealed class MemberListBinding : MemberBinding {

public ReadOnlyCollection&lt;ElementInit&gt;

Initializers { get; }

public MemberListBinding Update

(IEnumerable&lt;ElementInit&gt; initializers)

<h3 id="initializers-property">4.35.2 Initializers Property</h3>

This property returns the collection of ElementInit model objects for what values to add to the member collection and how to add each value.

Signature:

public ReadOnlyCollection&lt;ElementInit&gt;

Initializers { get; }

<h3 id="update-method-16">4.35.3 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public MemberListBinding Update

(IEnumerable&lt;ElementInit&gt; initializers)

<h3 id="factory-methods-17">4.35.4 Factory Methods</h3>

Expression has the following factory methods for MemberMemberBindings:

public static MemberListBinding ListBind

(MemberInfo member,

IEnumerable&lt;ElementInit&gt; initializers);

public static MemberListBinding ListBind

(MemberInfo member, params ElementInit\[\] initializers);

public static MemberListBinding ListBind

(MethodInfo propertyAccessor,

IEnumerable&lt;ElementInit&gt; initializers);

public static MemberListBinding ListBind

(MethodInfo propertyAccessor,

params ElementInit\[\] initializers);

The following is derived from the v1 spec ...

Member must be non-null, and must represent a field or property. Let T represent a FieldType or PropertyType for member. PropertyAccessor must represent a property accessor method. Let T also represent the property's type. T must be assignable to IEnumerable. Each method in ElementInits must be callable on instances of T for adding elements to the member.

<h2 id="membermemberbinding-class">4.36 MemberMemberBinding Class</h2>

This class represents the recursive initializing of one object's member with the creation of another object and setting the second object's members. This is a supporting type used in MemberInitExpressions. For example, the setting of Foo in:

new Blah {Foo = {Bar = ..., Baz = ...}, ...}

<h3 id="class-summary-30">4.36.1 Class Summary</h3>

public sealed class MemberMemberBinding : MemberBinding {

public ReadOnlyCollection&lt;MemberBinding&gt;

Bindings { get; }

public MemberMemberBinding Update

(IEnumerable&lt;MemberBinding&gt; bindings)

<h3 id="bindings-property-1">4.36.2 Bindings Property</h3>

This property returns the collection of MemberBindings that describe how to initialize members of an object. The object is the initialization value a member who MemberInitExpression recursively contains this MemberMemberBinding model object. This property is never null.

<h3 id="update-method-17">4.36.3 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public MemberMemberBinding Update

(IEnumerable&lt;MemberBinding&gt; bindings)

<h3 id="factory-methods-18">4.36.4 Factory Methods</h3>

Expression has the following factory methods for MemberMemberBindings:

public static MemberMemberBinding MemberBind

(MemberInfo member,

IEnumerable&lt;MemberBinding&gt; bindings);

public static MemberMemberBinding MemberBind

(MemberInfo member, params MemberBinding\[\] bindings);

public static MemberMemberBinding MemberBind

(MethodInfo propertyAccessor,

IEnumerable&lt;MemberBinding&gt; bindings);

public static MemberMemberBinding MemberBind

(MethodInfo propertyAccessor, params MemberBinding\[\] bindings);

The following is derived from the v1 spec ...

Member must be non-null, and must represent a field or property. Let T be the FieldType or PropertyType. PropertyAccessor must represent a property accessor method of a property. Let T also be the property's PropertyType. Bindings must be non-null, and for each element of bindings, the Member property must represent a member of T.

<h2 id="memberassignment-class">4.37 MemberAssignment Class</h2>

This class models assigning a scalar value to a MemberInitExpression's member, for example, the setting of Foo in:

new Blah {Foo = 5, ...}

<h3 id="class-summary-31">4.37.1 Class Summary</h3>

public sealed class MemberAssignment : MemberBinding {

public Expression Expression { get; }

public MemberAssignment Update(Expression expression)

<h3 id="expression-property-3">4.37.2 Expression Property</h3>

This property returns the Expression that models the value to assign to the MemberInitExpression member.

Signature:

public Expression Expression { get; }

<h3 id="update-method-18">4.37.3 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public MemberAssignment Update(Expression expression)

<h3 id="factory-methods-19">4.37.4 Factory Methods</h3>

Expression has the following factory methods for MemberAssignments:

public static MemberAssignment Bind(MethodInfo propertyAccessor,

Expression expression);

public static MemberAssignment Bind(MemberInfo member,

Expression expression);

The following is derived from the v1 spec ... with additions on binding to generic properties.

Member must be non-null, and must represent a field or property. PropertyAccessor must represent a property accessor method. Expression must be non-null, and its Type property must be assignable to the FieldType or PropertyType of the field or property represented by member or propertyAccessor.

You cannot simply bind to generic base type properties. For example, the following code does not work:

public class OrderBase&lt;T&gt; {

    public T OrderName { get; set; }

}

public class Order : OrderBase&lt;string&gt; {

    public string OrderID { get; set; }

}

class Program {

    static void Main(string\[\] args) {

        // Get handles for the setter method and the Order classs

        Type orderType = typeof(Order);

        RuntimeTypeHandle orderHandle = orderType.TypeHandle;

        PropertyInfo nameProp = orderType.GetProperty("OrderName");

        MethodInfo nameMethod = nameProp.GetSetMethod();

        RuntimeMethodHandle nameHandle = nameMethod.MethodHandle;

        // Now get a MethodInfo for the setter method

        MethodInfo nameMethodFromHandle =

(MethodInfo)MethodBase

.GetMethodFromHandle(nameHandle,

orderHandle);

        ConstantExpression constant = Expression.Constant("Ben");

        // Attempt to bind.  Fails with:

        // System.ArgumentException: The method

// 'TestBinding.OrderBase\`1\[System.String\].set\_OrderName'

// is not a property accessor

        MemberAssignment binding =

Expression.Bind(nameMethodFromHandle, constant);

    }

}

The Bind factory method ultimately fetches the DeclaringType from the MethodInfo, which doesn't work in this case. Bind fetches this so that it can get the PropertyInfo, which this node holds onto as convenience to tree walkers and consumers. You can construct the PropertyInfo for a generic base class by tweaking some of the DLR's internal code and calling it as a helper:

MemberBinding binding =

Expression.Bind(GetProperty(propertyAccessor,

Type.GetTypeFromHandle(

edmProperty

.PropertyDeclaringType)),

valueReader);

private static PropertyInfo GetProperty(MethodInfo setterMethod,

Type declaringType) {

    BindingFlags bindingAttr = BindingFlags.NonPublic \|

BindingFlags.Public \|

BindingFlags.Instance;

    foreach (PropertyInfo propertyInfo in

declaringType.GetProperties(bindingAttr)) {

        if (propertyInfo.GetSetMethod(nonPublic: true) ==

setterMethod) {

            return propertyInfo;

        }

    }

}

<h2 id="listinitexpression-class">4.38 ListInitExpression Class</h2>

This class represents a collection construction and initialization. It has the ListInit node kind. You can use this node to create any type that supports an "add" method (case-insensitive). For example,

> // new MyList { “hello”, “world” } becomes:
>
> MyList list = new MyList();
>
> list.Add(“hello”);
>
> list.Add(“world”);

This node reduces to a block that instantiates the type and adds the supplied elements. This node type is a ETs v1 type that, even with blocks and assignments in ETs v2, is still useful for convenience and meta-programming uses.

This node uses ElementInit as a helper model class for individual element values and what method to use to add the element.

<h3 id="class-summary-32">4.38.1 Class Summary</h3>

public sealed class ListInitExpression : Expression {

public ReadOnlyCollection&lt;ElementInit&gt;

Initializers { get; }

public NewExpression NewExpression { get; }

public ListInitExpression Update

(NewExpression newExpression,

IEnumerable&lt;ElementInit&gt; initializers)

<h3 id="initializers-property-1">4.38.2 Initializers Property</h3>

This property returns a read-only collection of ElementInit objects describing each element and how to add it to the new instance of this node's Type.

Signature:

public ReadOnlyCollection&lt;ElementInit&gt;

Initializers { get; }

<h3 id="newexpression-property-1">4.38.3 NewExpression Property</h3>

This property returns the NewExpression that creates an instance of this node's Type.

Signature:

public NewExpression NewExpression { get; }

<h3 id="update-method-19">4.38.4 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public ListInitExpression Update

(NewExpression newExpression,

IEnumerable&lt;ElementInit&gt; initializers)

<h3 id="factory-methods-20">4.38.5 Factory Methods</h3>

Expression has the following factory methods for ListInitExpressions:

public static ListInitExpression ListInit

(NewExpression newExpression,

params ElementInit\[\] initializers);

public static ListInitExpression ListInit

(NewExpression newExpression, MethodInfo addMethod,

params Expression\[\] initializers);

public static ListInitExpression ListInit

(NewExpression newExpression,

IEnumerable&lt;ElementInit&gt; initializers);

public static ListInitExpression ListInit

(NewExpression newExpression,

IEnumerable&lt;Expression&gt; initializers);

public static ListInitExpression ListInit

(NewExpression newExpression,

params Expression\[\] initializers);

public static ListInitExpression ListInit

(NewExpression newExpression, MethodInfo addMethod,

IEnumerable&lt;Expression&gt; initializers);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

NewExpression and initializers must be non-null. The type represented by newExpression.Type must implement System.Collections.IEnumerable. If a non-null addMethod is given, it must represent an instance method named “Add” with exactly one parameter. The Type property of all elements of initializers must represent a type that is assignable to the parameter type of addMethod.

The resulting ListInitExpression has:

- Node kind set to ListInit

- NewExpression set to newExpression

- Type set to newExpression.Type

- If initializers is given as an IEnumerable&lt;ElementInit&gt; or an ElementInit\[\], then the Initializers property is simply equal to initializers. Otherwise, this factory determines an addMethod to use and creates ElementInit objects for a collection of initializers as specified below.

If you supply a non-null addMethod, then that is the add method of the ListInitExpression. Otherwise, this factory searches for a single-argument instance method with a name equal to “Add” (ignoring case) on newExpression.Type and its base type. If exactly one method is found, that is the add method; otherwise, this factory throws an exception.

The Initializer property is then a list of ElementInit objects, one for each element of initializers, with the AddMethod property being the add method, and the Arguments property being a ReadOnlyCollection&lt;Expression&gt; containing the corresponding element of initializers as its single element.

<h2 id="elementinit-class">4.39 ElementInit Class</h2>

This class represents elements to be added to a new instance of a type in the ListInitExpression and MemberListBinding classes. This is a supporting type used in some expression nodes, and it is not an expression.

<h3 id="class-summary-33">4.39.1 Class Summary</h3>

public sealed class ElementInit {

public MethodInfo AddMethod { get; }

public ReadOnlyCollection&lt;Expression&gt; Arguments { get; }

public ElementInit Update(IEnumerable&lt;Expression&gt; arguments)

<h3 id="addmethod-property">4.39.2 AddMethod Property</h3>

This property returns the method that will be used to add an element to an object whose intantiation and initialization are modeled in a ListInitExpression or MemberListBinding.

Signature:

public MethodInfo AddMethod { get; }

<h3 id="arguments-property-4">4.39.3 Arguments Property</h3>

This property returns the collection of argument expressions for this object's AddMethod.

Signature:

public ReadOnlyCollection&lt;Expression&gt; Arguments { get; }

<h3 id="update-method-20">4.39.4 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public ElementInit Update(IEnumerable&lt;Expression&gt; arguments)

<h3 id="factory-methods-21">4.39.5 Factory Methods</h3>

Expression has the following factories for ElementInit objects:

public static ElementInit ElementInit

(MethodInfo addMethod,

IEnumerable&lt;Expression&gt; arguments);

public static ElementInit ElementInit

(MethodInfo addMethod, params Expression\[\] arguments);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

AddMethod and arguments must be non-null. AddMethod must represent an instance method named “Add” (ignoring case). Arguments must have the same number of elements as the number of parameters the method represented by addMethod takes. Each of the elements of arguments must be non-null, and the types of the values they represent must be assignable to the type of the corresponding parameter of addMethod. There is a special case the factory handles when an element of arguments has a Type property representing a type that is not assignable to the corresponding parameter type. If the parameter's type is a sub type of LambdaExpression, and the argument Expression object itself (that is, the Expression node) is of a type that is assignable to the parameter's type, then the argument Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

The resulting ElementInit has the AddMethod property equal to addMethod and the Arguments property equal to arguments.

<h2 id="newexpression-class">4.40 NewExpression Class</h2>

This class represents calling a constructor to instantiate a type. These nodes have the New node kind. These are distinct from MethodCallExpression for a couple of reasons. One is that object instantiation is often a distinct linguistic feature as opposed to being functionality that naturally fits other features like regular function calls. This distinction is worth explicitly representing for meta-programming purposes. The second reason is that in .NET ConstructorInfos are not the same as MethodInfos requiring a distinct type.

The NewExpression uses the New node kind. It represents calling a constructor to create a new object. Given a New node, exp, let T be the C\# name of the declaring type of exp.Constructor, and let e1…en be the comma-separated list of C\# expressions equivalent to the corresponding nodes in exp.Arguments. Then the C\# equivalent of exp is "new T(e1…en)".

<h3 id="class-summary-34">4.40.1 Class Summary</h3>

public class NewExpression : Expression {

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

public ConstructorInfo Constructor { get; }

public ReadOnlyCollection&lt;System.Reflection.MemberInfo&gt;

Members { get; }

public NewExpression Update(IEnumerable&lt;Expression&gt; arguments)

<h3 id="arguments-property-5">4.40.2 Arguments Property</h3>

This returns the arguments to the constructor invocation. This never returns null, returning an empty collection for the default constructor.

Signature:

public ReadOnlyCollection&lt;Expression&gt;

Arguments { get; }

<h3 id="constructor-property">4.40.3 Constructor Property</h3>

This returns the ConstructorInfo for the constructor. This never returns null.

Signature:

public ConstructorInfo Constructor { get; }

<h3 id="members-property">4.40.4 Members Property</h3>

When constructing an anonymous type, this returns MemberInfos describing the members of the type to construct. For example, the C\# expression “new { Foo = x, Bar = y }” could be represented by a NewExpression with Members having the PropertyInfos for “Foo” and “Bar”.

This never returns null, returning an empty collection for non-anonymous types.

Signature:

public ReadOnlyCollection&lt;MemberInfo&gt;

Members { get; }

<h3 id="update-method-21">4.40.5 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public NewExpression Update(IEnumerable&lt;Expression&gt; arguments)

<h3 id="factory-methods-22">4.40.6 Factory Methods</h3>

Expression has the following factory methods for creating NewExpressions:

public static NewExpression New

(ConstructorInfo constructor,

IEnumerable&lt;Expression&gt; arguments,

params MemberInfo\[\] members);

public static NewExpression New(ConstructorInfo constructor);

public static NewExpression New(Type type);

public static NewExpression New(ConstructorInfo constructor,

params Expression\[\] arguments);

public static NewExpression New

(ConstructorInfo constructor,

IEnumerable&lt;Expression&gt; arguments);

public static NewExpression New

(ConstructorInfo constructor,

IEnumerable&lt;Expression&gt; arguments,

IEnumerable&lt;MemberInfo&gt; members);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

Constructor and type must be non-null. Type must represent a type that has a constructor that takes no arguments.

If arguments is omitted or null, the factory stores an empty collection. If provided, arguments must have the same number of elements as the number of parameters for the constructor. Each of the elements of arguments must be non-null, and the types of the values they represent must be assignable to the type of the corresponding parameter of addMethod. There is a special case the factory handles when an element of arguments has a Type property representing a type that is not assignable to the corresponding parameter type. If the parameter's type is a sub type of LambdaExpression, and the argument Expression object itself (that is, the Expression node) is of a type that is assignable to the parameter's type, then the argument Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

If provided, members must have the same number of elements as arguments. Each of the elements of members must be non-null. Each must be a gettable instance PropertyInfo, FieldInfo, or MethodInfo that is available on the declaring type of constructor. You might expect the members to be setters, but these exist purely to capture the names of members, for pretty printing, or for debugging uses. They do not affect the semantics or results of NewExpression nodes. The corresponding element of arguments for a particular members element must have a type assignable to the type of the member.

The resulting NewExpression has:

- Node kind New

- Constructor property set to constructor, or the constructor of the type that takes no arguments.

- Arguments property set to arguments, except that some elements may be “quoted” as described above. If arguments is omitted or null, Arguments is an empty collection.

- Members property set to the same elements as members. If members is omitted or null, Members is an empty collection.

- Type property set to the declaring type of the constructor denoted by constructor, or set to the type argument.

Note that when member infos are supplied we map any get\_ or set\_ member infos to prop infos, and this is a breaking change from v1.

<h2 id="newarrayexpression-class">4.41 NewArrayExpression Class</h2>

This class represents creating a new array. It uses the NewArrayInit and NewArrayBounds node kinds. NewArrayInit represents making a one-dimensional array by specifying a list of elements, for example, in C\# "new T\[\]{e1…en}". NewArrayBounds represents making a new array by specifying its bounds for each dimension, for example, in C\# "new T\[e1…en\]".

<h3 id="class-summary-35">4.41.1 Class Summary</h3>

public class NewArrayExpression : Expression {

public ReadOnlyCollection&lt;Expression&gt;

Expressions { get; }

public NewArrayExpression Update

(IEnumerable&lt;Expression&gt; expressions)

<h3 id="expressions-property-1">4.41.2 Expressions Property</h3>

This property returns the collection of Expressions that provide values for initializing a single dimensional array or the integer bounds for each dimension of an array, depending on the node kind.

Signature:

public ReadOnlyCollection&lt;Expression&gt;

Expressions { get; }

<h3 id="update-method-22">4.41.3 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public NewArrayExpression Update

(IEnumerable&lt;Expression&gt; expressions)

<h3 id="factory-methods-23">4.41.4 Factory Methods</h3>

Expression has the following factory methods for NewArrayExpression nodes:

public static NewArrayExpression NewArrayBounds

(Type type,

IEnumerable&lt;Expression&gt; bounds);

public static NewArrayExpression NewArrayBounds

(Type type, params Expression\[\] bounds);

public static NewArrayExpression NewArrayInit

(Type type,

IEnumerable&lt;Expression&gt; initializers);

public static NewArrayExpression NewArrayInit

(Type type, params Expression\[\] initializers);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

For the ...Bounds factories Type and bounds must be non-null. Type represents the element type. Each element of bounds must be non-null, and its Type property must represent an integral type. The resulting NewArrayExpression has node kind NewArrayBounds. The Type property represents an array type with rank equal to the length of bounds and the element type represented by type. The Expressions property has the same elements as bounds.

For the ...Init factories type and initializers must be non-null. Type represents the element type. Each element of initializers must be non-null and have a Type property that represents a type assignable to the type represented by type. There is a special case the factory handles when an element of initializers has a Type property representing a type that is not assignable to type. If the initializer's type is a sub type of LambdaExpression, and the element Expression object itself (that is, the Expression node) is of a type that is assignable to type, then the element Expression node is wrapped in a Quote node. This supports a legacy ETs v1 behavior for how C\# chose to implement expression such as "Expression&lt;Func&lt;...&gt;&gt; = (...) =&gt; ...".

The resulting NewArrayExpression has node kind NewArrayInit. The Type property represents an array type with rank 1 and the element type represented by type. The Expressions property has the same elements as initializers.

<h2 id="lambdaexpression-class">4.42 LambdaExpression Class</h2>

This abstract class represents a function definition, and it always manifests as Expression&lt;T&gt;. Evaluating a LambdaExpression produces a delegate. It has node kind Lambda.

The LambdaExpression may be in the context of (that is, it is a sub ET of) other lambdas or BlockExpressions. When this occurs, and the inner LambdaExpression contains ParameterExpression nodes that refer to variables defined by outer lambdas or blocks, the expression compiler creates closure environments when needed, lifting variables.

<h3 id="examples-1">4.42.1 Examples</h3>

Iterative Fact:

static Func&lt;int, int&gt; ETFact() {

var value = Expression.Parameter(typeof(int), "value");

var result = Expression.Parameter(typeof(int), "result");

var label = Expression.Label(typeof(int));

var lambda = Expression.Lambda&lt;Func&lt;int, int&gt;&gt;(

Expression.Block(

new\[\] { result },

Expression.Assign(result, Expression.Constant(1)),

Expression.Loop(

Expression.IfThenElse(

Expression.GreaterThan(value,

Expression.Constant(1)),

Expression.MultiplyAssign

(result,

Expression.PostDecrementAssign(value)),

Expression.Break(label, result)),

label)),

value);

return lambda.Compile();

Recursive Fact with StrongBox to refer to function recursively:

using System;

using System.Linq.Expressions;

using System.Runtime.CompilerServices;

namespace LambdaRecursion

{

    class Program

    {

        static void Main(string\[\] args)

        {

            // fact = (x) =&gt; x &lt;= 1 ? 1 : x\*fact(x-1)

            var x = Expression.Parameter(typeof(int), "x");

            var factorial = new StrongBox&lt;Func&lt;int, int&gt;&gt;();

            var lambda = Expression.Lambda&lt;Func&lt;int, int&gt;&gt;(

                Expression.Condition(

                    Expression.LessThanOrEqual

(x, Expression.Constant(1)),

                    Expression.Constant(1),

                    Expression.Multiply(

                        x,

                        Expression.Invoke(

                            Expression.Field

(Expression.Constant(factorial),

"Value"),

                            Expression.Subtract

(x, Expression.Constant(1))))),

                x);

            factorial.Value = lambda.Compile();

            Console.WriteLine(factorial.Value(5));

Recursive Fact just using ParameterExpression variables for recursive reference:

ParameterExpression input = Expression.Parameter(typeof(int));

var test = Expression.GreaterThan(input, Expression.Constant(1));

var factorial = Expression.Parameter(typeof(Func));

Expression body = Expression.Block(

Expression.Condition(

test,

Expression.Multiply(

input,

Expression.Invoke(

factorial,

Expression.Subtract(

input,

Expression.Constant(1)))),

Expression.Constant(1)));

var fact = Expression.Lambda&gt;(body, input);

var block = Expression.Block(

new ParameterExpression\[\] { factorial },

Expression.Assign(factorial, fact),

Expression.Invoke(factorial, Expression.Constant(5)));

Expression.Lambda(block).Compile()();

<h3 id="class-summary-36">4.42.2 Class Summary</h3>

public abstract class LambdaExpression : Expression {

public Expression Body { get; }

public String Name { get; }

public ReadOnlyCollection

&lt;ParameterExpression&gt;

Parameters { get; }

public Type ReturnType { get; }

public void TailCall { get; }

public Delegate Compile();

public Delegate Compile(DebugInfoGenerator debugInfoGenerator)

public void CompileToMethod(MethodBuilder method);

public void CompileToMethod(MethodBuilder method,

DebugInfoGenerator debugInfoGenerator);

public Expression&lt;TDelegate&gt; Update

(Expression body, IEnumerable&lt;ParameterExpression&gt; parameters)

<h3 id="body-property-2">4.42.3 Body Property</h3>

This property returns the body expression of the lambda. It is never null.

This node's Body.Type is reference assignable to ReturnType with one exception. ReturnType may be void when Body.Type is not, for example:

static int NonVoidMethod() {

return 123;

}

static void Main(string\[\] args) {

Expression&lt;Action&gt; e = () =&gt; NonVoidMethod();

}

For convenience in this case, the types do not have to match, and the lambda will automatically convert the result to void or squelch it.

Signature:

public Expression Body { get; }

<h3 id="name-property-1">4.42.4 Name Property</h3>

This property returns the name of the lambda, which is used for debugging or pretty printing only. It has no semantic value. It may be null.

Signature:

public String Name { get; }

<h3 id="parameters-property">4.42.5 Parameters Property</h3>

This property returns the collection of ParameterExpressions that declare parameter variables for the lambda. The number and types are consistent with the delegate type in the Type property for a LambdaExpression.

Signature:

public ReadOnlyCollection

&lt;ParameterExpression&gt;

Parameters { get; }

<h3 id="returntype-property">4.42.6 ReturnType Property</h3>

This property returns the type that is the return type of the delegate type in this node's Type property. This node's Body.Type is reference assignable to ReturnType with one exception. ReturnType may be void when Body.Type is not, for example:

static int NonVoidMethod() {

return 123;

}

static void Main(string\[\] args) {

Expression&lt;Action&gt; e = () =&gt; NonVoidMethod();

}

For convenience in this case, the types do not have to match, and the lambda will automatically convert the result to void or squelch it.

<h3 id="tallcall-property">4.42.7 TallCall Property</h3>

This property returns whether compiling this LambdaExpression should attempt to use tail calls for any value returning expressions. If true, this is not a guarantee since some calls cannot be tailed called (for example, there may be write backs needed for some properties or ref args).

Signature:

public void TailCall { get; }

<h3 id="compile-methods">4.42.8 Compile\* Methods</h3>

These methods compile the LambdaExpression instance. Compile returns a delegate that you can immediately invoke to execute the ET. The delegate has the type represented by this nodes Type property.

DebugInfoGenerator, if supplied, indicates the compiler should emit sequence point and local variable information. It also implements members the compiler uses to emit this information.

Compiling an Expression&lt;D&gt; which contains calls of unsafe code may cause an exception to get thrown.

CompileToMethod is useful for hosted languages that want to provide some pre-compilation support so that their host applications do not have to read source code and compile it on each start up. It is up to the language using this method to prepare the assembly and write it to disk.

Signatures:

public Delegate Compile();

public Delegate Compile(DebugInfoGenerator debugInfoGenerator)

public void CompileToMethod(MethodBuilder method);

public void CompileToMethod(MethodBuilder method,

DebugInfoGenerator debugInfoGenerator);

<h3 id="update-method-23">4.42.9 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public Expression&lt;TDelegate&gt; Update

(Expression body, IEnumerable&lt;ParameterExpression&gt; parameters)

<h3 id="factory-methods-24">4.42.10 Factory Methods</h3>

Expression has the following factory methods for LambdaExpressions:

public static LambdaExpression Lambda

(Expression body, params ParameterExpression\[\] parameters);

public static LambdaExpression Lambda

(Expression body, bool tailcall,

params ParameterExpression\[\] parameters);

public static LambdaExpression Lambda

(Expression body,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static LambdaExpression Lambda

(Expression body, bool tailcall,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static LambdaExpression Lambda

(Expression body, String name,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static LambdaExpression Lambda

(Expression body, String name, bool tailcall,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body, String name,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body, String name, bool tailcall,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body, params ParameterExpression\[\] parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body, bool tailcall,

params ParameterExpression\[\] parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static Expression&lt;TDelegate&gt; Lambda&lt;TDelegate&gt;

(Expression body, bool tailcall,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body, String name,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body, String name,

bool tailcall,

IEnumerable&lt;ParameterExpression&gt; parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body,

params ParameterExpression\[\] parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body, bool tailcall,

params ParameterExpression\[\] parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body, bool tailcall,

params ParameterExpression\[\] parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body,

IEnumerable&lt;ParameterExpression&gt;

parameters);

public static LambdaExpression Lambda

(Type delegateType, Expression body, bool tailcall,

IEnumerable&lt;ParameterExpression&gt;

parameters);

The following is derived from the v1 spec ... with updates for correctness or new behaviors

When no delegate type is specified via generic or other parameters, body must be non-null. If you supply more than 16 parameters, the factory throws an exception; you can use Expression.GetDelegateType and another factory method to work around this. These methods construct a delegate type based on body.Type and the Type property of each parameter, if supplied. When possible, the delegate type is from the System.Action (if body has void Type) or System.Func (if body is value returning). These factories then pass the delegate type, body, and parameters to one of the other Lambda methods.

When you supply the delegate types via the generic parameter, it must be a delegate type. If supplied via the delegateType parameter, it must be a non-null delegate type. Body must be non-null, and body.Type must represent a type that is assignable to the return type of the delegate type. There are two special cases the factory handles when body's type is not assignable to the return type. The first is that if the delegate's return type is void, in which case the compiler pops the IL stack should Body.Expression leave a value on it. The second case is if the return type is a sub type of LambdaExpression, and the body expression object itself (that is, the Expression node) is of a type that is assignable to the return type. In this case the factory wraps the body Expression node in a Quote node. The list of parameters for the delegate must have the same length as parameters, or be 0 if parameters is null. If parameters is non-null, all its elements must be non-null, and their Type properties must represent types that are assignable from the corresponding parameter types of the delegate.

If tailcall is unsupplied, it defaults to False. When true, this parameter indicates that compiling this LambdaExpression should attempt to use tail calls for any value returning expressions. True is not a guarantee since some calls cannot be tailed called (for example, there may be write backs needed for some properties or ref args). There are also .NET CLR constraints when JIT'ing these calls within dynamic methods. You cannot call from security transparent dynamic methods to critical callees, which may be allowed in CLR 4.0 on x86 (it works on x64). If all the methods are in the same assembly, or for a dynamic method calling a dynamic method (such as, recursing on itself), JIT should use tail call on CLR 3.5 and 4.0.

Name, if not null, is used for debugging or pretty printing only. It has no semantic value. If you need to detect internal helper frames used by the DLR (for example, CallSite cache delegates), see System.Runtime.CompilerServices.IsInternalFrame.

The resulting object is always an Expression&lt;Tdelegate&gt; with:

- Node kind Lambda

- Body and Parameters set to body and parameters, respectively. If parameters is null, Parameters is an empty collection.

- TailCall property set to the tailcall parameter or False

- Type property set to the delegate type

<h2 id="expressiontdelegate-class">4.43 Expression&lt;TDelegate&gt; Class</h2>

This class is the concrete type instantiated for LambdaExpressions. See that type for more information.

<h3 id="class-summary-37">4.43.1 Class Summary</h3>

public sealed class Expression&lt;TDelegate&gt; : LambdaExpression {

public new TDelegate Compile();

public new TDelegate Compile(DebugInfoGenerator debugInfoGenerator)

<h2 id="debuginfogenerator-class">4.44 DebugInfoGenerator Class</h2>

This abstract class represents a debug information writer for use with LambdaExpression Compile methods. This is in the System.RuntimeCompilerServices namespace.

There is a static factory method for generating a default writer that emits a pdb file.

This class allows you to own the debug info writing, or to intercept it. You might create a default writer and wrap it in a customer writer. The custom writer can create other ways to get at the info (for example, for creating a debugging experience within an RIA under Silverlight where normal pdb's aren't available). Your custom writer can also delegate to the default writer for creating normal pdb's as well.

<h3 id="class-summary-38">4.44.1 Class Summary</h3>

public abstract class DebugInfoGenerator {

public static DebugInfoGenerator CreatePdbGenerator() {

return new SymbolDocumentGenerator();

public abstract void MarkSequencePoint

(LambdaExpression method, int ilOffset,

DebugInfoExpression sequencePoint);

<h3 id="debuginfogenerator-method">4.44.2 DebugInfoGenerator Method</h3>

This static factory method returns a default info write that generates a pdb file.

Signature:

public static DebugInfoGenerator CreatePdbGenerator() {

return new SymbolDocumentGenerator();

<h3 id="marksequencepoint-method">4.44.3 MarkSequencePoint Method</h3>

This method takes sequence point information from LambdaExpression.Compile\* methods. Method is the LambdaExpression being compiled, and ilOffset is the instruction counter offset into the dynamic methods IL. SequencePoint is the debug info marker expression in the ET for which this sequence point is being emitted.

Signature:

public abstract void MarkSequencePoint

(LambdaExpression method, int ilOffset,

DebugInfoExpression sequencePoint);

<h2 id="parameterexpression-class">4.45 ParameterExpression Class</h2>

This class represents a reference to a variable defined in the containing context. This node uses the Parameter node kind as a legacy from ETs v1.

Variables must be listed using ParameterExpressions as parameters for LambdaExpression or as lexicals in a BlockExpression to in effect define them in some sub tree. To reference a variable, you alias the ParameterExpression object used to define the variable. Note, while Parameter node references are what determine variable binding, you can declare the same Parameter object in nested BlockExpressions. The ET compiler resovles the references to the tightest containing Block that declares the Parameter.

The Name property is purely for debugging or pretty printing purposes and has no semantic value whatsoever.

Closure environments and lifting variables happens automatically as needed when compiling ETs.

<h3 id="class-summary-39">4.45.1 Class Summary</h3>

public class ParameterExpression : Expression {

public Boolean IsByRef { get; }

public String Name { get; }

<h3 id="isbyref-property">4.45.2 IsByRef Property</h3>

This property returns whether the variable is a parameter passed by reference instead of by value. This can only be true for parameters to LambdaExpression, not for variables in BlockExpression. Parameters marked IsByRef cannot be lifted to a closure environment.

An example of creating a ByRef parameter follows:

``` csharp
delegate void RefDelegate(ref int a);
...
```

// In some code somewhere ...

``` csharp
    var parameter = Expression.Parameter(typeof(int).MakeByRefType(), "x");
    var lambda = Expression.Lambda<RefDelegate>
        (Expression.Assign(parameter, Expression.Constant(123)), 
         new[] { parameter });
    var d = lambda.Compile();
    Console.WriteLine(lambda.Parameters[0].IsByRef);
    int x = 0;
    d(ref x);
    Console.WriteLine(x);
```

Signature:

public Boolean IsByRef { get; }

<h3 id="name-property-2">4.45.3 Name Property</h3>

This property returns the string name of the variable for debugging or pretty printing only. It has no semantic meaning, and it may be null.

Signature:

public String Name { get; }

<h3 id="factory-methods-25">4.45.4 Factory Methods</h3>

Expressions have the following factory methods for ParameterExpressions:

public static ParameterExpression Parameter

(Type type, String name);

public static ParameterExpression Parameter(Type type)

public static ParameterExpression Variable

(Type type, String name);

public static ParameterExpression Variable(Type type)

Type must be non-null. The resulting node has Type and Name set to the factory arguments.

<h2 id="runtimevariablesexpression-class">4.46 RuntimeVariablesExpression Class</h2>

This class represents variables that need to be accessed for getting/setting as lifted closure variables. This class uses the RuntimeVariables node kind.

The Type property of this expression kind is IRuntimeVariables. This could have been IList&lt;IStrongBox&gt;, but having IRuntimeVariables enables the implementation of the closure environment to change over time.

<h3 id="class-summary-40">4.46.1 Class Summary</h3>

public sealed class RuntimeVariablesExpression : Expression {

public ReadOnlyCollection&lt;ParameterExpression&gt; Variables { get; }

public RuntimeVariablesExpression Update

(IEnumerable&lt;ParameterExpression&gt; variables)

<h3 id="variables-property-1">4.46.2 Variables Property</h3>

This property returns the collection of ParameterExpressions representing the variables to close over and make available first class at runtime.

Signaure:

public ReadOnlyCollection&lt;ParameterExpression&gt; Variables { get; }

<h3 id="update-method-24">4.46.3 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public RuntimeVariablesExpression Update

(IEnumerable&lt;ParameterExpression&gt; variables)

<h3 id="factory-methods-26">4.46.4 Factory Methods</h3>

The Expression class has the following factory methods for creating RuntimeVariableExpressions:

public static RuntimeVariablesExpression RuntimeVariables

(IEnumerable&lt;ParameterExpression&gt;

variables);

public static RuntimeVariablesExpression RuntimeVariables

(params ParameterExpression\[\] variables);

If any member of variables is null, these throw an ArgumentNullException.

<h2 id="iruntimevariables-interface">4.47 IRuntimeVariables Interface</h2>

This interface provides access to local variables explicitly requested to be lifted into a closure via a RuntimeVariablesExpression.

<h3 id="class-summary-41">4.47.1 Class Summary</h3>

public interface IRuntimeVariables {

int Count { get; }

object this\[int index\] { get; set; }

<h3 id="count-property">4.47.2 Count Property</h3>

This property returns the number of variables captured by the RuntimeVariablesExpression.

Signature:

int Count { get; }

<h3 id="this-property">4.47.3 This Property</h3>

This property takes an index, zero to Count - 1, to get or set a closed over variable requested via a RuntimeVariablesExpression.

Signature:

object this\[int index\] { get; set; }

<h2 id="switchexpression-class">4.48 SwitchExpression Class</h2>

This class represents a switch or select expression, where possible cases are considered to find a match to a condition value. These nodes use the Switch node kind. Below are summary semantics, but see section for more details on the semantics of the Switch node kind.

These sorts of constructs vary widely across languages (kinds of values you can select over, whether cases can be merged, whether cases fall through to one another, how default cases are handled, etc. SwitchExpression neither matches C\# nor VB exactly, but it has a nice simplicity that supports many uses in those languages.

At a high level, this node's semantics is to evaluate the SwitchValue expression, then to evaluate each SwitchCase's TestValues in order. For each test value, if the SwitchCase.Comparison (invoked on the SwitchValue and TestValue) return True, then the corresponding SwitchCase.Body executes. If no case fires, then the DefaultBody executes. The value resulting from the SwitchExpression is the last expression executed, which is typically the last expression of the selected case body.

If you want the effect of case fall through, then you can use GotoExpression and construct the target case as follows. The case's body can be a BlockExpression, and the first expression in it can be a LabelExpression with a null Expression property. The expression compiler detects patterns for eliminating the goto's.

<h3 id="class-summary-42">4.48.1 Class Summary</h3>

public sealed class SwitchExpression : Expression {

public ReadOnlyCollection&lt;SwitchCase&gt;

Cases { get; }

public MethodInfo Comparison { get; }

public Expression DefaultBody { get; }

public Expression SwitchValue { get; }

public SwitchExpression Update(Expression switchValue,

IEnumerable&lt;SwitchCase&gt; cases,

Expression defaultBody)

<h3 id="cases-property">4.48.2 Cases Property</h3>

This property returns the collection of SwitchCases describing each branch of the SwitchExpression. Each case is considered in the order in which it appears in the collection.

Signature:

public ReadOnlyCollection&lt;SwitchCase&gt;

Cases { get; }

<h3 id="comparison-property">4.48.3 Comparison Property</h3>

This property returns the function that takes as its first argument the SwitchExpression.SwitchValue and each case's test value as the second argument. If it returns True, then the body of the case containing the test value executes.

Signature:

public MethodInfo Comparison { get; }

<h3 id="defaultbody-property">4.48.4 DefaultBody Property</h3>

This property returns the Expression to execute if no cases match the SwitchValue. This property may return null, but only if this SwitchExpression object's Type property represents void.

Signature:

public Expression DefaultBody { get; }

<h3 id="switchvalue-property">4.48.5 SwitchValue Property</h3>

This property returns the test condition value to be compared with all the cases' test values.

public Expression SwitchValue { get; }

<h3 id="update-method-25">4.48.6 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public SwitchExpression Update(Expression switchValue,

IEnumerable&lt;SwitchCase&gt; cases,

Expression defaultBody)

<h3 id="factory-methods-27">4.48.7 Factory Methods</h3>

The Expression class has the following factory methods for creating SwitchExpressions:

public static SwitchExpression Switch(Expression value,

params SwitchCase\[\] cases);

public static SwitchExpression Switch

(Expression switchValue, Expression defaultBody,

params SwitchCase\[\] cases)

public static SwitchExpression Switch

(Expression switchValue, Expression defaultBody,

MethodInfo comparison, params SwitchCase\[\] cases)

public static SwitchExpression Switch

(Type type, Expression switchValue, Expression defaultBody,

MethodInfo comparison, params SwitchCase\[\] cases)

public static SwitchExpression Switch

(Expression switchValue, Expression defaultBody,

MethodInfo comparison, IEnumerable&lt;SwitchCase&gt; cases)

public static SwitchExpression Switch

(Type type, Expression switchValue, Expression defaultBody,

MethodInfo comparison, IEnumerable&lt;SwitchCase&gt; cases)

Comparison defaults to the effect of using Expression.Equal. The comparison function gets invoked with the switch value as the first argument and each case's test value in turn as the second argument.

The Type property, if not supplied via the type parameter, is set to a case body's type, and all SwitchCase.Body.Type properties must represent the same type. This is for simplicity of the compiler in choosing a unifying type and for more explicit correctness by forcing ET producers to specify their exact intentions

If type is supplied as void, then the case bodies can be of any type, and any results are automatically "converted to void" or squelched. If the type is supplied as non-void, then the SwitchCase.Body.Type properties must represent types that are reference assignable to the type represented by the type parameter.

If DefaultBody is null, the Type property must represent void type. If the type parameter is unsupplied, then each SwitchCase.Body.Type must be void. If DefaultBody is non-null, then it's Type property must represent a type that is reference assignable to the type represented by the SwitchExpression's Type property.

If comparison is supplied, then every SwichCase TestValue must have a Type property that represents a type that is reference assignable to the second parameter of the comparison function. If comparison is not supplied, there are more constraints on the test values in case branches. All test value Expressions across every SwitchCase must have exactly the same Type property. The factories effectively call Expression.Equal on value and a case's test value to find a comparison method. These constraints are for simplicity of the compiler in choosing a unifying type and searching for an Equal method as well as simplicity for ET consumer. Note, if your intention is to get reference equality semantics, then you need to wrap the values in Convert to object expressions.

<h2 id="switchcase-class">4.49 SwitchCase Class</h2>

This class represents a single case in a SwitchExpression. While the cases do fit the expression-based model and result in a value, they are not Expressions since they cannot appear anywhere in an ET, only in Switch nodes.

<h3 id="class-summary-43">4.49.1 Class Summary</h3>

public sealed class SwitchCase {

public Expression Body { get; }

public ReadOnlyCollection&lt;Expression&gt;

TestValues { get; }

public SwitchCase Update(IEnumerable&lt;Expression&gt; testValues,

Expression body)

<h3 id="body-property-3">4.49.2 Body Property</h3>

This property returns the Expression to execute if one of this case's test values is equal to the containing Switch's SwitchValue.

Signature:

public Expression Body { get; }

<h3 id="testvalues-property">4.49.3 TestValues Property</h3>

This property returns the collection of test value Expressions to consider in order to determine whether to select this case.

Signature:

public ReadOnlyCollection&lt;Expression&gt;

TestValues { get; }

<h3 id="update-method-26">4.49.4 Update Method</h3>

This method creates a new expression that is like this one, but using the supplied children. If all of the children are the same, this method returns this expression instead of creating a new one. This is useful when implementing ExpressionVisitors to rewrite the current node only when you need to do so.

Signature:

public SwitchCase Update(IEnumerable&lt;Expression&gt; testValues,

Expression body)

<h3 id="factory-methods-28">4.49.5 Factory Methods</h3>

The Expression class has the following factory for creating SwitchCases objects:

public static SwitchCase SwitchCase(Expression body,

params Expression\[\] testValues)

public static SwitchCase SwitchCase

(Expression body, IEnumerable&lt;Expression&gt; testValues)

If the SwitchExpression to which the resulting SwitchCase gets added has an explicit comparison function, then each of the testValues must have a Type property that represents a type that is reference assignable to the second parameter of the comparison function. Otherwise all test value Expressions must have exactly the same Type property. This is for simplicity of the compiler in choosing a unifying type and searching for an Equal method as well as simplicity for ET consumer.

<h2 id="expressionvisitor-class">4.50 ExpressionVisitor Class</h2>

This class provides a visitor framework for ET nodes. With Extension node kinds and reducibility, providing a walker model is important for saving everyone effort and having well-behaved extensions. Without a common walking mechanism built-in, everyone would have to fully reduce extension nodes to walk them. Reducing is lossy for meta-programming because usually you can't go back to the original ET, especially if you're rewriting parts of the tree.

The ExpressionVisitor class is abstract with two main entry points and many methods for sub classes to override. The entry points visit an arbitrary Expression or collection of Expressions. The methods for sub classes to override correspond to the node types. For example, if you only care to inspect or act on BinaryExpressions and ParameterExpressions, you'd override VisitBinary and VisitParameter. The methods you override all have default implementations that just visit their children. If the result of visiting a child produces a new node, then the default implementations construct a new node of the same kind, filling in the new children. If the child comes back identity equal, then the default implementations just returns the node passed in.

As an Extension node kind author, you should override Expression.VisitChildren to visit your sub expressions. Furthermore, if any children come back as new objects, you should reconstruct your node type with the new children, returning the new node as your result. By default Expression.VisitChildren reduces the expression to all common nodes and then calls the visitor on the result.

The following subsections only spec the members that have design motivation or some behavior worth noting. Otherwise, the methods by default visit their children and create new instances of the node types or helper objects (for example, CatchBlock) if any children changed when they were visited.

<h3 id="rebinding-when-children-nodes-type-properties-change">4.50.1 Rebinding When Children Nodes' Type Properties Change</h3>

There are constraints in the default visitor methods regarding Type property changes when children get rewritten. Changing the types of children can cause the parent node to rebind its semantics when it creates a new instance of itself. For example, an Add node of two integers could become an Add node of a user-defined integer wrapper and an integer, where the user-defined op\_Add method adversely affects the original ET's semantics. Since accidental semantics changes are more likely programmer errors, the default visitor methods resist these changes.

If you intend to rewrite children node Type properties and cause rebinding in parent nodes, you'll need to explicitly override the parent node visitor method to control the rewriting and rebinding of the parent node's semantics. This should not mean you'll need to override all methods. Most likely you'll only need to override BinaryExpression and UnaryExpression, maybe a couple of others.

Some nodes inherently resist when type changes occur from rewrites. If the parent node has an explicit MethodInfo stating its semantics, then the default implementations of the visit methods call the factories with the method info. The factories will check that the new operands are still assignable to the method's parameters.

When there is no explicit method info, if the children nodes are primitive types, the default implementations require the children's types to remain the same after rewriting. If the new Type properties represent reference types, then there are a few cases:

- **Generally**:

- If the node has a methodinfo, the default implementation of the visitor method just calls the factory which confirms the methodinfo info still applies.

- If there's no method info, and the children had value types, the rewritten children must have the same value types as before.

- If there's no method info, and the children had reference types, the rewritten children must have Type properties representing types that are reference assignable to the original types represented by the children. Again, the default implementations check this by just calling the factory and ensuring the result type is still valid for the operation, and no new methodinfo got selected.

- **Equal (and NotEqual)**: if the Equal/NotEqual node had reference equality, then the default visitor method preserves these semantics rather than rebinding to user-defined equality (operand Type properties don’t matter then). Otherwise, the method info must fit the new children types.

- **Switch**: If the node had no methodinfo for comparing the switch value with the test values, and the new Swtich node with the rewritten children must not have an inferred methodinfo.

- **Convert, Coalesce, TypeAs**: these nodes are about ensuring their expression's Type aligns or unifies with other nodes. Since the methodInfo the conversion semantics, the general rules apply.

<h3 id="class-summary-44">4.50.2 Class Summary</h3>

public abstract class ExpressionVisitor {

protected ExpressionVisitor();

protected static ReadOnlyCollection&lt;T&gt; Visit&lt;T&gt;

(ReadOnlyCollection&lt;T&gt; nodes, Func&lt;T,T&gt; elementVisitor);

protected ReadOnlyCollection&lt;Expression&gt; Visit

(ReadOnlyCollection&lt;Expression&gt; nodes);

// Visit is virtual just for consistency with the visitor sample

// popular with ETs v1. You should never need to override it.

public virtual Expression Visit(Expression node);

protected ReadOnlyCollection&lt;T&gt; VisitAndConvert&lt;T&gt;

(ReadOnlyCollection&lt;T&gt; nodes, String callerName)

where T : Expression;

protected T VisitAndConvert&lt;T&gt;(T node, String callerName)

where T : Expression;

protected virtual Expression VisitBinary

(BinaryExpression node);

protected virtual Expression VisitBlock

(BlockExpression node);

protected virtual CatchBlock VisitCatchBlock(CatchBlock node);

protected virtual Expression VisitConditional

(ConditionalExpression node);

protected virtual Expression VisitConstant

(ConstantExpression node);

protected virtual Expression VisitDebugInfo

(DebugInfoExpression node);

protected virtual Expression VisitDefault

(DefaultExpression node);

protected virtual Expression VisitDynamic

(DynamicExpression node);

protected virtual ElementInit VisitElementInit(ElementInit node);

protected virtual Expression VisitExtension

(Expression node);

protected virtual Expression VisitGoto

(GotoExpression node);

protected virtual Expression VisitIndex

(IndexExpression node);

protected virtual Expression VisitInvocation

(InvocationExpression node);

protected virtual Expression VisitLabel

(LabelExpression node);

protected virtual LabelTarget VisitLabelTarget

(LabelTarget node);

protected virtual Expression VisitLambda&lt;T&gt;

(Expression&lt;T&gt; node);

protected virtual Expression VisitListInit

(ListInitExpression node);

protected virtual Expression VisitLoop

(LoopExpression node);

protected virtual Expression VisitMember

(MemberExpression node);

protected virtual MemberAssignment VisitMemberAssignment

(MemberAssignment node);

protected virtual MemberBinding VisitMemberBinding

(MemberBinding node);

protected virtual Expression VisitMemberInit

(MemberInitExpression node);

protected virtual MemberListBinding VisitMemberListBinding

(MemberListBinding binding);

protected virtual MemberMemberBinding VisitMemberMemberBinding

(MemberMemberBinding node);

protected virtual Expression VisitMethodCall

(MethodCallExpression node);

protected virtual Expression VisitNew

(NewExpression node);

protected virtual Expression VisitNewArray

(NewArrayExpression node);

protected virtual Expression VisitParameter

(ParameterExpression node);

protected virtual Expression VisitRuntimeVariables

(RuntimeVariablesExpression node);

protected virtual Expression VisitSwitch

(SwitchExpression node);

protected virtual SwitchCase VisitSwitchCase(SwitchCase node);

protected virtual Expression VisitTry

(TryExpression node);

protected virtual Expression VisitTypeBinary

(TypeBinaryExpression node);

protected virtual Expression VisitUnary

(UnaryExpression node);

<h3 id="visitt-method">4.50.3 Visit&lt;T&gt; Method</h3>

This method iterates over nodes invoking elementVisitor on each. If any invocation returns a new instance of the T, then Visit&lt;T&gt; copies the collection, aliasing unchanged elements and point to the new ones elementVisitor created.

This method is for convenience, and it is used in the default implementations of several methods to walk children (for example, SwitchCases, CatchBlocks, MemberBindings, etc.). It is protected so that you can use it as well.

Signature:

protected static ReadOnlyCollection&lt;T&gt; Visit&lt;T&gt;

(ReadOnlyCollection&lt;T&gt; nodes, Func&lt;T,T&gt; elementVisitor);

<h3 id="visitlambdat-method">4.50.4 VisitLambda&lt;T&gt; Method</h3>

This method visits the parameters and the body of a LambdaExpression. It requires the generic parameter to solve a couple of rewriting issues. While the T is the same delegate type as LambdaExpression.Type, in some cases it isn't possible to pass the delegate type to the factories for constructing new LambdaExpressions. When some code, A, is rewriting a Lambda node from other code, B, that has a private delegate type, A cannot always invoke the factories with the delegate type (for example, in partial trust). Due to how access works in .NET, when the delegate type flow in via the T parameter, you can call the factories with the private delegate type.

Signature:

protected virtual Expression VisitLambda&lt;T&gt;

(Expression&lt;T&gt; node);

<h3 id="visitandconvertt-method">4.50.5 VisitAndConvert&lt;T&gt; Method</h3>

This method effectively calls Visit on all the nodes, and if any node gets changed by the visitor, this method ensures the new node is the same type as the old node.

This is a convenience method for iterating nodes like BlockExpression.Variables or LambdaExpression.Parameters. It takes a string for the caller's name, and if there's a rewriting error, the this method throws an exception with a message saying you should override the calling visit method to handle iterating over the nodes or handle the rewrites yourself. This is protected so that you can use it too in derived visitors.

Signature:

protected ReadOnlyCollection&lt;T&gt; VisitAndConvert&lt;T&gt;

(ReadOnlyCollection&lt;T&gt; nodes, String callerName)

where T : Expression;

protected T VisitAndConvert&lt;T&gt;(T node, String callerName)

where T : Expression;

<h3 id="visitconstant-method">4.50.6 VisitConstant Method</h3>

This method by default just returns the node with no recursive visiting.

Signature:

protected virtual Expression VisitConstant

(ConstantExpression node);

<h3 id="visitdebuginfo-method">4.50.7 VisitDebugInfo Method</h3>

This method by default just returns the node with no recursive visiting.

Signature:

protected virtual Expression VisitDebugInfo

(DebugInfoExpression node);

<h3 id="visitdynamic-method">4.50.8 VisitDynamic Method</h3>

This method by default just visits the argument expressions.

Signature:

protected virtual Expression VisitDynamic

(DynamicExpression node);

<h3 id="visitdefault-method">4.50.9 VisitDefault Method</h3>

This method by default just returns the node with no recursive visiting.

Signature:

protected virtual Expression VisitDefault

(DefaultExpression node);

<h3 id="visitextension-method">4.50.10 VisitExtension Method</h3>

This method by default calls node.VisitChildren.

Signature:

protected virtual Expression VisitExtension

(Expression node);

<h3 id="visitlabeltarget-method">4.50.11 VisitLabelTarget Method</h3>

This method by default just returns the node with no recursive visiting.

Signature:

protected virtual Expression VisitLabelTarget

(LabelTarget node);

<h3 id="visitmember-method">4.50.12 VisitMember Method</h3>

This method by default just visits the object expression.

Signature:

protected virtual Expression VisitMember

(MemberExpression node);

<h3 id="visitnew-method">4.50.13 VisitNew Method</h3>

This method by default just visits the argument expressions. If any argument expression changes, then this method creates a new NewExpression with the same Members if there are any.

Signature:

protected virtual Expression VisitNew

(NewExpression node);

<h3 id="visitparameter-method">4.50.14 VisitParameter Method</h3>

This method by default just returns the node with no recursive visiting.

Signature:

protected virtual Expression VisitParameter

(ParameterExpression node);

<h2 id="post-clr-4.0----globalvariableexpression-class">4.51 POST CLR 4.0 -- GlobalVariableExpression Class</h2>

This type models a variable look up that requires hosting context. The variable gets looked up in a current ScriptScope in which he code is executing or along a chain of ScriptScopes including the ScriptRuntime.Globals ScriptScope.

This node's kind is Extension.

<h3 id="class-summary-45">4.51.1 Class Summary</h3>

public sealed class GlobalVariableExpression : Expression {

public Boolean IsLocal { get; }

public String Name { get; }

<h2 id="post-clr-4.0----generatorexpression">4.52 POST CLR 4.0 -- GeneratorExpression</h2>

This type models code that can contain YieldExpressions. This is not shipping in CLR 4.0 and is only available on [www.codeplex.com/dlr](http://www.codeplex.com/dlr) . This node reduces by generating an ET that embodies the state machine needed to return values, re-enter the generator, and re-establish any dynamic context such as try-catch's.

This node's kind is Extension.

While this expression is generally useful, it implements a couple of behaviors specific to Python and needs to be generalized, particularly around yield in finally blocks and re-entering the generator with a value as the result of a YieldExpression.

<h3 id="class-summary-46">4.52.1 Class Summary</h3>

public sealed class GeneratorExpression : Expression {

public Expression Body { get; }

public LabelTarget Target { get; }

<h3 id="body-property-4">4.52.2 Body Property</h3>

This property returns the code to build into the generator's state machine that computes values to yield.

<h3 id="target-property-2">4.52.3 Target Property</h3>

This property returns the representation of the place in the code that YieldExpressions will Goto to return values from the generator.

<h2 id="post-clr-4.0----yieldexpression-class">4.53 POST CLR 4.0 -- YieldExpression Class</h2>

This type models locations in GeneratorExpressions where the code you return a value, and where the generator's state machine needs to be able to re-enter to continue computing values. It is only valid to use YieldExpression nodes in a sub ET with a GeneratorExpression root node.

This node's kind is Extension.

While this expression is generally useful, it implements a couple of behaviors specific to Python and needs to be generalized, particularly around yield in finally blocks and re-entering the generator with a value as the result of a YieldExpression.

<h3 id="class-summary-47">4.53.1 Class Summary</h3>

public sealed class YieldExpression : Expression {

public LabelTarget Target { get; }

public Expression Value { get; }

public Int32 YieldMarker { get; }

<h3 id="target-property-3">4.53.2 Target Property</h3>

This property returns the representation of the place in the code that YieldExpressions will Goto to return values from the generator. It must match the Target property of the GeneratorExpression node that is the root of the sub ET containing this YieldExpression.

Signature:

public LabelTarget Target { get; }

<h3 id="value-property-2">4.53.3 Value Property</h3>

This property returns the expression that models the value the generator returns at this point in the code.

Signature:

public Expression Value { get; }

<h3 id="yieldmarker-property">4.53.4 YieldMarker Property</h3>

This property returns a unique value within the GeneratorExpression that is the root of the sub ET containing this YieldExpression. This unique value identifies this YieldExpression for tree rewriters. The generator object returned from GeneratorExpression has a method on it

The uniqueness of this value is the responsibility of the node creator, and the DLR does not ensure it is unique.

Signature:

public Int32 YieldMarker { get; }

<h2 id="cut-annotations-class">4.54 CUT ~~Annotations Class~~</h2>

~~This class represents a collection of information associated with an Expression node instance. The information is keyed by a type. An Annotations object can have more than one element of a given type.~~

~~Annotations instances are immutable. Of course, users can add an annotation member that is an indirection point, from which they can maintain mutable information. You might do this in some processing pass where an annotation changes, but you do not want to incur the cost of copying the sub ET from the node to the root of its containing tree over and over.~~

<h3 id="class-summary-48">4.54.1 ~~Class Summary~~</h3>

~~\[SerializableAttribute\]~~

~~public sealed class Annotations : IEnumerable&lt;System.Object&gt;,~~

~~IEnumerable {~~

~~public static readonly Annotations Empty;~~

~~public Annotations Add&lt;T&gt;(T annotation);~~

~~public Boolean Contains&lt;T&gt;();~~

~~public T Get&lt;T&gt;();~~

~~public Annotations Remove&lt;T&gt;();~~

~~public Boolean TryGet&lt;T&gt;(out T annotation);~~

<h3 id="empty-field">4.54.2 ~~Empty Field~~</h3>

~~This field returns the empty Annotations object from which you can start your own Annotations collection.~~

~~Signature:~~

~~public static readonly Annotations Empty;~~

<h3 id="addt-method">4.54.3 ~~Add&lt;T&gt; Method~~</h3>

~~This method returns a copy of the Annotations object with the argument added.~~

~~Should we guarantee new element is at the end for purposes of Get and TryGet semantics?~~

~~Signature:~~

~~public Annotations Add&lt;T&gt;(T annotation);~~

<h3 id="containst-method">4.54.4 ~~Contains&lt;T&gt; Method~~</h3>

~~This method returns true if there are any elements of type T, otherwise it returns false.~~

~~Signature:~~

~~public Boolean Contains&lt;T&gt;();~~

<h3 id="gett-method">4.54.5 ~~Get&lt;T&gt; Method~~</h3>

~~This method returns the first element of type T found in the Annotations object. If there are no such elements, then it returns the default instance of T.~~

~~Signature:~~

~~public T Get&lt;T&gt;();~~

<h3 id="trygett-method">4.54.6 ~~TryGet&lt;T&gt; Method~~</h3>

~~This method returns true and sets its argument the first element of type T found in the Annotations object. If there are no such elements, then it returns false and sets the argument to the default instance of T.~~

~~Signature:~~

~~public Boolean TryGet&lt;T&gt;(out T annotation);~~

<h3 id="removet-method">4.54.7 ~~Remove&lt;T&gt; Method~~</h3>

~~This method returns a new Annotations object that is a copy of this Annotations object with all elements of type T removed from it.~~

~~Signature:~~

~~public Annotations Remove&lt;T&gt;();~~
