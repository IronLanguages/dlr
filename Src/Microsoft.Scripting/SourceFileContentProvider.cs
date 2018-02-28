// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_REMOTING
using System.Runtime.Remoting;
#else
using MarshalByRefObject = System.Object;
#endif

using System;
using System.IO;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {

    /// <summary>
    /// Provides a StreamContentProvider for a stream of content backed by a file on disk.
    /// </summary>
    [Serializable]
    internal sealed class FileStreamContentProvider : StreamContentProvider {
        private readonly string _path;
        private readonly PALHolder _pal;

        internal string Path {
            get { return _path; }
        }

        #region Construction

        internal FileStreamContentProvider(PlatformAdaptationLayer pal, string path) {
            Assert.NotNull(pal, path);

            _path = path;
            _pal = new PALHolder(pal);
        }

        #endregion

        public override Stream GetStream() {
            return _pal.GetStream(Path);
        }

        [Serializable]
        private class PALHolder : MarshalByRefObject {
            [NonSerialized]
            private readonly PlatformAdaptationLayer _pal;

            internal PALHolder(PlatformAdaptationLayer pal) {
                _pal = pal;
            }

            internal Stream GetStream(string path) {
                return _pal.OpenInputFileStream(path);
            }

#if FEATURE_REMOTING
            // TODO: Figure out what is the right lifetime
            public override object InitializeLifetimeService() {
                return null;
            }
#endif
        }
    }
}
