// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Scripting {
    [Serializable]
    public class ArgumentTypeException : Exception {
        public ArgumentTypeException()
            : base() {
        }

        public ArgumentTypeException(string message)
            : base(message) {
        }

        public ArgumentTypeException(string message, Exception innerException)
            : base(message, innerException) {
        }

#if FEATURE_SERIALIZATION
        protected ArgumentTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }
}
