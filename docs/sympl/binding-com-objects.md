# 19 Binding COM Objects

This support has been moved to Codeplex only (after the writing of the Sympl sample and this document). This is in the Microsoft.Dynamic.dll. It is the COM binding C\# uses, but C\# uses it privately now. Sympl has been updated with a new using statement to continue working. Note that this is the one bit of functionality that Sympl demonstrates in the base csharp directory that is not shipping functionality in .NET 4.0.

Having your language work late bound using IDispatch on COM objects is very easy. You can look at any of the FallbackX methods to see how Sympl does this, but here's an example from FallbackGetMember in runtime.cs:

public override DynamicMetaObject FallbackGetMember(

DynamicMetaObject targetMO,

DynamicMetaObject errorSuggestion) {

// First try COM binding.

DynamicMetaObject result;

if (ComBinder.TryBindGetMember(this, targetMO, out result,

true)) {

return result;

You can look at the code for ComBinder, and for each TryBindX member, add coded similar to the above to your corresponding XBinder.

TryBindGetMember is the only one with the odd Boolean flag. It accommodates the distinction between languages like Python and C\#. Python has a strict model of getting a member and then calling it; there is no invoke member. If ComBinder.TryBindGetMember can prove the member is parameterless and a data member, then it can produce a rule for eager evaluation. If either test fails, then passing true means TryBindGetMember returns a rule for lazily fetching the value as a callable wrapper. When the flag is false, ComBider always eagerly evaluates, and if the member requires parameters that aren't available, then ComBinder returns a rule that throws. C\# passes false here. It doesn't really matter for Sympl, which just copied this code from IronPython.
