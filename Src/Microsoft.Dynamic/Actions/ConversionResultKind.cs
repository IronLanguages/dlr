// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Determines the result of a conversion action.  The result can either result in an exception, a value that
    /// has been successfully converted or default(T), or a true/false result indicating if the value can be converted.
    /// </summary>
    public enum ConversionResultKind {
        /// <summary>
        /// Attempts to perform available implicit conversions and throws if there are no available conversions.
        /// </summary>
        ImplicitCast,
        /// <summary>
        /// Attempst to perform available implicit and explicit conversions and throws if there are no available conversions.
        /// </summary>
        ExplicitCast,
        /// <summary>
        /// Attempts to perform available implicit conversions and returns default(ReturnType) if no conversions can be performed.
        /// 
        /// If the return type of the rule is a value type then the return value will be zero-initialized.  If the return type
        /// of the rule is object or another class then the return type will be null (even if the conversion is to a value type).
        /// This enables ImplicitTry to be used to do TryConvertTo even if the type is value type (and the difference between
        /// null and a real value can be distinguished).
        /// </summary>
        ImplicitTry,
        /// <summary>
        /// Attempts to perform available implicit and explicit conversions and returns default(ReturnType) if no conversions 
        /// can be performed.
        /// 
        /// If the return type of the rule is a value type then the return value will be zero-initialized.  If the return type
        /// of the rule is object or another class then the return type will be null (even if the conversion is to a value type).
        /// This enables ExplicitTry to be used to do TryConvertTo even if the type is value type (and the difference between
        /// null and a real value can be distinguished).
        /// </summary>
        ExplicitTry
    }
}
