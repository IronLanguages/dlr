// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq.Expressions;

using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// TODO: Alternatively, it should be sufficient to remember indices for this, list, dict and block.
    /// </summary>
    public struct Argument : IEquatable<Argument> {
        private readonly ArgumentType _kind;
        private readonly string _name;

        public static readonly Argument Simple = new Argument(ArgumentType.Simple, null);

        public ArgumentType Kind => _kind;
        public string Name => _name;

        public Argument(string name) {
            _kind = ArgumentType.Named;
            _name = name;
        }

        public Argument(ArgumentType kind) {
            _kind = kind;
            _name = null;
        }

        public Argument(ArgumentType kind, string name) {
            ContractUtils.Requires((kind == ArgumentType.Named) ^ (name == null), nameof(kind));
            _kind = kind;
            _name = name;
        }

        public override bool Equals(object obj) =>
            obj is Argument argument && Equals(argument);

        public bool Equals(Argument other) =>
            _kind == other._kind && _name == other._name;

        public static bool operator ==(Argument left, Argument right) => left.Equals(right);

        public static bool operator !=(Argument left, Argument right) => !left.Equals(right);

        public override int GetHashCode() {
            return (_name != null) ? _name.GetHashCode() ^ (int)_kind : (int)_kind;
        }

        public bool IsSimple => Equals(Simple);

        public override string ToString() {
            return _name == null ? _kind.ToString() : _kind.ToString() + ":" + _name;
        }

        internal Expression CreateExpression() {
            return Expression.New(
                typeof(Argument).GetConstructor(new Type[] { typeof(ArgumentType), typeof(string) }),
                AstUtils.Constant(_kind),
                AstUtils.Constant(_name, typeof(string))
            );
        }
    }
}
