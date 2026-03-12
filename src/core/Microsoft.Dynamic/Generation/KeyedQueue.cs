// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Scripting.Generation {
    
    /// <summary>
    /// A simple dictionary of queues, keyed off a particular type
    /// This is useful for storing free lists of variables
    /// </summary>
    internal sealed class KeyedQueue<K, V> {
        private readonly Dictionary<K, Queue<V>> _data;

        internal KeyedQueue() {
            _data = new Dictionary<K, Queue<V>>();
        }

        internal void Enqueue(K key, V value) {
            if (!_data.TryGetValue(key, out Queue<V> queue)) {
                _data.Add(key, queue = new Queue<V>());
            }
            queue.Enqueue(value);
        }

        internal V Dequeue(K key) {
            if (!_data.TryGetValue(key, out Queue<V> queue)) {
                throw Error.QueueEmpty();
            }
            V result = queue.Dequeue();
            if (queue.Count == 0) {
                _data.Remove(key);
            }
            return result;
        }

        internal bool TryDequeue(K key, out V value) {
            if (_data.TryGetValue(key, out Queue<V> queue) && queue.Count > 0) {
                value = queue.Dequeue();
                if (queue.Count == 0) {
                    _data.Remove(key);
                }
                return true;
            }
            value = default(V);
            return false;
        }

        internal V Peek(K key) {
            if (!_data.TryGetValue(key, out Queue<V> queue)) {
                throw Error.QueueEmpty();
            }
            return queue.Peek();
        }

        internal int GetCount(K key) {
            if (!_data.TryGetValue(key, out Queue<V> queue)) {
                return 0;
            }
            return queue.Count;
        }

        internal void Clear() {
            _data.Clear();
        }
    }
}
