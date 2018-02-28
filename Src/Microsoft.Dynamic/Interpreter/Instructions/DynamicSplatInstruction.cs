// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter {
    /// <summary>
    /// Implements dynamic call site with many arguments. Wraps the arguments into <see cref="ArgumentArray"/>.
    /// </summary>
    internal sealed partial class DynamicSplatInstruction : Instruction {
        private readonly CallSite<Func<CallSite, ArgumentArray, object>> _site;
        private readonly int _argumentCount;

        internal DynamicSplatInstruction(int argumentCount, CallSite<Func<CallSite, ArgumentArray, object>> site) {
            _site = site;
            _argumentCount = argumentCount;
        }

        public override int ProducedStack => 1;
        public override int ConsumedStack => _argumentCount;

        public override int Run(InterpretedFrame frame) {
            int first = frame.StackIndex - _argumentCount;
            object ret = _site.Target(_site, new ArgumentArray(frame.Data, first, _argumentCount));
            frame.Data[first] = ret;
            frame.StackIndex = first + 1;

            return 1;
        }

        public override string ToString() {
            return "DynamicSplatInstruction(" + _site + ")";
        }
    }
}
