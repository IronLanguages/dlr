﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Linq.Expressions;

using System;
using System.Diagnostics;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// Encapsulates information about the result that should be produced when 
    /// a OldDynamicAction cannot be performed.  The ErrorInfo can hold one of:
    ///     an expression which creates an Exception to be thrown 
    ///     an expression which produces a value which should be returned 
    ///         directly to the user and represents an error has occured (for
    ///         example undefined in JavaScript)
    ///     an expression which produces a value which should be returned
    ///         directly to the user but does not actually represent an error.
    /// 
    /// ErrorInfo's are produced by an ActionBinder in response to a failed
    /// binding.  
    /// </summary>
    public sealed class ErrorInfo {
        /// <summary>
        /// Private constructor - consumers must use static From* factories
        /// to create ErrorInfo objects.
        /// </summary>
        private ErrorInfo(Expression value, ErrorInfoKind kind) {
            Debug.Assert(value != null);

            Expression = value;
            Kind = kind;
        }

        /// <summary>
        /// Creates a new ErrorInfo which represents an exception that should
        /// be thrown.
        /// </summary>
        public static ErrorInfo FromException(Expression exceptionValue) {
            ContractUtils.RequiresNotNull(exceptionValue, nameof(exceptionValue));
            ContractUtils.Requires(typeof(Exception).IsAssignableFrom(exceptionValue.Type), nameof(exceptionValue), Strings.MustBeExceptionInstance);

            return new ErrorInfo(exceptionValue, ErrorInfoKind.Exception);
        }

        /// <summary>
        /// Creates a new ErrorInfo which represents a value which should be
        /// returned to the user.
        /// </summary>
        public static ErrorInfo FromValue(Expression resultValue) {
            ContractUtils.RequiresNotNull(resultValue, nameof(resultValue));

            return new ErrorInfo(resultValue, ErrorInfoKind.Error);
        }

        /// <summary>
        /// Crates a new ErrorInfo which represents a value which should be returned
        /// to the user but does not represent an error.
        /// </summary>
        /// <param name="resultValue"></param>
        /// <returns></returns>
        public static ErrorInfo FromValueNoError(Expression resultValue) {
            ContractUtils.RequiresNotNull(resultValue, nameof(resultValue));

            return new ErrorInfo(resultValue, ErrorInfoKind.Success);
        }

        public ErrorInfoKind Kind { get; }

        public Expression Expression { get; }
    }

    public enum ErrorInfoKind {
        /// <summary>
        /// The ErrorInfo expression produces an exception
        /// </summary>
        Exception,
        /// <summary>
        /// The ErrorInfo expression produces a value which represents the error (e.g. undefined)
        /// </summary>
        Error,
        /// <summary>
        /// The ErrorInfo expression produces a value which is not an error
        /// </summary>
        Success
    }
}
