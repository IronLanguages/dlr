// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>
    /// Represents information about a failure to convert an argument from one
    /// type to another.
    /// </summary>
    public sealed class ConversionResult {
        private readonly object _arg;

        internal ConversionResult(object arg, Type argType, Type toType, bool failed) {
            _arg = arg;
            ArgType = argType;
            To = toType;
            Failed = failed;
        }

        /// <summary>
        /// Value of the argument or null if it is not available.
        /// </summary>
        public object Arg => _arg;

        /// <summary>
        /// Argument actual type or its limit type if the value not known.
        /// DynamicNull if the argument value is null.
        /// </summary>
        public Type ArgType { get; }

        public Type To { get; }

        public bool Failed { get; }

        internal static void ReplaceLastFailure(IList<ConversionResult> failures, bool isFailure) {
            ConversionResult failure = failures[failures.Count - 1];
            failures.RemoveAt(failures.Count - 1);
            failures.Add(new ConversionResult(failure.Arg, failure.ArgType, failure.To, isFailure));
        }

        public string GetArgumentTypeName(ActionBinder binder) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return (_arg != null) ? binder.GetObjectTypeName(_arg) : binder.GetTypeName(ArgType);
        }
    }
}
