﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions {
    /// <summary>
    /// OperatorInfo provides a mapping from DLR ExpressionType to their associated .NET methods.
    /// </summary>
    internal class OperatorInfo {
        private static Dictionary<ExpressionType, OperatorInfo> _infos = MakeOperatorTable(); // table of ExpressionType, names, and alt names for looking up methods.

        private OperatorInfo(ExpressionType op, string name, string altName) {
            Operator = op;
            Name = name;
            AlternateName = altName;
        }

        /// <summary>
        /// Given an operator returns the OperatorInfo associated with the operator or null
        /// </summary>
        public static OperatorInfo GetOperatorInfo(ExpressionType op) {
            _infos.TryGetValue(op, out OperatorInfo res);
            return res;
        }

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
        /// This method name is usally in the form of op_Operator (e.g. op_Addition).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the secondary method name associated with the method.
        /// This method name is usually a standard .NET method name with pascal casing (e.g. Add).
        /// </summary>
        public string AlternateName { get; }

        private static Dictionary<ExpressionType, OperatorInfo> MakeOperatorTable() {
            var res = new Dictionary<ExpressionType, OperatorInfo>();

            // alternate names from: http://msdn2.microsoft.com/en-us/library/2sk3x8a7(vs.71).aspx
            //   different in:
            //    comparisons all support alternative names, Xor is "ExclusiveOr" not "Xor"

            // unary ExpressionType as defined in Partition I Architecture 9.3.1:
            res[ExpressionType.Decrement]          = new OperatorInfo(ExpressionType.Decrement,           "op_Decrement",                 "Decrement");      // --
            res[ExpressionType.Increment]          = new OperatorInfo(ExpressionType.Increment,           "op_Increment",                 "Increment");      // ++
            res[ExpressionType.Negate]             = new OperatorInfo(ExpressionType.Negate,              "op_UnaryNegation",             "Negate");         // - (unary)
            res[ExpressionType.UnaryPlus]          = new OperatorInfo(ExpressionType.UnaryPlus,           "op_UnaryPlus",                 "Plus");           // + (unary)
            res[ExpressionType.Not]                = new OperatorInfo(ExpressionType.Not,                 "op_LogicalNot",                null);             // !
            res[ExpressionType.IsTrue]             = new OperatorInfo(ExpressionType.IsTrue,              "op_True",                      null);             // not defined
            res[ExpressionType.IsFalse]            = new OperatorInfo(ExpressionType.IsFalse,             "op_False",                     null);             // not defined
            //res.Add(new OperatorInfo(ExpressionType.AddressOf,           "op_AddressOf",                 null);             // & (unary)
            res[ExpressionType.OnesComplement]     = new OperatorInfo(ExpressionType.OnesComplement,      "op_OnesComplement",            "OnesComplement"); // ~
            //res.Add(new OperatorInfo(ExpressionType.PointerDereference,  "op_PointerDereference",        null);             // * (unary)

            // binary ExpressionType as defined in Partition I Architecture 9.3.2:
            res[ExpressionType.Add]                = new OperatorInfo(ExpressionType.Add,                 "op_Addition",                  "Add");            // +
            res[ExpressionType.Subtract]           = new OperatorInfo(ExpressionType.Subtract,            "op_Subtraction",               "Subtract");       // -
            res[ExpressionType.Multiply]           = new OperatorInfo(ExpressionType.Multiply,            "op_Multiply",                  "Multiply");       // *
            res[ExpressionType.Divide]             = new OperatorInfo(ExpressionType.Divide,              "op_Division",                  "Divide");         // /
            res[ExpressionType.Modulo]             = new OperatorInfo(ExpressionType.Modulo,              "op_Modulus",                   "Mod");            // %
            res[ExpressionType.ExclusiveOr]        = new OperatorInfo(ExpressionType.ExclusiveOr,         "op_ExclusiveOr",               "ExclusiveOr");    // ^
            res[ExpressionType.And]                = new OperatorInfo(ExpressionType.And,                 "op_BitwiseAnd",                "BitwiseAnd");     // &
            res[ExpressionType.Or]                 = new OperatorInfo(ExpressionType.Or,                  "op_BitwiseOr",                 "BitwiseOr");      // |
            res[ExpressionType.And]                = new OperatorInfo(ExpressionType.And,                 "op_LogicalAnd",                "And");            // &&
            res[ExpressionType.Or]                 = new OperatorInfo(ExpressionType.Or,                  "op_LogicalOr",                 "Or");             // ||
            res[ExpressionType.LeftShift]          = new OperatorInfo(ExpressionType.LeftShift,           "op_LeftShift",                 "LeftShift");      // <<
            res[ExpressionType.RightShift]         = new OperatorInfo(ExpressionType.RightShift,          "op_RightShift",                "RightShift");     // >>
            res[ExpressionType.Equal]              = new OperatorInfo(ExpressionType.Equal,               "op_Equality",                  "Equals");         // ==   
            res[ExpressionType.GreaterThan]        = new OperatorInfo(ExpressionType.GreaterThan,         "op_GreaterThan",               "GreaterThan");    // >
            res[ExpressionType.LessThan]           = new OperatorInfo(ExpressionType.LessThan,            "op_LessThan",                  "LessThan");       // <
            res[ExpressionType.NotEqual]           = new OperatorInfo(ExpressionType.NotEqual,            "op_Inequality",                "NotEquals");      // != 
            res[ExpressionType.GreaterThanOrEqual] = new OperatorInfo(ExpressionType.GreaterThanOrEqual,  "op_GreaterThanOrEqual",        "GreaterThanOrEqual");        // >=
            res[ExpressionType.LessThanOrEqual]    = new OperatorInfo(ExpressionType.LessThanOrEqual,     "op_LessThanOrEqual",           "LessThanOrEqual");        // <=
            res[ExpressionType.MultiplyAssign]     = new OperatorInfo(ExpressionType.MultiplyAssign,      "op_MultiplicationAssignment",  "InPlaceMultiply");       // *=
            res[ExpressionType.SubtractAssign]     = new OperatorInfo(ExpressionType.SubtractAssign,      "op_SubtractionAssignment",     "InPlaceSubtract");       // -=
            res[ExpressionType.ExclusiveOrAssign]  = new OperatorInfo(ExpressionType.ExclusiveOrAssign,   "op_ExclusiveOrAssignment",     "InPlaceExclusiveOr");            // ^=
            res[ExpressionType.LeftShiftAssign]    = new OperatorInfo(ExpressionType.LeftShiftAssign,     "op_LeftShiftAssignment",       "InPlaceLeftShift");      // <<=
            res[ExpressionType.RightShiftAssign]   = new OperatorInfo(ExpressionType.RightShiftAssign,    "op_RightShiftAssignment",      "InPlaceRightShift");     // >>=
            res[ExpressionType.ModuloAssign]       = new OperatorInfo(ExpressionType.ModuloAssign,        "op_ModulusAssignment",         "InPlaceMod");            // %=
            res[ExpressionType.AddAssign]          = new OperatorInfo(ExpressionType.AddAssign,           "op_AdditionAssignment",        "InPlaceAdd");            // += 
            res[ExpressionType.AndAssign]          = new OperatorInfo(ExpressionType.AndAssign,           "op_BitwiseAndAssignment",      "InPlaceBitwiseAnd");     // &=
            res[ExpressionType.OrAssign]           = new OperatorInfo(ExpressionType.OrAssign,            "op_BitwiseOrAssignment",       "InPlaceBitwiseOr");      // |=
            res[ExpressionType.DivideAssign]       = new OperatorInfo(ExpressionType.DivideAssign,        "op_DivisionAssignment",        "InPlaceDivide");         // /=

            return res;
        }
    }
}
