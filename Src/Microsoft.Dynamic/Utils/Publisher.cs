// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;

namespace Microsoft.Scripting.Utils {
    /// <summary>
    /// Thread safe dictionary that allows lazy-creation where readers will block for
    /// the creation of the lazily created value.  Call GetOrCreateValue w/ a key
    /// and a callback function.  If the value exists it is returned, if not the create
    /// callback is called (w/o any locks held).  The create call back will only be called
    /// once for each key.  
    /// </summary>
    public class Publisher<TKey, TValue> {
        private readonly Dictionary<TKey, PublishInfo<TValue>> data = new Dictionary<TKey, PublishInfo<TValue>>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")] // TODO: fix
        public TValue GetOrCreateValue(TKey key, Func<TValue> create) {
            bool lockTaken = false;
            try {
                MonitorUtils.Enter(data, ref lockTaken);

                if (data.TryGetValue(key, out PublishInfo<TValue> pubValue)) {
                    if (pubValue.Value == null && pubValue.Exception == null) {
                        pubValue.PrepareForWait();
                        MonitorUtils.Exit(data, ref lockTaken);

                        try {
                            pubValue.WaitForPublish();
                        } finally {
                            MonitorUtils.Enter(data, ref lockTaken);
                            pubValue.FinishWait();
                        }
                    }

                    if (pubValue.Exception != null) throw new Exception("Error", pubValue.Exception);

                    return pubValue.Value;
                }

                TValue ret;
                // publish the empty PublishInfo
                data[key] = pubValue = new PublishInfo<TValue>();

                // release our lock while we create the new value
                // then re-acquire the lock and publish the info.
                MonitorUtils.Exit(data, ref lockTaken);

                try {
                    try {
                        ret = create();
                        Debug.Assert(ret != null, "Can't publish a null value");
                    } finally {
                        MonitorUtils.Enter(data, ref lockTaken);
                    }
                } catch (Exception e) {
                    pubValue.PublishError(e);
                    throw;
                }

                pubValue.PublishValue(ret);
                return ret;
            } finally {
                if (lockTaken) {
                    Monitor.Exit(data);
                }
            }
        }

        public IEnumerable<TKey> Keys {
            get {
                return data.Keys;
            }
        }

        /// <summary>
        /// Helper class which stores the published value
        /// </summary>
        class PublishInfo<T> {

            // TODO: seems to be FxCop bug
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            public T Value;

            // TODO: seems to be FxCop bug
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            public Exception Exception;

            // TODO: seems to be FxCop bug
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            private ManualResetEvent _waitEvent;

            // TODO: seems to be FxCop bug
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            private int _waiters;

            public void PublishValue(T value) {
                Value = value;
                _waitEvent?.Set();
            }

            public void PublishError(Exception e) {
                Exception = e;
            }

            public void PrepareForWait() {
                if (_waitEvent == null) {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    if (Interlocked.CompareExchange<ManualResetEvent>(ref _waitEvent, mre, null) != null) {
                        mre.Dispose();
                    }
                }
                _waiters++;
            }

            public void WaitForPublish() {
                _waitEvent.WaitOne();
            }

            public void FinishWait() {
                _waiters--;
                if (_waiters == 0) {
                    _waitEvent.Dispose();
                }
            }
        }
    }


}
