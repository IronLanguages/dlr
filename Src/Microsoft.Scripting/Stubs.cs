// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

#if WIN8

// When compiled with Dev10 VS CSC reports errors if this is not defined
// error CS0656: Missing compiler required member 'System.Threading.Thread.get_ManagedThreadId'
// error CS0656: Missing compiler required member 'System.Threading.Thread.get_CurrentThread'
namespace System.Threading {
    internal class Thread {
        public int ManagedThreadId { get { throw new NotImplementedException(); } }
        public static Thread CurrentThread { get { throw new NotImplementedException(); } }
    }
}

namespace System.IO {
    [Serializable]
    public enum FileMode {
        CreateNew = 1,
        Create,
        Open,
        OpenOrCreate,
        Truncate,
        Append
    }

    [Serializable]
    public enum FileAccess {
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }

    [Serializable]
    public enum FileShare {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = 3,
        Delete = 4,
        Inheritable = 16
    }
}

#endif

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
