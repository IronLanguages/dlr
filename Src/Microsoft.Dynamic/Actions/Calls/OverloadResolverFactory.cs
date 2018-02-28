// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions.Calls {
    public abstract class OverloadResolverFactory {
        public abstract DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType);
    }
}
