// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {

    /// <summary>
    /// Creates a dictionary of locals in this scope
    /// </summary>
    public sealed class LocalsDictionary : CustomStringDictionary {
        private readonly IRuntimeVariables _locals;
        private readonly string[] _symbols;
        private Dictionary<string, int> _boxes;

        public LocalsDictionary(IRuntimeVariables locals, string[] symbols) {
            Assert.NotNull(locals, symbols);
            _locals = locals;
            _symbols = symbols;
        }

        private void EnsureBoxes() {
            if (_boxes == null) {
                int count = _symbols.Length;
                Dictionary<string, int> boxes = new Dictionary<string, int>(count);
                for (int i = 0; i < count; i++) {
                    boxes[_symbols[i]] = i;
                }
                _boxes = boxes;
            }
        }

        public override string[] GetExtraKeys() {
            return _symbols;
        }

        protected internal override bool TrySetExtraValue(string key, object value) {
            EnsureBoxes();

            if (_boxes.TryGetValue(key, out int index)) {
                _locals[index] = value;
                return true;
            }

            return false;
        }

        protected internal override bool TryGetExtraValue(string key, out object value) {
            EnsureBoxes();

            if (_boxes.TryGetValue(key, out int index)) {
                value = _locals[index];
                return true;
            }
            value = null;
            return false;
        }
    }
}
