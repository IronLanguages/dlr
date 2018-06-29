// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Convention for an individual argument at a callsite.
    /// 
    /// Multiple different callsites can match against a single declaration. 
    /// Some argument kinds can be "unrolled" into multiple arguments, such as list and dictionary. 
    /// </summary>
    public enum ArgumentType {
        /// <summary>
        /// Simple unnamed positional argument.
        /// In Python: foo(1,2,3) are all simple arguments.
        /// </summary>
        Simple,

        /// <summary>
        /// Argument with associated name at the callsite
        /// In Python: foo(a=1)
        /// </summary>
        Named,

        /// <summary>
        /// Argument containing a list of arguments. 
        /// In Python: foo(*(1,2*2,3))  would match 'def foo(a,b,c)' with 3 declared arguments such that (a,b,c)=(1,4,3).
        ///      it could also match 'def foo(*l)' with 1 declared argument such that l=(1,4,3)
        /// </summary>
        List,

        /// <summary>
        /// Argument containing a dictionary of named arguments.
        /// In Python: foo(**{'a':1, 'b':2})
        /// </summary>
        Dictionary,


        Instance
    };
}
