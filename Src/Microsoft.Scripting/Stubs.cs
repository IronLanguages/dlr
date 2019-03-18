// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

#if !FEATURE_SERIALIZATION

namespace System {
    using System.Diagnostics;

#pragma warning disable CA1018
    [Conditional("STUB")]
    public sealed class SerializableAttribute : Attribute {
    }

    [Conditional("STUB")]
    public sealed class NonSerializedAttribute : Attribute {
    }
#pragma warning restore CA1018

#pragma warning disable CA1040
    namespace Runtime.Serialization {
        public interface ISerializable {
        }

        public interface IDeserializationCallback {
        }
    }
#pragma warning restore CA1040

    public class SerializationException : Exception {
    }
}

#endif
