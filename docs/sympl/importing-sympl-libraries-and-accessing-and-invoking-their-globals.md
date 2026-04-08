# 9 Importing Sympl Libraries and Accessing and Invoking Their Globals

Most of the ground work for importing libraries and accessing other file module's globals has been done. Section on Import code generation discusses some of how this works too. Since file scopes are ExpandoObjects, their DynamicMetaObjects handle the member accesses. When Sympl imports another file of Sympl code, Sympl stores into the importing file's globals scope the name of the file (no directory or extension) using the imported file's module object (an ExpandoObject) as the value. Sympl code can then just dot into these like any other object with members.

Sympl adapts a trick from python for loading files from the same directory that contains the Sympl file doing the importing . You can see in the code excerpt in section that Sympl stores into each file's module globals a special name, "\_\_file\_\_", bound to the full path of the source file. If you look at the RuntimeHelpers.SymplImport in runtime.cs, you can see it fetches this name to build a path for importing a library of Sympl code.

The rest of the support for fetching library globals just falls out of emitting GetMember DynamicExpressions. At this point in Sympl's implementation, the SymplGetMemberBinder still just conveys the name and ignoreCase metadata for binding. That changes after Sympl adds type instantiation, coming up next.

The InvokeMember story is a bit different. Consider this Sympl code:

(imports lists)

(lists.append '(a b c) '(1 2 3))

Sympl compiles "(lists.append" to an InvokeMember DynamicExpression. The meta-object for the 'lists' module's ExpandoObject gets calls to bind the InvokeMember operation. ExpandoObject's don't supply a 'this' or otherwise have a notion of InvokeMember. They do a GetMember using the name on the binder and then call the binder's FallbackInvoke method. FallbackInvoke can just invoke the object received as the target argument. FallbackInvoke for some languages might check to see if the object is a special runtime callable object with special calling conventions or properties. This is described more fully in section .

At this point in Sympl's implementation evolution, its SymplInvokeMemberBinder had a FallbackInvokeMember method that just returned the result of CreateThrow. Its FallbackInvoke method was essentially:

return new DynamicMetaObject(

Expression.Dynamic(

new SymplInvokeBinder(new CallInfo(args.Length)),

typeof(object),

argexprs),

targetMO.Restrictions.Merge(

BindingRestrictions.Combine(args)));

This was not only enough to get cross-library calls working, it became the final implementation. The code in SymplInvokeBinder.FallbackInvoke (discussed in section ) is doing all the work in the nested CallSite resulting from this DynamicExpression.
