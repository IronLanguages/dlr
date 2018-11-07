// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// The delegate representing the DLR Main function
    /// </summary>
    // TODO: remove in favor of Func<Scope, LanguageContext, object>
    public delegate object DlrMainCallTarget(Scope scope, LanguageContext context);
}
