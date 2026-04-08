# 5 Appendix

<h2 id="dynamicobject-virtual-methods">5.1 DynamicObject Virtual Methods</h2>

The 16 virtual DynamicObject methods you may override and the dynamic operations they encode are listed below:

<table>
<thead>
<tr class="header">
<th><strong>Virtual method</strong></th>
<th><strong>Encodes dynamic operation</strong></th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td><strong>TryGetMember</strong></td>
<td><p>Represents an access to an object’s member that retrieves the value.</p>
<p><em>Example:</em> o.m</p></td>
</tr>
<tr class="even">
<td><strong>TrySetMember</strong></td>
<td><p>Represents an access to an object’s member that assigns a value.</p>
<p><em>Example:</em> o.m = 12</p></td>
</tr>
<tr class="odd">
<td><strong>TryDeleteMember</strong></td>
<td><p>Represents an access to an object’s member that deletes the member.</p>
<p><em>Example:</em> delete o.m</p></td>
</tr>
<tr class="even">
<td><strong>TryGetIndex</strong></td>
<td><p>Represents an access to an indexed element of an object or an object’s member that retrieves the value.</p>
<p>The binder may choose to create the element if it doesn’t exist or throw an exception.</p>
<p><em>Example:</em> o[0] <em>or</em> o.m[0]</p></td>
</tr>
<tr class="odd">
<td><strong>TrySetIndex</strong></td>
<td><p>Represents an access to an indexed element of an object or an object’s member that assigns a value.</p>
<p><em>Example:</em> o[0] = 12 <em>or</em> o.m[0] = 12</p></td>
</tr>
<tr class="even">
<td><strong>TryDeleteIndex</strong></td>
<td><p>Represents an access to an indexed element of an object or an object’s member that deletes the element.</p>
<p><em>Example:</em> delete o[0] <em>or</em> delete o.m[0]</p></td>
</tr>
<tr class="odd">
<td><strong>TryInvoke</strong></td>
<td><p>Represents invocation of an invocable object, such as a delegate or first-class function object, with a set of positional/named arguments.</p>
<p><em>Example:</em> a(3)</p>
<p>When binding an expression like a.b(3) in a language with first-class function objects, such as Python, the intermediate object a.b will be requested first with a call to GetMember, and then the invocable object you return will be invoked using Invoke.</p></td>
</tr>
<tr class="even">
<td><strong>TryInvokeMember</strong></td>
<td><p>Represents invocation of an invocable member on an object, such as a method, with a set of positional/named arguments.</p>
<p><em>Example:</em> a.b(3)</p>
<p>InvokeMember is used instead of GetMember + Invoke in languages where invoking a member on an object is an atomic operation. For example, in C#, a.b() may refer to a method group b that represents multiple overloads and would have no intermediate object representation for GetMember implementation to return.</p>
<p>A DynamicObject which does offer a first-class GetMember and Invoke may compose these methods to trivially implement InvokeMember.</p></td>
</tr>
<tr class="odd">
<td><strong>TryCreateInstance</strong></td>
<td><p>Represents an object instantiation with a set of positional/named constructor arguments.</p>
<p><em>Example:</em> new X(3, 4, 5)</p></td>
</tr>
<tr class="even">
<td><strong>TryConvert</strong></td>
<td><p>Represents a conversion of an expression to a target type.</p>
<p>This conversion may be marked in the ConvertBinder parameter as being an implicit compiler-inferred conversion, or an explicit conversion specified by the developer.</p>
<p><em>Example:</em> (TargetType)o</p></td>
</tr>
<tr class="odd">
<td><p><strong>TryUnaryOperation</strong></p>
<p><strong>TryBinaryOperation</strong></p></td>
<td><p>Represents a miscellaneous unary or binary operation, respectively, such as unary minus, or addition.</p>
<p>Contains an Operation string that specifies the operation to perform, such as Add, Subtract, Negate, etc.. There is a core set of operations defined that all language binders should support if they map reasonably to concepts in the language. Languages may also define their own Operation strings for features unique to their language, and may agree independently to share these strings to enable interop for these features.</p>
<p><em>Examples:</em> -a<em>,</em> a + b<em>,</em> a * b</p></td>
</tr>
</tbody>
</table>

<h2 id="fastnbag-full-source">5.2 FastNBag Full Source</h2>

Below is the full source code for the FastNBag implementation discussed above.

using System;

using System.Collections.Generic;

using System.Text;

using System.Linq;

using System.Dynamic;

using System.Linq.Expressions;

using System.ComponentModel;

namespace Bags

{

public class FastNBag : IDynamicMetaObjectProvider, INotifyPropertyChanged

{

private object\[\] fastArray;

private Dictionary&lt;string, int&gt; fastTable;

private Dictionary&lt;string, object&gt; hashTable

= new Dictionary&lt;string, object&gt;();

private readonly int fastCount;

public int Version { get; set; }

public event PropertyChangedEventHandler PropertyChanged;

public FastNBag(int fastCount)

{

this.fastCount = fastCount;

this.fastArray = new object\[fastCount\];

this.fastTable = new Dictionary&lt;string, int&gt;(fastCount);

}

public bool TryGetValue(string key, out object value)

{

int index = GetFastIndex(key);

if (index != -1)

{

value = GetFastValue(index);

return true;

}

else if (fastTable.Count == fastCount)

{

return hashTable.TryGetValue(key, out value);

}

else

{

value = null;

return false;

}

}

public void SetValue(string key, object value)

{

int index = GetFastIndex(key);

if (index != -1)

SetFastValue(index, value);

else

if (fastTable.Count &lt; fastCount)

{

index = fastTable.Count;

fastTable\[key\] = index;

SetFastValue(index, value);

}

else

hashTable\[key\] = value;

Version++;

if (PropertyChanged != null)

{

PropertyChanged(this, new PropertyChangedEventArgs(key));

}

}

public object GetFastValue(int index)

{

return fastArray\[index\];

}

public void SetFastValue(int index, object value)

{

fastArray\[index\] = value;

}

public int GetFastIndex(string key)

{

int index;

if (fastTable.TryGetValue(key, out index))

return index;

else

return -1;

}

public IEnumerable&lt;string&gt; GetKeys()

{

var fastKeys = fastTable.Keys;

var hashKeys = hashTable.Keys;

var keys = fastKeys.Concat(hashKeys);

return keys;

}

public bool CheckVersion(int ruleVersion)

{

return (Version == ruleVersion);

}

public DynamicMetaObject GetMetaObject(Expression parameter)

{

return new MetaFastNBag(parameter, this);

}

private class MetaFastNBag : DynamicMetaObject

{

public MetaFastNBag(Expression expression, FastNBag value)

: base(expression, BindingRestrictions.Empty, value) { }

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

public override IEnumerable&lt;string&gt; GetDynamicMemberNames()

{

var bag = (FastNBag)base.Value;

return bag.GetKeys();

}

}

}

}
