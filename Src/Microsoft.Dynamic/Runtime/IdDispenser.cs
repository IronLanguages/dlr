// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    public static class IdDispenser {
        // The one and only comparer instance.
        private static readonly IEqualityComparer<object> _comparer = new WrapperComparer();
        [MultiRuntimeAware]
        private static Dictionary<object, object> _hashtable = new Dictionary<object, object>(_comparer);
        private static readonly Object _synchObject = new Object();  // The one and only global lock instance.
        // We do not need to worry about duplicates that to using long for unique Id.
        // It takes more than 100 years to overflow long on year 2005 hardware.
        [MultiRuntimeAware]
        private static long _currentId = 42; // Last unique Id we have given out.

        // cleanupId and cleanupGC are used for efficient scheduling of hashtable cleanups
        [MultiRuntimeAware]
        private static long _cleanupId; // currentId at the time of last cleanup
        [MultiRuntimeAware]
        private static int _cleanupGC; // GC.CollectionCount(0) at the time of last cleanup

        /// <summary>
        /// Given an ID returns the object associated with that ID.
        /// </summary>
        public static object GetObject(long id) {
            lock (_synchObject) {
                foreach (Wrapper w in _hashtable.Keys) {
                    if (w.Target != null) {
                        if (w.Id == id) return w.Target;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a unique ID for an object
        /// </summary>
        public static long GetId(object o) {
            if (o == null)
                return 0;

            lock (_synchObject) {

                // If the object exists then return it's existing ID.
                if (_hashtable.TryGetValue(o, out object res)) {
                    return ((Wrapper)res).Id;
                }

                long uniqueId = checked(++_currentId);

                long change = uniqueId - _cleanupId;

                // Cleanup the table if it is a while since we have done it last time.
                // Take the size of the table into account.
                if (change > 1234 + _hashtable.Count / 2) {
                    // It makes sense to do the cleanup only if a GC has happened in the meantime.
                    // WeakReferences can become zero only during the GC.
                    int currentGC = GC.CollectionCount(0);
                    if (currentGC != _cleanupGC) {
                        Cleanup();

                        _cleanupId = uniqueId;
                        _cleanupGC = currentGC;
                    } else {
                        _cleanupId += 1234;
                    }
                }

                Wrapper w = new Wrapper(o, uniqueId);
                _hashtable[w] = w;

                return uniqueId;
            }
        }

        /// <summary>
        /// Goes over the hashtable and removes empty entries 
        /// </summary>
        private static void Cleanup() {
            int liveCount = 0;
            int emptyCount = 0;

            foreach (Wrapper w in _hashtable.Keys) {
                if (w.Target != null)
                    liveCount++;
                else
                    emptyCount++;
            }

            // Rehash the table if there is a significant number of empty slots
            if (emptyCount > liveCount / 4) {
                Dictionary<object, object> newtable = new Dictionary<object, object>(liveCount + liveCount / 4, _comparer);

                foreach (Wrapper w in _hashtable.Keys) {
                    if (w.Target != null)
                        newtable[w] = w;
                }

                _hashtable = newtable;
            }
        }

        /// <summary>
        /// Weak-ref wrapper caches the weak reference, our hash code, and the object ID.
        /// </summary>
        private sealed class Wrapper {
            private readonly WeakReference _weakReference;
            private readonly int _hashCode;

            public Wrapper(object obj, long uniqueId) {
                // CF throws doesn't support long weak references (NotSuportedException is thrown)
                _weakReference = new WeakReference(obj, true);

                _hashCode = (obj == null) ? 0 : ReferenceEqualityComparer<object>.Instance.GetHashCode(obj);
                Id = uniqueId;
            }

            public long Id { get; }

            public object Target => _weakReference.Target;

            public override int GetHashCode() {
                return _hashCode;
            }
        }

        /// <summary>
        /// WrapperComparer treats Wrapper as transparent envelope 
        /// </summary>
        private sealed class WrapperComparer : IEqualityComparer<object> {            
            bool IEqualityComparer<object>.Equals(object x, object y) {
                if (x is Wrapper wx)
                    x = wx.Target;

                if (y is Wrapper wy)
                    y = wy.Target;

                return ReferenceEquals(x, y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj) {
                if (obj is Wrapper wobj)
                    return wobj.GetHashCode();

                return GetHashCodeWorker(obj);
            }

            private static int GetHashCodeWorker(object o) {
                if (o == null) return 0;
                return ReferenceEqualityComparer<object>.Instance.GetHashCode(o);
            }
        }
    }
}
