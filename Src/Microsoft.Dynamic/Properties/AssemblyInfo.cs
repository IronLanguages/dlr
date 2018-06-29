// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

[assembly: AssemblyTitle("Microsoft.Dynamic")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: CLSCompliant(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1bbee69c-30c5-41df-8912-b81da6d658c2")]

#if FEATURE_SECURITY_RULES
[assembly: System.Security.SecurityRules(SecurityRuleSet.Level1)]
#endif

[assembly: SecurityTransparent]

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]
[assembly: InternalsVisibleTo("TestInternalDLR, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c10ce00dd2e0ce5046d68183d3ad035b47e92bf0ce7bcf8a03a217ca5d0b0c7db973fdf97579b52b502a23d4069dbf043389e1ab65a1d6c508a9837f3e2350f15e05cc63c0fc4b0410867a51919090e4c33f80203e9b0035b21c32bae20f98b068f90d99a50133a5336480d94039b176519f5fd8524765f33be43da65c4b68ba")]

[assembly: TypeForwardedTo(typeof(Microsoft.Scripting.Runtime.DynamicStackFrame))]
