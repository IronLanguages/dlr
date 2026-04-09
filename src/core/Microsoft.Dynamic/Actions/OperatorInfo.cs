// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#if NET8_0_OR_GREATER
using System.Collections.Frozen;
using OperatorDictionary = System.Collections.Frozen.FrozenDictionary<System.Linq.Expressions.ExpressionType, Microsoft.Scripting.Actions.OperatorInfo>;
#else
using OperatorDictionary = System.Collections.Generic.Dictionary<System.Linq.Expressions.ExpressionType, Microsoft.Scripting.Actions.OperatorInfo>;
#endif


namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// OperatorInfo provides a mapping from DLR ExpressionType to their associated .NET methods.
    /// </summary>
    internal sealed class OperatorInfo {
        private static readonly OperatorDictionary _infos = MakeOperatorTable(); // table of ExpressionType, names, and alt names for looking up methods.

        private OperatorInfo(ExpressionType op, string name, string altName) {
            Operator = op;
            Name = name;
            AlternateName = altName;
        }

        /// <summary>
        /// Given an operator returns the OperatorInfo associated with the operator or null.
        /// </summary>
        public static OperatorInfo GetOperatorInfo(ExpressionType op) {
            _infos.TryGetValue(op, out OperatorInfo data);
            return data;
        }

        [Obsolete("This method is not efficient; use GetOperatorInfo(ExpressionType) instead.")]
        public static OperatorInfo GetOperatorInfo(string name) {
            foreach (OperatorInfo info in _infos.Values) {
                if (info.Name == name || info.AlternateName == name) {
                    return info;
                }
            }
            return null;
        }


        /// <summary>
        /// Gets the operator the OperatorInfo provides info for.
        /// </summary>
        public ExpressionType Operator { get; }

        /// <summary>
        /// Gets the primary method name associated with the method.
        /// This method name is usually in the form of op_Operator (e.g. op_Addition).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the secondary method name associated with the method.
        /// This method name is usually a standard .NET method name with pascal casing (e.g. Add).
        /// </summary>
        public string AlternateName { get; }

        private static OperatorDictionary MakeOperatorTable() {

#if NET10_0_OR_GREATER
            ReadOnlySpan<KeyValuePair<ExpressionType, OperatorInfo>> data = [
#elif NET8_0_OR_GREATER
            var data = new KeyValuePair<ExpressionType, OperatorInfo>[] {
#else
            var data = new KvpDictionary<ExpressionType, OperatorInfo>() {
#endif

                // alternate names from: http://msdn2.microsoft.com/en-us/library/2sk3x8a7(vs.71).aspx
                //   different in:
                //    comparisons all support alternative names, Xor is "ExclusiveOr" not "Xor"

                // unary ExpressionType as defined in Partition I Architecture 9.3.1:
                new(ExpressionType.Decrement,          new(ExpressionType.Decrement,           "op_Decrement",                 "Decrement")),         // --
                new(ExpressionType.Increment,          new(ExpressionType.Increment,           "op_Increment",                 "Increment")),         // ++
                new(ExpressionType.Negate,             new(ExpressionType.Negate,              "op_UnaryNegation",             "Negate")),            // - (unary)
                new(ExpressionType.UnaryPlus,          new(ExpressionType.UnaryPlus,           "op_UnaryPlus",                 "Plus")),              // + (unary)
                new(ExpressionType.Not,                new(ExpressionType.Not,                 "op_LogicalNot",                null)),                // !
                new(ExpressionType.IsTrue,             new(ExpressionType.IsTrue,              "op_True",                      null)),                // not defined
                new(ExpressionType.IsFalse,            new(ExpressionType.IsFalse,             "op_False",                     null)),                // not defined
                //new(ExpressionType.AddressOf,          new(ExpressionType.AddressOf,           "op_AddressOf",                 null));              // & (unary)
                new(ExpressionType.OnesComplement,     new(ExpressionType.OnesComplement,      "op_OnesComplement",            "OnesComplement")),    // ~
                //new(ExpressionType.PointerDereference, new(ExpressionType.PointerDereference,  "op_PointerDereference",        null));              // * (unary)

                // binary ExpressionType as defined in Partition I Architecture 9.3.2:
                new(ExpressionType.Add,                new(ExpressionType.Add,                 "op_Addition",                  "Add")),                // +
                new(ExpressionType.Subtract,           new(ExpressionType.Subtract,            "op_Subtraction",               "Subtract")),           // -
                new(ExpressionType.Multiply,           new(ExpressionType.Multiply,            "op_Multiply",                  "Multiply")),           // *
                new(ExpressionType.Divide,             new(ExpressionType.Divide,              "op_Division",                  "Divide")),             // /
                new(ExpressionType.Modulo,             new(ExpressionType.Modulo,              "op_Modulus",                   "Mod")),                // %
                new(ExpressionType.ExclusiveOr,        new(ExpressionType.ExclusiveOr,         "op_ExclusiveOr",               "ExclusiveOr")),        // ^
                new(ExpressionType.And,                new(ExpressionType.And,                 "op_BitwiseAnd",                "BitwiseAnd")),         // &
                new(ExpressionType.Or,                 new(ExpressionType.Or,                  "op_BitwiseOr",                 "BitwiseOr")),          // |
                new(ExpressionType.And,                new(ExpressionType.And,                 "op_LogicalAnd",                "And")),                // &&
                new(ExpressionType.Or,                 new(ExpressionType.Or,                  "op_LogicalOr",                 "Or")),                 // ||
                new(ExpressionType.LeftShift,          new(ExpressionType.LeftShift,           "op_LeftShift",                 "LeftShift")),          // <<
                new(ExpressionType.RightShift,         new(ExpressionType.RightShift,          "op_RightShift",                "RightShift")),         // >>
                new(ExpressionType.Equal,              new(ExpressionType.Equal,               "op_Equality",                  "Equals")),             // ==
                new(ExpressionType.GreaterThan,        new(ExpressionType.GreaterThan,         "op_GreaterThan",               "GreaterThan")),        // >
                new(ExpressionType.LessThan,           new(ExpressionType.LessThan,            "op_LessThan",                  "LessThan")),           // <
                new(ExpressionType.NotEqual,           new(ExpressionType.NotEqual,            "op_Inequality",                "NotEquals")),          // !=
                new(ExpressionType.GreaterThanOrEqual, new(ExpressionType.GreaterThanOrEqual,  "op_GreaterThanOrEqual",        "GreaterThanOrEqual")), // >=
                new(ExpressionType.LessThanOrEqual,    new(ExpressionType.LessThanOrEqual,     "op_LessThanOrEqual",           "LessThanOrEqual")),    // <=
                new(ExpressionType.MultiplyAssign,     new(ExpressionType.MultiplyAssign,      "op_MultiplicationAssignment",  "InPlaceMultiply")),    // *=
                new(ExpressionType.SubtractAssign,     new(ExpressionType.SubtractAssign,      "op_SubtractionAssignment",     "InPlaceSubtract")),    // -=
                new(ExpressionType.ExclusiveOrAssign,  new(ExpressionType.ExclusiveOrAssign,   "op_ExclusiveOrAssignment",     "InPlaceExclusiveOr")), // ^=
                new(ExpressionType.LeftShiftAssign,    new(ExpressionType.LeftShiftAssign,     "op_LeftShiftAssignment",       "InPlaceLeftShift")),   // <<=
                new(ExpressionType.RightShiftAssign,   new(ExpressionType.RightShiftAssign,    "op_RightShiftAssignment",      "InPlaceRightShift")),  // >>=
                new(ExpressionType.ModuloAssign,       new(ExpressionType.ModuloAssign,        "op_ModulusAssignment",         "InPlaceMod")),         // %=
                new(ExpressionType.AddAssign,          new(ExpressionType.AddAssign,           "op_AdditionAssignment",        "InPlaceAdd")),         // +=
                new(ExpressionType.AndAssign,          new(ExpressionType.AndAssign,           "op_BitwiseAndAssignment",      "InPlaceBitwiseAnd")),  // &=
                new(ExpressionType.OrAssign,           new(ExpressionType.OrAssign,            "op_BitwiseOrAssignment",       "InPlaceBitwiseOr")),   // |=
                new(ExpressionType.DivideAssign,       new(ExpressionType.DivideAssign,        "op_DivisionAssignment",        "InPlaceDivide")),      // /=

#if NET10_0_OR_GREATER
            ];
            return FrozenDictionary.Create(data);
#elif NET8_0_OR_GREATER
            };
            return data.ToFrozenDictionary();
#else
            };
            return data;
#endif
        }

        private sealed class KvpDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public void Add(KeyValuePair<TKey, TValue> kvp) => Add(kvp.Key, kvp.Value);
        }
    }
}
