// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Scripting.Utils {
    public static class ExceptionUtils {
        public static ArgumentOutOfRangeException MakeArgumentOutOfRangeException(string paramName, object actualValue, string message) {
            return new ArgumentOutOfRangeException(paramName, actualValue, message);
        }

        public static ArgumentNullException MakeArgumentItemNullException(int index, string arrayName) {
            return new ArgumentNullException($"{arrayName}[{index}]");
        }

#if FEATURE_REMOTING
        public static object GetData(this Exception e, object key) {
            return e.Data[key];
        }

        public static void SetData(this Exception e, object key, object data) {
            e.Data[key] = data;
        }

        public static void RemoveData(this Exception e, object key) {
            e.Data.Remove(key);
        }
#else
        private static ConditionalWeakTable<Exception, List<KeyValuePair<object, object>>> _exceptionData;

        public static void SetData(this Exception e, object key, object value) {
            if (_exceptionData == null) {
                Interlocked.CompareExchange(ref _exceptionData, new ConditionalWeakTable<Exception, List<KeyValuePair<object, object>>>(), null);
            }

            lock (_exceptionData) {
                var data = _exceptionData.GetOrCreateValue(e);
            
                int index = data.FindIndex(entry => entry.Key == key);
                if (index >= 0) {
                    data[index] = new KeyValuePair<object, object>(key, value);
                } else {
                    data.Add(new KeyValuePair<object, object>(key, value));
                }
            }
        }

        public static object GetData(this Exception e, object key) {
            if (_exceptionData == null) {
                return null;
            }

            lock (_exceptionData) {
                List<KeyValuePair<object, object>> data;
                if (!_exceptionData.TryGetValue(e, out data)) {
                    return null;
                }

                return data.FirstOrDefault(entry => entry.Key == key).Value;
            }
        }

        public static void RemoveData(this Exception e, object key) {
            if (_exceptionData == null) {
                return;
            }

            lock (_exceptionData) {
                List<KeyValuePair<object, object>> data;
                if (!_exceptionData.TryGetValue(e, out data)) {
                    return;
                }

                int index = data.FindIndex(entry => entry.Key == key);
                if (index >= 0) {
                    data.RemoveAt(index);
                }
            }
        }
#endif
    }
}
