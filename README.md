Dynamic Language Runtime
========================
The Dynamic Language Runtime enables language developers to more easily create dynamic
languages for the .NET platform. In addition to being a pluggable back-end for dynamic
language compilers, the DLR provides language interop for dynamic operations on
objects. The DLR has common hosting APIs for using dynamic languages as libraries or
for scripting in your .NET applications.

Installation
------------
The best way to install the DLR is through the NuGet DynamicLanguageRuntime package.
For now you will need to enable prerelease packages, as there is no production NuGet
release yet.

Documentation
-------------
The best current documentation is in the Docs/ directory, in Word and PDF format (it
*was* a Microsoft project, after all).

Help
----
If you have any questions, [open an issue](https://github.com/IronLanguages/dlr/issues/new), even if it's not an actual bug. The issues are an acceptable discussion forum as well.

History
-------
The original DLR site is at http://dlr.codeplex.com. The DLR was part of a much larger repository containing IronPython and IronRuby as well; you can find it at https://github.com/IronLangauges/main/. This is a smaller repository containing just the DLR, which makes it easier to package and should make it easier to do more regular releases.

Changes
-------
###1.2.0-alpha0
* First official NuGet release from the split repository.
