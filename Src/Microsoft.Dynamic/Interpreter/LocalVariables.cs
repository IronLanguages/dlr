// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter {
    public sealed class LocalVariable {
        private const int IsBoxedFlag = 1;
        private const int InClosureFlag = 2;

        public readonly int Index;
        private int _flags;

        public bool IsBoxed {
            get => (_flags & IsBoxedFlag) != 0;
            set {
                if (value) {
                    _flags |= IsBoxedFlag;
                } else {
                    _flags &= ~IsBoxedFlag;
                }
            }
        }

        public bool InClosure => (_flags & InClosureFlag) != 0;

        public bool InClosureOrBoxed => InClosure | IsBoxed;

        internal LocalVariable(int index, bool closure, bool boxed) {
            Index = index;
            _flags = (closure ? InClosureFlag : 0) | (boxed ? IsBoxedFlag : 0);
        }

        internal Expression LoadFromArray(Expression frameData, Expression closure) {
            Expression result = Expression.ArrayAccess(InClosure ? closure : frameData, Expression.Constant(Index));
            return IsBoxed ? Expression.Convert(result, typeof(StrongBox<object>)) : result;
        }

        public override string ToString() {
            return $"{Index}: {(IsBoxed ? "boxed" : null)} {(InClosure ? "in closure" : null)}";
        }
    }

    public struct LocalDefinition : IEquatable<LocalDefinition> {

        internal LocalDefinition(int localIndex, ParameterExpression parameter) {
            Index = localIndex;
            Parameter = parameter;
        }

        public int Index { get; }

        public ParameterExpression Parameter { get; }

        public override bool Equals(object obj) {
            if (obj is LocalDefinition other) {
                return other.Index == Index && other.Parameter == Parameter;
            }

            return false;
        }

        public bool Equals(LocalDefinition other) =>
            Index == other.Index && Parameter == other.Parameter;

        public override int GetHashCode() {
            if (Parameter == null) {
                return 0;
            }
            return Parameter.GetHashCode() ^ Index.GetHashCode();
        }

        public static bool operator ==(LocalDefinition self, LocalDefinition other) => self.Equals(other);

        public static bool operator !=(LocalDefinition self, LocalDefinition other) => !self.Equals(other);
    }

    public sealed class LocalVariables {
        private readonly HybridReferenceDictionary<ParameterExpression, VariableScope> _variables = new HybridReferenceDictionary<ParameterExpression, VariableScope>();

        private int _localCount, _maxLocalCount;

        internal LocalVariables() {
        }

        public LocalDefinition DefineLocal(ParameterExpression variable, int start) {
            ContractUtils.RequiresNotNull(variable, nameof(variable));
            ContractUtils.Requires(start >= 0, nameof(start), "start must be positive");

            LocalVariable result = new LocalVariable(_localCount++, false, false);
            _maxLocalCount = Math.Max(_localCount, _maxLocalCount);

            VariableScope newScope;
            if (_variables.TryGetValue(variable, out VariableScope existing)) {
                newScope = new VariableScope(result, start, existing);
                if (existing.ChildScopes == null) {
                    existing.ChildScopes = new List<VariableScope>();
                }
                existing.ChildScopes.Add(newScope);
            } else {
                newScope = new VariableScope(result, start, null);
            }

            _variables[variable] = newScope;
            return new LocalDefinition(result.Index, variable);
        }

        public void UndefineLocal(LocalDefinition definition, int end) {
            var scope = _variables[definition.Parameter];
            scope.Stop = end;
            if (scope.Parent != null) {
                _variables[definition.Parameter] = scope.Parent;
            } else {
                _variables.Remove(definition.Parameter);
            }
            
            _localCount--;
        }

        internal void Box(ParameterExpression variable, InstructionList instructions) {
            var scope = _variables[variable];

            LocalVariable local = scope.Variable;
            Debug.Assert(!local.IsBoxed && !local.InClosure);
            _variables[variable].Variable.IsBoxed = true;
                
            int curChild = 0;
            for (int i = scope.Start; i < scope.Stop && i < instructions.Count; i++) {
                if (scope.ChildScopes != null && scope.ChildScopes[curChild].Start == i) {
                    // skip boxing in the child scope
                    var child = scope.ChildScopes[curChild];
                    i = child.Stop;

                    curChild++;
                    continue;
                }

                instructions.SwitchToBoxed(local.Index, i);
            }
        }

        public int LocalCount => _maxLocalCount;

        public int GetOrDefineLocal(ParameterExpression var) {
            int index = GetLocalIndex(var);
            if (index == -1) {
                return DefineLocal(var, 0).Index;
            }
            return index;
        }

        public int GetLocalIndex(ParameterExpression var) {
            return _variables.TryGetValue(var, out VariableScope loc) ? loc.Variable.Index : -1;
        }

        public bool TryGetLocalOrClosure(ParameterExpression var, out LocalVariable local) {
            if (_variables.TryGetValue(var, out VariableScope scope)) {
                local = scope.Variable;
                return true;
            }
            if (ClosureVariables != null && ClosureVariables.TryGetValue(var, out local)) {
                return true;
            }

            local = null;
            return false;
        }

        /// <summary>
        /// Gets a copy of the local variables which are defined in the current scope.
        /// </summary>
        /// <returns></returns>
        internal Dictionary<ParameterExpression, LocalVariable> CopyLocals() {
            var res = new Dictionary<ParameterExpression, LocalVariable>(_variables.Count);
            foreach (var keyValue in _variables) {
                res[keyValue.Key] = keyValue.Value.Variable;
            }
            return res;
        }

        /// <summary>
        /// Checks to see if the given variable is defined within the current local scope.
        /// </summary>
        internal bool ContainsVariable(ParameterExpression variable) {
            return _variables.ContainsKey(variable);
        }

        /// <summary>
        /// Gets the variables which are defined in an outer scope and available within the current scope.
        /// </summary>
        internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; private set; }

        internal LocalVariable AddClosureVariable(ParameterExpression variable) {
            if (ClosureVariables == null) {
                ClosureVariables = new Dictionary<ParameterExpression, LocalVariable>();
            }
            LocalVariable result = new LocalVariable(ClosureVariables.Count, true, false);
            ClosureVariables.Add(variable, result);
            return result;
        }

        /// <summary>
        /// Tracks where a variable is defined and what range of instructions it's used in
        /// </summary>
        private sealed class VariableScope {
            public readonly int Start;
            public int Stop = Int32.MaxValue;
            public readonly LocalVariable Variable;
            public readonly VariableScope Parent;
            public List<VariableScope> ChildScopes;

            public VariableScope(LocalVariable variable, int start, VariableScope parent) {
                Variable = variable;
                Start = start;
                Parent = parent;
            }
        }
    }
}
