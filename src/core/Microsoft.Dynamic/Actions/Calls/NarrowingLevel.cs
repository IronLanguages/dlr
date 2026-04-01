// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// Narrowing conversions are conversions that cannot be proved to always succeed, conversions that are 
    /// known to possibly lose information, and conversions across domains of types sufficiently different 
    /// to merit narrowing notation like casts. 
    /// 
    /// Its upto every language to define the levels for conversions. The narrowling levels can be used by
    /// for method overload resolution, where the overload is based on the parameter types (and not the number 
    /// of parameters).
    /// </summary>
    public enum NarrowingLevel {
        /// <summary>
        /// Conversions at this level do not do any narrowing. Typically, this will include
        /// implicit numeric conversions, Type.IsAssignableFrom, StringBuilder to string, etc.
        /// </summary>
        None,
        /// <summary>
        /// Language defined prefered narrowing conversion.  First level that introduces narrowing
        /// conversions.
        /// </summary>
        One,
        /// <summary>
        /// Language defined preferred narrowing conversion.  Second level that introduces narrowing
        /// conversions and should have more conversions than One.
        /// </summary>
        Two,
        /// <summary>
        /// Language defined preferred narrowing conversion.  Third level that introduces narrowing
        /// conversions and should have more conversions that Two.
        /// </summary>
        Three,
        /// <summary>
        /// A somewhat meaningful conversion is possible, but it will quite likely be lossy.
        /// For eg. BigInteger to an Int32, Boolean to Int32, one-char string to a char,
        /// larger number type to a smaller numeric type (where there is no overflow), etc
        /// </summary>
        All
    }
}
