# 6 CreateThrow Runtime Binding Helper

As we noted in the high-level description of execution flow when searching for a binding rule, DynamicMetaObjects and binders should NEVER throw when they fail to bind an operation. Of course, they throw if there's some internal integrity error, but when they cannot produce a binding for an operation, they should return a DynamicMetaObject representing a Throw expression of an appropriate error so that the binding process finishes smoothly. The reason is that another DynamicMetaObject or binder down the chain of execution might handle the operation. There are situations described later where DynamicMetaObjects call on binders to get their rule or error meta-object to fold those into the result the DynamicMetaObject produces.

Since there's enough boiler plate code to creating this ThrowExpression, Sympl has a runtime helper function in the RuntimeHelpers class from runtime.cs:

public static DynamicMetaObject CreateThrow

(DynamicMetaObject target, DynamicMetaObject\[\] args,

BindingRestrictions moreTests,

Type exception, params object\[\] exceptionArgs) {

Expression\[\] argExprs = null;

Type\[\] argTypes = Type.EmptyTypes;

int i;

if (exceptionArgs != null) {

i = exceptionArgs.Length;

argExprs = new Expression\[i\];

argTypes = new Type\[i\];

i = 0;

foreach (object o in exceptionArgs) {

Expression e = Expression.Constant(o);

argExprs\[i\] = e;

argTypes\[i\] = e.Type;

i += 1;

}

}

ConstructorInfo constructor =

exception.GetConstructor(argTypes);

if (constructor == null) {

throw new ArgumentException(

"Type doesn't have constructor with a given signature");

}

return new DynamicMetaObject(

Expression.Throw(

Expression.New(constructor, argExprs),

typeof(object)),

target.Restrictions.Merge(

BindingRestrictions.Combine(args)).Merge(moreTests));

This function takes the target and arguments that were passed to a binder's FallbackX method. Since this ThrowExpression is a binding result (and therefore a rule), it is important to put the right restrictions on it. Otherwise, the rule might fire as a false positives and throw when a successful binding could be found. CreateThrow takes moreTests for this purpose. The function also takes the exception type and arguments for the ThrowExpression.

If there are any arguments to the Exception constructor, CreateThrow gathers their types and Constant expressions for each value. Then it looks up the constructor using the argument types.

CreateThrow passes the object type to the Throw factory to ensure the ThrowExpression's Type property represents the object type. Though Throw never really returns, its factory has this overload precisely to support the strict typing model of Expression Trees. The DLR CallSites use the Expression.Type property also to ensure DynamicMetaObjects and binders return implementations for operations that is strictly typed as assignable to object. For more information on why, see section 3.2.4.

The resulting DynamicMetaObject has default restrictions like Sympl puts on almost all rules its binders produce. Since the restrictions for throwing need to match the restrictions a binder would have produced for a positive rule, there are a couple of calls to CreateThrow that pass several restrictions they got from RuntimeHelpers.GetTargetArgsRestrictions. These restrictions duplicate the default restrictions CreateThrow adds, but fortunately the DLR removes duplicates restrictions when it produces the final expression in DynamicMetaObjectBinder.
