// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

#if FEATURE_COM

using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop {

    public class ComMethodDesc {

        private readonly int _memid;  // this is the member id extracted from FUNCDESC.memid
        private readonly string _name;
        internal readonly INVOKEKIND InvokeKind;
        private readonly int _paramCnt;

        internal ComMethodDesc(string name, int dispId) {
            // no ITypeInfo constructor
            _memid = dispId;
            _name = name;
        }

        internal ComMethodDesc(string name, int dispId, INVOKEKIND invkind)
            : this(name, dispId) {
            InvokeKind = invkind;
        }

        internal ComMethodDesc(ITypeInfo typeInfo, FUNCDESC funcDesc) {
            _memid = funcDesc.memid;
            InvokeKind = funcDesc.invkind;

            string[] rgNames = new string[1 + funcDesc.cParams];
            typeInfo.GetNames(_memid, rgNames, rgNames.Length, out int cNames);
            if (IsPropertyPut && rgNames[rgNames.Length - 1] is null) {
                rgNames[rgNames.Length - 1] = "value";
                cNames++;
            }
            Debug.Assert(cNames == rgNames.Length);
            _name = rgNames[0];

            _paramCnt = funcDesc.cParams;
        }

        public string Name {
            get {
                Utils.Assert.NotNull(_name);
                return _name;
            }
        }

        public int DispId {
            get { return _memid; }
        }

        public bool IsPropertyGet {
            get {
                return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYGET) != 0;
            }
        }

        public bool IsDataMember {
            get {
                //must be regular get
                if (!IsPropertyGet || DispId == ComDispIds.DISPID_NEWENUM) {
                    return false;
                }

                //must have no parameters
                return _paramCnt == 0;
            }
        }

        public bool IsPropertyPut {
            get {
                return (InvokeKind & (INVOKEKIND.INVOKE_PROPERTYPUT | INVOKEKIND.INVOKE_PROPERTYPUTREF)) != 0;
            }
        }

        public bool IsPropertyPutRef {
            get {
                return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0;
            }
        }
        
        internal int ParamCount {
            get {
                return _paramCnt;
            }
        }
    }
}

#endif
