// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    ///   Runtime-async orchestrator for <see cref="Microsoft.Scripting.Ast.AsyncBodyExpression"/>.
    /// </summary>
    /// <remarks>
    ///   The <see cref="Drive"/> method is itself an <c>async Task</c> whose IL is
    ///   produced by Roslyn on .NET 11 under <c>&lt;Features&gt;runtime-async=on&lt;/Features&gt;</c>,
    ///   so each <c>await</c> below becomes a real .NET 11 runtime-async opcode. The state
    ///   machine of the Python function body is delegated to a
    ///   <c>GeneratorRewriter</c>-produced <see cref="IEnumerator{T}"/> of yielded
    ///   tasks; this method awaits each one and feeds the result back through the
    ///   shared result slot. Faulted awaits are routed through the exception slot
    ///   so the body's try/except can observe them at the resumption point — this
    ///   is how <c>async for</c>'s <c>StopAsyncIteration</c> catch works.
    /// </remarks>
    public static class AsyncRunner {
        public static async Task<object?> Drive(IEnumerator<object> states, StrongBox<object?> resultSlot, StrongBox<object?> returnSlot, StrongBox<Exception?> exceptionSlot) {
            while (states.MoveNext()) {
                object yielded = states.Current;
                if (yielded is Task task) {
                    try {
                        await task.ConfigureAwait(false);
                        resultSlot.Value = ExtractTaskResult(task);
                        exceptionSlot.Value = null;
                    } catch (Exception ex) {
                        // The body's rewriter inserts a check after each yield
                        // that rethrows this on the resumed thread, so a Python
                        // try/except inside `await` can catch it.
                        resultSlot.Value = null;
                        exceptionSlot.Value = ex;
                    }
                } else {
                    // Synchronously-produced value forwards straight through.
                    resultSlot.Value = yielded;
                    exceptionSlot.Value = null;
                }
            }
            return returnSlot.Value;
        }

        private static object? ExtractTaskResult(Task task) {
            // The runtime type may be a Task<T> subclass (like AsyncStateMachineBox<TStateMachine,TResult>);
            // walk up until we find Task<T>.
            Type? t = task.GetType();
                while (t is not null) {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>)) {
                    Type arg = t.GenericTypeArguments[0];
                    // Roslyn-emitted async Task methods carry an internal
                    // VoidTaskResult type argument; surface that as null.
                    if (!arg.IsVisible) return null;
                    return t.GetProperty("Result")!.GetValue(task);
                }
                t = t.BaseType;
            }
            return null;
        }
    }
}
