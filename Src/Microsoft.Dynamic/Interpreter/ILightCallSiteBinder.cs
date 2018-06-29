// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using System.Reflection;
using Microsoft.Scripting.Interpreter;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Microsoft.Scripting.Interpreter {
    public interface ILightCallSiteBinder {
        bool AcceptsArgumentArray { get; }
    }
}
