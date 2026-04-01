// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class AmbiguousFileNameException : Exception {
        public string FirstPath { get; }

        public string SecondPath { get; }

        public AmbiguousFileNameException(string firstPath, string secondPath)
            : this(firstPath, secondPath, null, null) {
        }

        public AmbiguousFileNameException(string firstPath, string secondPath, string message)
            : this(firstPath, secondPath, message, null) {
        }

        public AmbiguousFileNameException(string firstPath, string secondPath, string message, Exception inner)
            : base(message ??
                   $"File name is ambiguous; more files are matching the same name (including '{firstPath}' and '{secondPath}')", inner) {
            ContractUtils.RequiresNotNull(firstPath, nameof(firstPath));
            ContractUtils.RequiresNotNull(secondPath, nameof(secondPath));

            FirstPath = firstPath;
            SecondPath = secondPath;
        }


#if FEATURE_SERIALIZATION
        protected AmbiguousFileNameException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("firstPath", FirstPath);
            info.AddValue("secondPath", SecondPath);

            base.GetObjectData(info, context);
        }
#endif
    }
}
