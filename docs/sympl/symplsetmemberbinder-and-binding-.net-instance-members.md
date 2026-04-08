# 13 SymplSetMemberBinder and Binding .NET Instance Members

At runtime, when trying to set a member of a .NET static object, the default .NET meta-object will call FallbackSetMember on SymplSetMemberBinder. There is more code to setting members than getting them, and by convention, the resulting DynamicMetaObject's expression needs to ensure it returns the value stored. As a reminder, if the object that flows into the CallSite is some dynamic object, then it's DynamicMetaObject's BindSetMember will produce a rule for setting the member.

Here's the code for SymplSetMemberBinder's FallbackSetMember from runtime.cs, which is essentially all there is to the class:

public override DynamicMetaObject FallbackSetMember(

DynamicMetaObject targetMO, DynamicMetaObject value,

DynamicMetaObject errorSuggestion) {

// ... Deleted checking for COM and need to Defer for now ...

var flags = BindingFlags.IgnoreCase \| BindingFlags.Static \|

BindingFlags.Instance \| BindingFlags.Public;

var members = targetMO.LimitType.GetMember(this.Name, flags);

if (members.Length == 1) {

MemberInfo mem = members\[0\];

Expression val;

if (mem.MemberType == MemberTypes.Property)

val = Expression.Convert(

value.Expression,

((PropertyInfo)mem).PropertyType);

else if (mem.MemberType == MemberTypes.Field)

val = Expression.Convert(value.Expression,

((FieldInfo)mem).FieldType);

else

return (errorSuggestion ??

RuntimeHelpers.CreateThrow(

targetMO, null,

BindingRestrictions.GetTypeRestriction(

targetMO.Expression,

targetMO.LimitType),

typeof(InvalidOperationException),

"Sympl only supports setting Properties " +

"and fields at this time."));

return new DynamicMetaObject(

RuntimeHelpers.EnsureObjectResult(

Expression.Assign(

Expression.MakeMemberAccess(

Expression.Convert(targetMO.Expression,

members\[0\].DeclaringType),

members\[0\]),

val)),

BindingRestrictions.GetTypeRestriction(

targetMO.Expression,

targetMO.LimitType));

} else {

return errorSuggestion ??

RuntimeHelpers.CreateThrow(

targetMO, null,

BindingRestrictions.GetTypeRestriction(

targetMO.Expression,

targetMO.LimitType),

typeof(MissingMemberException),

"IDynObj member name conflict.");

Let's first talk about what we aren't talking about now. This code snippet omits the code to check if the target is a COM object and to use built-in COM support. See section for information adding this to your binders. The snippet also omits some very important code that protects binders and DynamicMetaObjects from infinitely looping due to producing bad rules. It is best to discuss this in one place, so see section for how the infinite loop happens and how to prevent it for all binders.

FallbackSetMember uses .NET reflection to get the member with the name in the binder's metadata. If there's exactly one, the binder needs to confirm the kind of member for two reasons. The first is to make sure the kind of member is supported for setting in Sympl. The second is due to .NET's reflection API not having a single name for getting the type of values the member can store. FallbackSetMember creates a ConvertExpression to convert the value to the member's type. To be more correct or consistent, the code should check for the property or field being of type Type and the value being a TypeModel, similar to what ConvertArguments does. In this case, it could build an expression like the helper method GetRuntimeTypeMoFromModel does.

The FallbackSetMember returns a DynamicMetaObject result with an Assign node. The left hand side argument is the same expression created in SymplGetMemberBinder, so see that description for uses of DeclaringType and LimitType in the expression and restrictions. The right hand side is the ConvertExpression discuss above. Expression Tree Assign nodes guarantee returning the value stored, so the binder complies with that convention. The operation implementation expression passes through EnsureObjectResult in case it needs to be wrapped to ensure it is strictly typed as assignable to object. For more information, see section 3.2.4.

For restrictions, this binder doesn't need the binding helpers since it only has to test the target object. Also, since the code doesn't make any decisions based on the property or field's type, having restrictions consistent with the conversions isn't necessary here. If the code were conditional on whether the property or field was assignable, then the result would need more restrictions. You might think we need a restriction for the name to prevent the rule from firing for any name. However, this rule is only good on CallSites that point to this binder, and it only returns rules for this one name.

If there isn't exactly one member, or if the member is not a property or field, then Sympl either uses the suggested result or creates a DynamicMetaObject result that throws an Exception. See section for a discussion of CreateThrow and restrictions. See section 12 for a discussion of errorSuggestion arguments to binders.
