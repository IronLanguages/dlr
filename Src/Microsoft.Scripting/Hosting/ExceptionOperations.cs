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

using System;
using System.Collections.Generic;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

namespace Microsoft.Scripting.Hosting {
    public sealed class ExceptionOperations : MarshalByRefObject {
        private readonly LanguageContext _context;

        internal ExceptionOperations(LanguageContext context) {
            _context = context;
        }

        public string FormatException(Exception exception) {
            return _context.FormatException(exception);
        }

        public void GetExceptionMessage(Exception exception, out string message, out string errorTypeName) {
            _context.GetExceptionMessage(exception, out message, out errorTypeName);
        }

        public bool HandleException(Exception exception) {
            ContractUtils.RequiresNotNull(exception, nameof(exception));
            return false;
        }

        public IList<DynamicStackFrame> GetStackFrames(Exception exception) {
            ContractUtils.RequiresNotNull(exception, nameof(exception));
            return _context.GetStackFrames(exception);
        }

#if FEATURE_REMOTING
        public string FormatException(ObjectHandle exception) {
            ContractUtils.RequiresNotNull(exception, nameof(exception));
            var exceptionObj = exception.Unwrap() as Exception;
            ContractUtils.Requires(exceptionObj != null, nameof(exception), "ObjectHandle must be to Exception object");

            return _context.FormatException(exceptionObj);
        }

        public void GetExceptionMessage(ObjectHandle exception, out string message, out string errorTypeName) {
            ContractUtils.RequiresNotNull(exception, nameof(exception));
            var exceptionObj = exception.Unwrap() as Exception;
            ContractUtils.Requires(exceptionObj != null, nameof(exception), "ObjectHandle must be to Exception object");

            _context.GetExceptionMessage(exceptionObj, out message, out errorTypeName);
        }

        public bool HandleException(ObjectHandle exception) {
            ContractUtils.RequiresNotNull(exception, nameof(exception));
            ContractUtils.Requires(exception.Unwrap() is Exception, nameof(exception), "ObjectHandle must be to Exception object");

            return false;
        }

        public IList<DynamicStackFrame> GetStackFrames(ObjectHandle exception) {
            ContractUtils.RequiresNotNull(exception, nameof(exception));
            var exceptionObj = exception.Unwrap() as Exception;
            ContractUtils.Requires(exceptionObj != null, nameof(exception), "ObjectHandle must be to Exception object");

            return _context.GetStackFrames(exceptionObj);
        }

        // TODO: Figure out what is the right lifetime
        public override object InitializeLifetimeService() {
            return null;
        }
#endif
    }
}
