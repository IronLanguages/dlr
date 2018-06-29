// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation {
    internal static partial class DelegateHelpers {

        private static Dictionary<ICollection<Type>, Type> _DelegateTypes;

        private static Type MakeCustomDelegate(Type[] types) {
            if (_DelegateTypes == null) {
                Interlocked.CompareExchange(
                    ref _DelegateTypes,
                    new Dictionary<ICollection<Type>, Type>(ListEqualityComparer<Type>.Instance),
                    null
                );
            }

            bool found;
            Type type;

            //
            // LOCK to retrieve the delegate type, if any
            //

            lock (_DelegateTypes) {
                found = _DelegateTypes.TryGetValue(types, out type);
            }

            if (!found && type != null) {
                return type;
            }

            //
            // Create new delegate type
            //
            type = MakeNewCustomDelegate(types);

            //
            // LOCK to insert new delegate into the cache. If we already have one (racing threads), use the one from the cache
            //
            lock (_DelegateTypes) {
                if (_DelegateTypes.TryGetValue(types, out Type conflict) && conflict != null) {
                    type = conflict;
                } else {
                    _DelegateTypes[types] = type;
                }
            }

            return type;
        }

        private static Type MakeNewCustomDelegate(Type[] types) {
#if FEATURE_REFEMIT
            Type returnType = types[types.Length - 1];
            Type[] parameters = types.RemoveLast();

            return Snippets.Shared.DefineDelegate("Delegate" + types.Length, returnType, parameters);
#else
            throw new NotSupportedException("Signature not supported on this platform");
#endif
        }
    }
}
