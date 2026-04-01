// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Represents an ops-extension method which is added as an operator.
    /// 
    /// The name must be a well-formed name such as "Add" that matches the CLS
    /// naming conventions for adding overloads associated with op_* methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OperatorSlotAttribute : Attribute {
        public OperatorSlotAttribute() { }
    }
}
