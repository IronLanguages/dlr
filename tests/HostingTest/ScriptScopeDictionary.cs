// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using System.Runtime.Serialization;

namespace HostingTest {

    /// <summary>
    /// Example containner to be used for misc ScriptScope testing.
    /// </summary>
    [Serializable()]
    public class ScriptScopeDictionary : CustomStringDictionary, ISerializable  {

        public ScriptScopeDictionary(SerializationInfo info, StreamingContext context):base() {
            var e = info.GetEnumerator();
            while(e.MoveNext()){
                base[e.Name as object] = e.Value;
            }
        }

        public ScriptScopeDictionary()
            : base() {
        }

        public override string[] GetExtraKeys() {
            return new string[] { };
        }


        protected override bool TrySetExtraValue(string key, object value) {
            return false;
        }

        protected override bool TryGetExtraValue(string key, out object value) {
            value = null;
            return false;
        }

        #region ISerializable Members

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            foreach (var v in base.Keys) {
                info.AddValue(v as string, base[v]);
            }
        }

        #endregion
    }

}
