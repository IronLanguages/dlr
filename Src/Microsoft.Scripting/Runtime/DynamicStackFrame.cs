// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// Helper for storing information about stack frames.
    /// </summary>
    [Serializable]
    public class DynamicStackFrame {
        private readonly string _funcName;
        private readonly string _filename;
        private readonly int _lineNo;
        private readonly MethodBase _method;

        public DynamicStackFrame(MethodBase method, string funcName, string filename, int line) {
            _funcName = funcName;
            _filename = filename;
            _lineNo = line;
            _method = method;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public MethodBase GetMethod() {
            return _method;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public string GetMethodName() {
            return _funcName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public string GetFileName() {
            return _filename;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public int GetFileLineNumber() {
            return _lineNo;
        }

        public override string ToString() {
            return
                $"{_funcName ?? "<function unknown>"} in {_filename ?? "<filename unknown>"}:{_lineNo}, {(_method != null ? _method.ToString() : "<method unknown>")}";
        }
    }
}
