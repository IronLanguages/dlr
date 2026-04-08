# 15 Indexing Expressions: GetIndex and SetIndex

Sympl supports indexing its built-in lists, arrays, and indexers/indexed properties. Expression Trees v1 had an ArrayIndex factory that would return either a BinaryExpression for single-dimensional arrays or a MethodCallExpression for multi-dimensional arrays. These now exist only for LINQ backward compatibility. All new code should use the ArrayAccess or MakeIndex factories that return IndexExpressions. Expression Trees v2 support IndexExpressions everywhere, including the left hand side of assignments and as byref arguments.

SymplGetIndexBinder and SymplSetIndexBinder both use the RuntimeHelpers method GetIndexingExpression. It does most of the work for FallbackGetIndex. FallbackSetIndex has to do some extra work.

<h2 id="symplgetindexbinders-fallbackgetindex">15.1 SymplGetIndexBinder's FallbackGetIndex</h2>

Here's the code from runtime.cs, which is described further below:

public override DynamicMetaObject FallbackGetIndex(

DynamicMetaObject target, DynamicMetaObject\[\] indexes,

DynamicMetaObject errorSuggestion) {

// ... Deleted checking for COM and need to Defer for now ...

// Give good error for Cons.

if (target.LimitType == typeof(Cons)) {

if (indexes.Length != 1)

return errorSuggestion ??

RuntimeHelpers.CreateThrow(

target, indexes, BindingRestrictions.Empty,

typeof(InvalidOperationException),

"Indexing list takes single index. " +

"Got " + indexes.Length.ToString());

}

var indexingExpr =

RuntimeHelpers.EnsureObjectResult(

RuntimeHelpers.GetIndexingExpression(target,

indexes));

var restrictions = RuntimeHelpers.GetTargetArgsRestrictions(

target, indexes, false);

return new DynamicMetaObject(indexingExpr, restrictions);

Let's first talk about what we aren't talking about now. This code snippet omits the code to check if the target is a COM object and to use built-in COM support. See section for information adding this to your binders. The snippet also omits some very important code that protects binders and DynamicMetaObjects from infinitely looping due to producing bad rules. It is best to discuss this in one place, so see section for how the infinite loop happens and how to prevent it for all binders.

As we said before, GetIndexingExpression does most of the work here. Before calling it, FallbackGetIndex checks if the indexing is for Sympl built-in lists and whether the argument count is right. After calling GetIndexingExpression, the expression passes through EnsureObjectResult in case it needs to be wrapped to ensure it is strictly typed as assignable to object. For more information, see section 3.2.4. FallbackGetIndex uses the binding helper GetTargetArgsRestrictions to get restrictions and returns the resulting DynamicMetaObject with the indexing expression and restrictions.

<h2 id="getindexingexpression">15.2 GetIndexingExpression</h2>

SymplGetIndexBinder and SymplSetIndexBinder both use the RuntimeHelpers method GetIndexingExpression. It does most of the work FallbackGetIndex. FallbackSetIndex has to do some extra work.

Here's the code from RuntimeHelpers in runtime.cs, which is further explained below:

public static Expression GetIndexingExpression(

DynamicMetaObject target,

DynamicMetaObject\[\] indexes) {

Debug.Assert(target.HasValue &&

target.LimitType != typeof(Array));

var indexExpressions = indexes.Select(

i =&gt; Expression.Convert(i.Expression, i.LimitType))

.ToArray();

// HANDLE CONS

if (target.LimitType == typeof(Cons)) {

// Call RuntimeHelper.GetConsElt

var args = new List&lt;Expression&gt;();

// The first argument is the list

args.Add(

Expression.Convert(

target.Expression,

target.LimitType)

);

args.AddRange(indexExpressions);

return Expression.Call(

typeof(RuntimeHelpers),

"GetConsElt",

null,

args.ToArray());

// HANDLE ARRAY

} else if (target.LimitType.IsArray) {

// the target has an array type

return Expression.ArrayAccess(

Expression.Convert(target.Expression,

target.LimitType),

indexExpressions

);

// HANDLE INDEXERS

} else {

var props = target.LimitType.GetProperties();

var indexers = props.

Where(p =&gt; p.GetIndexParameters().Length &gt; 0).ToArray();

indexers = indexers.

Where(idx =&gt; idx.GetIndexParameters().Length ==

indexes.Length).ToArray();

var res = new List&lt;PropertyInfo&gt;();

foreach (var idxer in indexers) {

if (RuntimeHelpers.ParametersMatchArguments(

idxer.GetIndexParameters(),

indexes)) {

// all parameter types match

res.Add(idxer);

}

}

if (res.Count == 0) {

return Expression.Throw(

Expression.New(

typeof(MissingMemberException)

.GetConstructor(new Type\[\]

{ typeof(string) }),

Expression.Constant(

"Can't bind because there is no " +

"matching indexer.")

)

);

}

return Expression.MakeIndex(

Expression.Convert(target.Expression, target.LimitType),

res\[0\], indexExpressions);

The first thing GetIndexingExpression does is get ConvertExpressions for all the indexing arguments. It converts them to the specific LimitType of the DynamicMetaObject. See section for a discussion of using LimitType over RuntimeType. It may seem odd to convert the object to the type that LimitType reports it to be, but the type of the meta-object's expression might be more general and require an explicit Convert node to satisfy the strict typing of the Expression Tree factory or the actual emitted code that executes. The Expression Tree compiler removes unnecessary Convert nodes.

The first kind of indexing Sympl supports is its built-in lists (target's LimitType is Cons). In this case the GetIndexingExpression creates a ConvertExpression for the target to its LimitType, just like the index arguments discussed above. Then it takes the converted target and argument expressions to create a MethodCallExpression for RuntimeHelpers.GetConsElt. You can see this runtime helper in runtime.cs.

The second kind of indexing Sympl supports is arrays (target's LimitType IsArray). In this case GetIndexingExpression also creates a ConvertExpression for the target to its LimitType. Then it takes the converted target and argument expressions to create an IndexExpression.

The third kind of indexing Sympl supports is looking for an indexer or indexed property. GetIndexingExpression gets the target's properties and filters for those whose parameter count matches the index arguments count. Then it filters for matching parameter types, which is described in section . If no properties match, GetIndexingExpression returns a Throw expression (doesn't use CreateThrow here since it returns a DynamicMetaObject). Finally GetIndexingExpression calls MakeIndex to return an IndexExpression. It also converts the target to its LimitType, as discussed above.

<h2 id="symplsetindexbinders-fallbacksetindex">15.3 SymplSetIndexBinder's FallbackSetIndex</h2>

Here's the code from runtime.cs, which is described further below:

public override DynamicMetaObject FallbackSetIndex(

DynamicMetaObject target, DynamicMetaObject\[\] indexes,

DynamicMetaObject value,

DynamicMetaObject errorSuggestion) {

// ... Deleted checking for COM and need to Defer for now ...

Expression valueExpr = value.Expression;

if (value.LimitType == typeof(TypeModel)) {

valueExpr = RuntimeHelpers.GetRuntimeTypeMoFromModel(value)

.Expression;

}

Debug.Assert(target.HasValue &&

target.LimitType != typeof(Array));

Expression setIndexExpr;

if (target.LimitType == typeof(Cons)) {

if (indexes.Length != 1) {

return errorSuggestion ??

RuntimeHelpers.CreateThrow(

target, indexes, BindingRestrictions.Empty,

typeof(InvalidOperationException),

"Indexing list takes single index. " +

"Got " + indexes);

}

// Call RuntimeHelper.SetConsElt

List&lt;Expression&gt; args = new List&lt;Expression&gt;();

// The first argument is the list

args.Add(

Expression.Convert(

target.Expression,

target.LimitType)

);

// The second argument is the index.

args.Add(Expression.Convert(indexes\[0\].Expression,

indexes\[0\].LimitType));

// The last argument is the value

args.Add(Expression.Convert(valueExpr, typeof(object)));

// Sympl helper returns value stored.

setIndexExpr = Expression.Call(

typeof(RuntimeHelpers),

"SetConsElt",

null,

args.ToArray());

} else {

Expression indexingExpr =

RuntimeHelpers.GetIndexingExpression(target,

indexes);

setIndexExpr = Expression.Assign(indexingExpr, valueExpr);

}

BindingRestrictions restrictions =

RuntimeHelpers.GetTargetArgsRestrictions(target, indexes,

false);

return new DynamicMetaObject(

RuntimeHelpers.EnsureObjectResult(setIndexExpr),

restrictions);

Let's first talk about what we aren't talking about now. This code snippet omits the code to check if the target is a COM object and to use built-in COM support. See section for information adding this to your binders. The snippet also omits some very important code that protects binders and DynamicMetaObjects from infinitely looping due to producing bad rules. It is best to discuss this in one place, so see section for how the infinite loop happens and how to prevent it for all binders.

At a high level FallbackSetIndex examines the value meta-object, then gets an expression to set the index, then forms restrictions, and lastly returns the resulting DynamicMetaObject representing the bound operation. The value processing is just a check for whether to convert TypeModel to a RuntimeType meta-object. See section for a discussion of the restrictions. The only difference here is the false value to get a type restriction on the target.

To determine the indexing expression, FallbackSetIndex checks for the target being a Cons. If it is, the binder needs to call the RuntimeHelpers.SetConsElt method. The binder first checks the number of arguments and whether it should call CreateThrow. See section for a discussion of CreateThrow and restrictions. As discussed with GetIndexingExpression, the binder creates a ConvertExpression to convert the target to its LimitType, and does the same for the index arguments. The binder converts the value to the Type object because that's what the runtime helper takes. Finally, the resulting indexing expression for setting a Cons element is a MethodCallExpression for SetConsElt. This helper returns the value stored to be in compliance with the convention for binders and meta-objects.

In the alternative branch, FallbackSetIndex uses the GetIndexingExpression binding helper. The binder then wraps that in an Assign node. This guarantees returning the value stored also.

In either case, the operation implementation expression passes through EnsureObjectResult in case it needs to be wrapped to ensure it is strictly typed as assignable to object. For more information, see section 3.2.4.
