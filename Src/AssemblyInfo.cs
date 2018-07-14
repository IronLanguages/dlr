// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyProduct("Dynamic Language Runtime")]

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif

[assembly: AssemblyCompany("DLR Open Source Team")]
[assembly: AssemblyCopyright("© DLR Contributors.")]

[assembly: AllowPartiallyTrustedCallers]

// Versioning
[assembly: AssemblyVersion(DynamicLanguageRuntime.CurrentVersion.AssemblyVersion)]
[assembly: AssemblyFileVersion(DynamicLanguageRuntime.CurrentVersion.AssemblyFileVersion)]
[assembly: AssemblyInformationalVersion(DynamicLanguageRuntime.CurrentVersion.AssemblyInformationalVersion)]
