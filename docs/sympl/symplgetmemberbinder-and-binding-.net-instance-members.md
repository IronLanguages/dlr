# 11 SymplGetMemberBinder and Binding .NET Instance Members

Now that Sympl can instantiate types it is worth fleshing out its GetMemberBinder. At runtime, when trying to access a member of a .NET static object, the default .NET meta-object will call FallbackGetMember on SymplGetMemberBinder. This code is much simpler than the code for binding InvokeMember we looked at before. As a reminder, if the object that flows into the CallSite is some dynamic object, then it's DynamicMetaObject's BindGetMember will produce a rule for fetching the member.

Here's the code from runtime.cs, which is described further below:

public class SymplGetMemberBinder : GetMemberBinder {

public SymplGetMemberBinder(string name) : base(name, true) {

}

public override DynamicMetaObject FallbackGetMember(

DynamicMetaObject targetMO,

DynamicMetaObject errorSuggestion) {

// ... Deleted checking for COM and need to Defer for now ...

var flags = BindingFlags.IgnoreCase \| BindingFlags.Static \|

BindingFlags.Instance \| BindingFlags.Public;

var members = targetMO.LimitType.GetMember(this.Name, flags);

if (members.Length == 1) {

return new DynamicMetaObject(

RuntimeHelpers.EnsureObjectResult(

Expression.MakeMemberAccess(

Expression.Convert(targetMO.Expression,

members\[0\].DeclaringType),

members\[0\])),

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

"cannot bind member, " + this.Name +

", on object " + targetMO.Value.ToString());

Let's first talk about what we aren't talking about now. This code snippet omits the code to check if the target is a COM object and to use built-in COM support. See section for information adding this to your binders. The snippet also omits some very important code that protects binders and DynamicMetaObjects from infinitely looping due to producing bad rules. It is best to discuss this in one place, so see section for how the infinite loop happens and how to prevent it for all binders.

FallbackGetMember uses .NET reflection to get the member with the name in the binder's metadata. If there's exactly one, the binder returns a DynamicMetaObject result with a MemberExpression. It uses the MakeMemberAccess factory which figures out whether the member is a property or a field, and otherwise throws. FallbackGetMember includes a ConvertExpression to ensure the target expression is the member's DeclaringType (expressions tend to flow through Sympl as type object). The operation implementation expression passes through EnsureObjectResult in case it needs to be wrapped to ensure it is strictly typed as assignable to object. For more information, see section 3.2.4.

For restrictions, this binder doesn't need the binding helpers since it only has to test the target object. The restriction uses the target's LimitType, see section for a discussion of using LimitType over RuntimeType. The restriction does not use DeclaringType because the rule was built using members from LimitType. You might think we need a restriction for the name to prevent the rule from firing for any name. However, this rule is only good on CallSites that point to this binder, and it only returns rules for this one name.

If there isn't exactly one member, then Sympl either uses the suggested result or creates a DynamicMetaObject result that throws an Exception. See section for a discussion of CreateThrow and restrictions. See section 12 for a discussion of errorSuggestion arguments to binders.
