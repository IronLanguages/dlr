// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading.Tasks;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    ///   The kind of an <see cref="AsyncBodyEvent"/>. Each kind documents the type of the event's <see cref="AsyncBodyEvent.Payload"/>.
    /// </summary>
    /// <remarks>
    ///   Each stretch of the body running on one thread opens with <see cref="Started"/> or <see cref="Resumed"/> and closes with
    ///   <see cref="Awaiting"/>, <see cref="Yielding"/> or <see cref="Returning"/>.
    /// </remarks>
    public enum AsyncBodyEventKind {
        /// <summary>
        ///   Before the body runs its first code. Payload: none (<see langword="null"/>).
        /// </summary>
        Started = 1,

        /// <summary>
        ///   Before an await of a task that hasn't completed. Payload: that <see cref="Task"/> to be awaited.
        /// </summary>
        Awaiting,

        /// <summary>
        ///   Before an async generator hands a value to its consumer. Payload: the value to be yielded.
        /// </summary>
        Yielding,

        /// <summary>
        ///   On the thread that continues the body. Payload: <see langword="null"/>, or an <see cref="Exception"/> the body never sees:
        ///   the one the awaited task failed with, when an interrupt from the <see cref="AsyncInterruptSource"/> arrived before the
        ///   body continued and was raised at the await instead. It is always <see langword="null"/> after yielding.
        /// </summary>
        Resumed,

        /// <summary>
        ///   The body has returned. Payload: the result; <see langword="null"/> for an async generator.
        /// </summary>
        Returning,
    }

    /// <summary>
    ///   A notification from a running async body, that of an <see cref="Ast.AsyncExpression"/> or an
    ///   <see cref="Ast.AsyncEnumerableExpression"/>, to the <see cref="IObserver{T}"/> set by their <c>WithObserver</c>.
    /// </summary>
    /// <remarks>
    ///   <para>The observer is called synchronously, on the thread running the body, before the body continues. The calls for one body
    ///   follow Rx's grammar: <see cref="IObserver{T}.OnNext"/> any number of times, then <see cref="IObserver{T}.OnCompleted"/> or
    ///   <see cref="IObserver{T}.OnError"/>, which is final. <see cref="AsyncBodyEventKind.Started"/> comes first, then pairs of 
    ///   <see cref="AsyncBodyEventKind.Awaiting"/> or <see cref="AsyncBodyEventKind.Yielding"/>
    ///   and <see cref="AsyncBodyEventKind.Resumed"/>, and <see cref="AsyncBodyEventKind.Returning"/> last, if the body returns.</para>
    ///
    ///   <para><see cref="IObserver{T}.OnError"/> receives the exception the body's task fails with, or its async enumeration
    ///   throws: an exception the body raises, or an interrupt from its <see cref="AsyncInterruptSource"/> that the body hasn't
    ///   observed when it returns. An interrupt pending before the start fails the body without running it, and is then the only
    ///   call. An exception the body catches isn't reported through <see cref="IObserver{T}.OnError"/>.</para>
    ///
    ///   <para>An async generator whose consumer abandons it at a <c>yield</c> gets <see cref="IObserver{T}.OnCompleted"/> after
    ///   <see cref="AsyncBodyEventKind.Yielding"/>, with no <see cref="AsyncBodyEventKind.Returning"/>. An async generator that is
    ///   never enumerated, or is abandoned before it starts, gets no calls at all, so an observer must not rely on a final call
    ///   unless it has seen <see cref="AsyncBodyEventKind.Started"/>.</para>
    ///
    ///   <para>The body's interrupt source is closed before the final call. An observer must not throw. If it does, the exception
    ///   leaves the driver: the body's task fails with it, or its async enumeration throws it, the body is abandoned, and the
    ///   observer gets no further calls.</para>
    /// </remarks>
    public readonly struct AsyncBodyEvent(AsyncBodyEventKind kind, object? payload) {

        /// <summary>
        ///   What has happened.
        /// </summary>
        public AsyncBodyEventKind Kind { get; } = kind;

        /// <summary>
        ///   The object that comes with the event. Its type depends on <see cref="Kind"/>; see each kind's documentation.
        /// </summary>
        public object? Payload { get; } = payload;
    }
}
