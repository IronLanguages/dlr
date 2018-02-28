// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    internal sealed partial class DynamicInstructionN : Instruction {
        private readonly CallInstruction _targetInvocationInstruction;
        private readonly object _targetDelegate;
        private readonly CallSite _site;
        private readonly int _argumentCount;
        private readonly bool _isVoid;

        public DynamicInstructionN(Type delegateType, CallSite site) {
            var methodInfo = delegateType.GetMethod("Invoke");
            var parameters = methodInfo.GetParameters();

            // <Delegate>.Invoke is ok to target by a delegate in partial trust (SecurityException is not thrown):
            _targetInvocationInstruction = CallInstruction.Create(methodInfo, parameters);
            _site = site;
            _argumentCount = parameters.Length - 1;
            _targetDelegate = site.GetType().GetInheritedFields("Target").First().GetValue(site);
        }

        public DynamicInstructionN(Type delegateType, CallSite site, bool isVoid)
            : this(delegateType, site) {
            _isVoid = isVoid;
        }

        public override int ProducedStack => _isVoid ? 0 : 1;
        public override int ConsumedStack => _argumentCount;

        public override int Run(InterpretedFrame frame) {
            int first = frame.StackIndex - _argumentCount;
            object[] args = new object[1 + _argumentCount];
            args[0] = _site;
            for (int i = 0; i < _argumentCount; i++) {
                args[1 + i] = frame.Data[first + i];
            }

            object ret = _targetInvocationInstruction.InvokeInstance(_targetDelegate, args);
            if (_isVoid) {
                frame.StackIndex = first;
            } else {
                frame.Data[first] = ret;
                frame.StackIndex = first + 1;
            }

            return 1;
        }

        public override string ToString() {
            return "DynamicInstructionN(" + _site + ")";
        }
    }
}
