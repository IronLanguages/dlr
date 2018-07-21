// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

#if !FEATURE_SERIALIZATION

namespace System {
    using System.Diagnostics;

    [Conditional("STUB")]
    public class SerializableAttribute : Attribute {
    }

    [Conditional("STUB")]
    public class NonSerializedAttribute : Attribute {
    }

    namespace Runtime.Serialization {
        public interface ISerializable {
        }

        public interface IDeserializationCallback {
        }
    }

    public class SerializationException : Exception {
    }
}

#endif
