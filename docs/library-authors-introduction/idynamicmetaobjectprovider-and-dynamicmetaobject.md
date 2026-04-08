# 4 IDynamicMetaObjectProvider and DynamicMetaObject

**IDynamicMetaObjectProvider** and **DynamicMetaObject** are the core of the DLR's interoperability protocol. This is the level at which languages plug in for maximum power. For example, IronPython's PythonCallableObject or IronRuby's RubyMutableString implementation objects implement IDynamicMetaObjectProvider and produce their own DynamicMetaObjects for these runtime objects. Their DynamicMetaObjects have full control over how they participate in the interoperability protocol and the DLR's fast dynamic dispatch caching.

During binding of dynamic operations, the DLR checks if a target object implements IDynamicMetaObjectProvider, which has a single method GetMetaObject. If the target object does implement IDynamicMetaObjectProvider, the DLR uses its DynamicMetaObject instead of making a default reflection-based DynamicMetaObject. Binders call on DynamicMetaObjects by convention to give them first crack at binding operations, and an IDynamicMetaObjectProvider's custom DynamicMetaObject can offer its own binding in the form of an expression tree that represents the semantics desired for that requested operation. The DLR combines the expression tree with others and compiles them into a highly-optimized cache using the DLR's fast dynamic dispatch mechanism.

In the case of DynamicObject, this detail is abstracted away. You simply override methods for the dynamic operations in which your dynamic object should participate. The DLR automatically creates a DynamicMetaObject for your DynamicObject and returns expression trees to the dynamic caching that simply call your GetMember method. You do get some of the benefit of caching in that the call to your GetMember function is extremely fast. However you get far more efficiency if you implement IDynamicMetaObjectProvider yourself, where the cache can store an Expression Tree that performs the behavior for that specific name.

<h2 id="fastnbag-faster-bags-for-n-slots">4.1 FastNBag: Faster Bags for N Slots</h2>

Let’s say that we know that the first set of values added to our bag class will be accessed most often, with later additions accessed less frequently. In this case, we can create a custom bag class, FastNBag, which optimizes for that situation by storing the first N members in a fast array. The members added after the first N, go in a dictionary. Our bag can then generate optimized rules for the DLR call site caches by returning expression trees that directly access these array elements for the first N fast members.

After initializing the fast array and the hashtable, the bulk of the logic is then our DynamicMetaObject, MetaFastNBag. Its BindGetMember and BindSetMember methods effectively return expressions that manage adding and fetching the members appropriately. Any access to the first N members is essentially a type check, field access, and index operation at a known index.

You can find the full source for FastNBag in the appendix.

<h3 id="bindsetmember-method">4.1.1 BindSetMember Method</h3>

BindSetMember is the simpler of the two to understand. We’re less concerned with optimizing member assignments than member fetches, so the expression trees remain simpler. They just call our helper method, SetValue:

public override DynamicMetaObject BindSetMember(

SetMemberBinder binder, DynamicMetaObject value)

{

var self = this.Expression;

var keyExpr = Expression.Constant(binder.Name);

var valueExpr = Expression.Convert(

value.Expression,

typeof(object)

);

var target =

Expression.Call(

Expression.Convert(self, typeof(FastNBag)),

typeof(FastNBag).GetMethod("SetValue"),

keyExpr,

valueExpr

);

var restrictions = BindingRestrictions

.GetTypeRestriction(self, typeof(FastNBag));

return new DynamicMetaObject(target, restrictions);

}

The goal of the method is to produce a DynamicMetaObject that represents the result of our binding. When building a DynamicMetaObject, the most important components are the target and the restrictions:

- A DynamicMetaObject’s **target** is the expression tree that represents the desired semantics we’d like for a given operation. In the case of BindSetMember, the tree we generate just calls our general SetValue method in all cases, passing it the binder’s member name as the key and the value object as the value. Our SetValue method decides whether to assign the new entry to the fast array or to the hashtable, and increments a version counter to indicate that our object has changed. This target expression will ultimately be stored in the call site’s cache. When this member is assigned again, the site hits the cache, avoiding another call to BindSetMember and directly calling SetValue. It is often easier to create methods like SetValue that implement the functionality you want and call that from the expression tree, rather than implement all of the logic in the expression tree itself.

- A DynamicMetaObject’s **restrictions** let us constrain when the target expression we’ve specified will be applicable again in the future. A given SetMemberBinder encapsulates a specific member name, so we need to decide which other restrictions will be necessary. In this case, by calling BindingRestrictions.GetTypeRestriction, we’re creating a **type restriction**, constraining this target to apply in all future cases where the bag object has the exact type of FastNBag. This way, if we were to define a subclass of FastNBag with a different implementation, the two types would have distinct cache entries. However, the same cache rule works for multiple instances of one of the types.

<h3 id="bindgetmember-method">4.1.2 BindGetMember Method</h3>

Our more involved caching logic is present inside BindGetMember, where we want accesses to the first N values to go directly to the array, storing the array index inside the target expression as a constant:

public override DynamicMetaObject BindGetMember

(GetMemberBinder binder)

{

var self = this.Expression;

var bag = (FastNBag)base.Value;

int index = bag.GetFastIndex(binder.Name);

Expression target;

// If match found in fast array:

if (index != -1)

{

// Fetch result from fast array.

target =

Expression.Call(

Expression.Convert(self, typeof(FastNBag)),

typeof(FastNBag).GetMethod("GetFastValue"),

Expression.Constant(index)

);

}

// Else, if no match found in fast array, but fast array is full:

else if (bag.fastTable.Count == bag.fastCount)

{

// Fetch result from dictionary.

var keyExpr = Expression.Constant(binder.Name);

var valueExpr = Expression.Variable(typeof(object));

var dictCheckExpr =

Expression.Call(

Expression.Convert(self, typeof(FastNBag)),

typeof(FastNBag).GetMethod("TryGetValue"),

keyExpr,

valueExpr

);

var dictFailExpr =

Expression.Block(

binder.FallbackGetMember(this).Expression,

Expression.Default(typeof(object))

);

target =

Expression.Block(

new \[\] { valueExpr },

Expression.Condition(

dictCheckExpr,

valueExpr,

dictFailExpr

)

);

}

// Else, no match found in fast array, fast array is not yet full:

else

{

// Fail binding, but only until fast array is updated.

var versionCheckExpr =

Expression.Call(

Expression.Convert(self, typeof(FastNBag)),

typeof(FastNBag).GetMethod("CheckVersion"),

Expression.Constant(bag.Version)

);

var versionMatchExpr =

binder.FallbackGetMember(this).Expression;

var updateExpr =

binder.GetUpdateExpression(versionMatchExpr.Type);

target =

Expression.Condition(

versionCheckExpr,

versionMatchExpr,

updateExpr

);

}

var restrictions = BindingRestrictions

.GetInstanceRestriction(self, bag);

return new DynamicMetaObject(target, restrictions);

}

The BindGetMember implementation creates a target and restrictions as did BindSetMember, but creates one of three different target expression trees. The correct binding logic depends on both the member name and the state of the bag in terms of whether we've seen N names yet:

- We first look up the member name to see if it already exists as an entry in the bag’s fast array. If so, the target expression is a call to the GetFastValue method, passing in the specific array index to fetch. As entries will never move around inside the array, this rule will be applicable for this bag forever. Note, we're doing the extra look up work at bind time, but from then on, the fetch is an 'if' test, field access, and array access.

- If the name is not in the fast array, and the fast array is full, this means that the name could only ever appear in the hashtable from now on. In this case, the target expression emitted is a call to the slower TryGetValue method, which checks the hashtable for the member name and returns its value through an out parameter.  
      
    We do not check at bind time whether the member name exists in the hashtable. At runtime, TryGetValue handles missing keys by returning false. That causes execution to flow to the the dictFailExpr branch of the Condition expression. That branch was generated by calling FallbackGetMember to fetch a binding expression from the language's binder. This Fallback call lets the language’s own error semantics surface, such as a language-specific exception or even a special sentinel value like JScript’s “$Undefined”.  
      
    An alternative to FallbackGetMember would be to return a Throw expression tree to throw an exception when member lookup failures or to just throw an exception inside the TryGetValue method. The downside of this approach is that it does not respect the source language’s error semantics in the first case. In the second case, it’s important to avoid actually throwing the exceptions during binding itself as this breaks the binding machinery, which expects to always complete with with an expression. Representing exceptions and fallbacks as expression trees instead of throwing allows these failed bindings to be cached as well.

- If the key is not found in the fast array, and there are still free slots in the array, we have a trickier situation. We know the hashtable must be empty, and so we want the target to throw an exception for now, but this may not be the case forever. As more entries are added to the bag, this rule could become invalid.

To handle the situation where we’ve set fewer than N names, and we're trying to get a value for a name we haven’t seen before, the expression tree changes its behavior based on a version test. Each time the bag sets new member, it increments an internal version number which can be used to tell if the bag has remained the same since a rule was created. In this case, we only want our throw expression to be applicable in the future if the bag is still the same version. We therefore encode a call to our CheckVersion method into the target, hard-coding the current version number. At runtime, if this rule is chosen, it will check that the version number is the same, then throw an exception since we know the member doesn't exist in that version.

> Quick note, we use a distinct version counter in this example for explanation purposes, but you could just write this code using fasttable.Count as the version tick. You only really need to increment the version when you add a new member to the fast table since setting existing members doesn't change the binding logic. Setting existing members would only be interesting to version if, say, you chose to hard code the member's value itself in the rule's target (instead of an array lookup expression).

When the actual version is not the same as the version we "hard coded" into the target expression, we need this target expression to give up. It needs to defer control back to the bag’s DynamicMetaObject to bind the operation again. We do this with a special expression supplied by the binder base class, the Update expression. By inserting the expression returned from DynamicMetaObjectBinder.GetUpdateExpression into a branch of our target expression, we can cause the dynamic call site to rebind and update its cache when the version number does not match. To update, the DLR continue trying cached rules that may apply. If no match is found, the DLR rebinds the CallSite’s operation by calling on the binder. This lets the binder and DynamicMetaObject produce a rule based on the current state of the bag, including the bag’s new version number.

Unlike BindSetMember where we always called SetValue, the rules generated by BindGetMember are specific not just to the bag’s type. BindGetMember's rules use restrictions that match specific instances of the bag since the N entries in the fast array may differ between bags. Therefore, the restriction for the applicability of BindGetMember’s target expression is an **instance restriction**, generated by the helper method BindingRestrictions.GetInstanceRestriction.

Because our FastNBag implements IDynamicMetaObjectProvider, it’s usable in any context where DynamicBag, NamedBag or ExpandoObject were valid before. By tuning our bags using the various mechanisms available to us, we now have bags with a range of performance characteristics. FastNBag should be faster than both DynamicBag and NamedBag as it benefits from using lower-level controls of DLR caching. FastNBag may turn out to be slower than NamedBag if the Name member is not among the first N members while, but is accessed quite often. ExpandoObject should be faster than all of these bags since it has been heavily optimized and uses an even more sophisticated scheme than our FastNBag to ensure quick, cached access for all its members in many different situations.

<h3 id="getdynamicmembernames-method">4.1.3 GetDynamicMemberNames Method</h3>

To aid in debugging programs that use FastNBag, we can implement the GetDynamicMemberNames method on MetaFastNBag.

By returning a sequence of member names as strings from this method, we can let consumers of our object know at runtime which member names we can currently bind. This allows an IDE populate tooltips or other IDE features with information about the members of a dynamic object. For example, Visual Studio shows a “Dynamic View” node in DataTips and in the Locals/Watch windows that expand to list out the dynamic members available on an object.

To provide this member list, we override GetDynamicMemberNames in the MetaFastNBag class:

public override IEnumerable&lt;string&gt; GetDynamicMemberNames()

{

var bag = (FastNBag)base.Value;

return bag.GetKeys();

}

Our implementation calls into a GetKeys method we define in FastNBag:

public IEnumerable&lt;string&gt; GetKeys()

{

var fastKeys = fastTable.Keys;

var hashKeys = hashTable.Keys;

var keys = fastKeys.Concat(hashKeys);

return keys;

}

When an IDE needs to determine the bindable members of a FastNBag instance, it can call IDynamicMetaObjectProvider.GetDynamicMetaObject for the instance and then call GetDynamicMemberNames, which will return the set of keys in both the fast array and the hashtable.

<h2 id="further-reading">4.2 Further Reading</h2>

If you’d like to learn more about IDynamicMetaObjectProvider and DynamicMetaObject, check out the accompanying "Sites, Binders, and Dynamic Object Interop" spec, which covers the underlying mechanisms of the DLR at a deeper level.
