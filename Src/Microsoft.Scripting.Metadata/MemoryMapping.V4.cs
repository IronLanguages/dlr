// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Scripting.Metadata {
    [SecurityCritical]
    public unsafe sealed class MemoryMapping : CriticalFinalizerObject {
        [SecurityCritical]
        internal byte* _pointer;
        
        private SafeMemoryMappedViewHandle _handle;
        internal long _capacity;

        [CLSCompliant(false)]
        public byte* Pointer {
            [SecurityCritical]
            get {
                if (_pointer == null) {
                    throw new ObjectDisposedException("MemoryMapping");
                }
                return _pointer;
            }
        }

        public long Capacity {
            get { return _capacity; }
        }

        public MemoryBlock GetRange(int start, int length) {
            if (_pointer == null) {
                throw new ObjectDisposedException("MemoryMapping");
            }
            if (start < 0) {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (length < 0 || length > _capacity - start) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            return new MemoryBlock(this, _pointer + start, length);
        }

        [SecuritySafeCritical]
        public static MemoryMapping Create(string path) {
            MemoryMappedFile file = null;
            MemoryMappedViewAccessor accessor = null;
            SafeMemoryMappedViewHandle handle = null;
            MemoryMapping mapping = null;
            FileStream stream = null;
            byte* ptr = null;
            
            try {
                stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite);

#if NET45
                file = MemoryMappedFile.CreateFromFile(stream, null, 0, MemoryMappedFileAccess.Read, null, HandleInheritability.None, true);
#else
                file = MemoryMappedFile.CreateFromFile(stream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
#endif
                accessor = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                mapping = new MemoryMapping();

                // we need to make sure that the handle and the acquired pointer get stored to MemoryMapping:
                RuntimeHelpers.PrepareConstrainedRegions();
                try { } finally {
                    handle = accessor.SafeMemoryMappedViewHandle;
                    handle.AcquirePointer(ref ptr);
                    if (ptr == null) {
                        throw new IOException("Cannot create a file mapping");
                    }
                    mapping._handle = handle;
                    mapping._pointer = ptr;
                    mapping._capacity = accessor.Capacity;
                }
            } finally {
                stream?.Dispose();
                accessor?.Dispose();
                file?.Dispose();
            }
            return mapping;
        }

        [SecuritySafeCritical]
        ~MemoryMapping() {
            if (_pointer == null) {
                // uninitialized:
                return;
            }

            // It is not safe to close the view at this point if there are any MemoryBlocks alive.
            // It's up to the user to ensure not to use them. Since you need unmanaged code priviledge to use them
            // this is not a security issue (it would be if this API was security safe critical).
            _handle.ReleasePointer();
            _pointer = null;
        }
    }
}
