// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime {
    /// <summary>
    /// These are some generally useful helper methods. Currently the only methods are those to
    /// cached boxed representations of commonly used primitive types so that they can be shared.
    /// This is useful to most dynamic languages that use object as a universal type.
    /// 
    /// The methods in RuntimeHelepers are caleld by the generated code. From here the methods may
    /// dispatch to other parts of the runtime to get bulk of the work done, but the entry points
    /// should be here.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public static partial class ScriptingRuntimeHelpers {
        private const int MIN_CACHE = -100;
        private const int MAX_CACHE = 1000;
        private static readonly object[] cache = MakeCache();

        /// <summary>
        /// A singleton boxed boolean true.
        /// </summary>
        public static readonly object True = true;

        /// <summary>
        ///A singleton boxed boolean false.
        /// </summary>
        public static readonly object False = false;

        internal static readonly MethodInfo BooleanToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("BooleanToObject");
        internal static readonly MethodInfo Int32ToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("Int32ToObject");

        private static object[] MakeCache() {
            object[] result = new object[MAX_CACHE - MIN_CACHE];

            for (int i = 0; i < result.Length; i++) {
                result[i] = (object)(i + MIN_CACHE);
            }

            return result;
        }

#if DEBUG
        public static void NoteException(Exception e) {
            PerfTrack.NoteEvent(PerfTrack.Categories.Exceptions, "LightEH Missed: " + e.GetType());
        }
#endif

        /// <summary>
        /// Gets a singleton boxed value for the given integer if possible, otherwise boxes the integer.
        /// </summary>
        /// <param name="value">The value to box.</param>
        /// <returns>The boxed value.</returns>
        public static object Int32ToObject(Int32 value) {
            // caches improves pystone by ~5-10% on MS .Net 1.1, this is a very integer intense app
            // TODO: investigate if this still helps perf. There's evidence that it's harmful on
            // .NET 3.5 and 4.0
            if (value < MAX_CACHE && value >= MIN_CACHE) {
                return cache[value - MIN_CACHE];
            }
            return (object)value;
        }

        private static readonly string[] chars = MakeSingleCharStrings();

        private static string[] MakeSingleCharStrings() {
            string[] result = new string[255];

            for (char ch = (char)0; ch < result.Length; ch++) {
                result[ch] = new string(ch, 1);
            }

            return result;
        }

        public static object BooleanToObject(bool value) {
            return value ? True : False;
        }

        public static string CharToString(char ch) {
            if (ch < 255) return chars[ch];
            return new string(ch, 1);
        }

        internal static object GetPrimitiveDefaultValue(Type type) {
            switch (type.GetTypeCode()) {
                case TypeCode.Boolean: return ScriptingRuntimeHelpers.False;
                case TypeCode.SByte: return default(SByte);
                case TypeCode.Byte: return default(Byte);
                case TypeCode.Char: return default(Char);
                case TypeCode.Int16: return default(Int16);
                case TypeCode.Int32: return ScriptingRuntimeHelpers.Int32ToObject(0);
                case TypeCode.Int64: return default(Int64);
                case TypeCode.UInt16: return default(UInt16);
                case TypeCode.UInt32: return default(UInt32);
                case TypeCode.UInt64: return default(UInt64);
                case TypeCode.Single: return default(Single);
                case TypeCode.Double: return default(Double);
#if FEATURE_DBNULL
                case TypeCode.DBNull: return default(DBNull);
#endif
                case TypeCode.DateTime: return default(DateTime);
                case TypeCode.Decimal: return default(Decimal);
                default: return null;
            }
        }

        public static ArgumentTypeException SimpleTypeError(string message) {
            return new ArgumentTypeException(message);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")] // TODO: fix
        public static Exception CannotConvertError(Type toType, object value) {
            return SimpleTypeError($"Cannot convert {CompilerHelpers.GetType(value).Name}({value}) to {toType.Name}");
        }

        public static Exception SimpleAttributeError(string message) {
            //TODO: localize
            return new MissingMemberException(message);
        }

        public static object ReadOnlyAssignError(bool field, string fieldName) {
            if (field) {
                throw Error.FieldReadonly(fieldName);
            }

            throw Error.PropertyReadonly(fieldName);
        }

        /// <summary>
        /// Helper method to create an instance.  Work around for Silverlight where Activator.CreateInstance
        /// is SecuritySafeCritical.
        /// 
        /// TODO: Why can't we just emit the right thing for default(T)?
        /// It's always null for reference types and it's well defined for value types
        /// </summary>
        public static T CreateInstance<T>() {
            return default(T);
        }

        // TODO: can't we just emit a new array?
        public static T[] CreateArray<T>(int args) {
            return new T[args];
        }
        
        /// <summary>
        /// EventInfo.EventHandlerType getter is marked SecuritySafeCritical in CoreCLR
        /// This method is to get to the property without using Reflection
        /// </summary>
        /// <param name="eventInfo"></param>
        /// <returns></returns>
        public static Type GetEventHandlerType(EventInfo eventInfo) {
            ContractUtils.RequiresNotNull(eventInfo, nameof(eventInfo));
            return eventInfo.EventHandlerType;
        }

        public static IList<string> GetStringMembers(IList<object> members) {
            List<string> res = new List<string>();
            foreach (object o in members) {
                if (o is string str) {
                    res.Add(str);
                }
            }
            return res;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")] // TODO: fix
        public static void SetEvent(EventTracker eventTracker, object value) {
            if (value is EventTracker et) {
                if (et != eventTracker) {
                    throw Error.UnexpectedEvent(eventTracker.DeclaringType.Name,
                                                eventTracker.Name,
                                                et.DeclaringType.Name,
                                                et.Name);
                }
                return;
            }

            BoundMemberTracker bmt = value as BoundMemberTracker;
            if (bmt == null) {
                throw Error.ExpectedBoundEvent(CompilerHelpers.GetType(value).Name);
            }
            if (bmt.BoundTo.MemberType != TrackerTypes.Event) throw Error.ExpectedBoundEvent(bmt.BoundTo.MemberType.ToString());

            if (bmt.BoundTo != eventTracker) throw Error.UnexpectedEvent(
                eventTracker.DeclaringType.Name,
                eventTracker.Name,
                bmt.BoundTo.DeclaringType.Name,
                bmt.BoundTo.Name);
        }

        // TODO: just emit this in the generated code
        public static bool CheckDictionaryMembers(IDictionary dict, string[] names) {
            if (dict.Count != names.Length) return false;

            foreach (string name in names) {
                if (!dict.Contains(name)) {
                    return false;
                }
            }
            return true;
        }

        // TODO: just emit this in the generated code
        [Obsolete("use MakeIncorrectBoxTypeError instead")]
        public static T IncorrectBoxType<T>(object received) {
            throw Error.UnexpectedType("StrongBox<" + typeof(T).Name + ">", CompilerHelpers.GetType(received).Name);
        }

        public static Exception MakeIncorrectBoxTypeError(Type type, object received) {
            return Error.UnexpectedType("StrongBox<" + type.Name + ">", CompilerHelpers.GetType(received).Name);
        }
        
        /// <summary>
        /// Provides the test to see if an interpreted call site should switch over to being compiled.
        /// </summary>
        public static bool InterpretedCallSiteTest(bool restrictionResult, object bindingInfo) {
            if (restrictionResult) {
                CachedBindingInfo bindInfo = (CachedBindingInfo)bindingInfo;
                if (bindInfo.CompilationThreshold >= 0) {
                    // still interpreting...
                    bindInfo.CompilationThreshold--;
                    return true;
                }

                return bindInfo.CheckCompiled();
            }
            return false;
        }
    }
}
