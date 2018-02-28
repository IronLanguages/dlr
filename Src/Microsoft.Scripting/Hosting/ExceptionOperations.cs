// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
