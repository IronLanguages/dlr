// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

//TODO - this file should NOT be under ClrAssembly as it has dependencies on the DLR!

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.Scripting.Runtime;

namespace Merlin.Testing.DynamicObjectModel
{
    public class TestBeforeMembers
    {
        public string foo { get { return "original string"; } }

        private string _spam;
        public string spam
        {
            get { return _spam; }
        }

        public string normal;

        [SpecialName]
        public object GetCustomMember(string name)
        {
            if (name == "foo")
                return "custom string";
            else
                return OperationFailed.Value;
        }

        [SpecialName]
        public bool SetMember(string name, object value)
        {
            if (name == "spam")
            {
                _spam = (string)value;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class TestBeforeMemberHijack
    {
        Dictionary<string, object> _dict = new Dictionary<string, object>();

        //This will never be reached since GetCustomMember is hijacking the getter.
        public string foo { get { return "this should not be returned"; } }

        public const double PI = 3.14;

        [SpecialName]
        public object GetCustomMember(string name)
        {
            if (name == "_dict")
                return _dict;
            else // if the attribute is missing, throw a KeyError from the dictionary
                return _dict[name];
        }

        [SpecialName]
        public void SetMember(string name, object value)
        {
            _dict[name] = value;
        }

        [SpecialName]
        public void DeleteMember(string name)
        {
            _dict.Remove(name);
        }

        [SpecialName]
        public IEnumerable<string> GetMemberNames()
        {
            return _dict.Keys;
        }
    }

    public class TestAfterMembers
    {
        private string _foo = "original string";
        public string foo
        {
            get { return _foo; }
            set { _foo = value; }
        }

        private string _spam;
        public string spam
        {
            get { return _spam; }
        }

        [SpecialName]
        public object GetBoundMember(string name)
        {
            if (name == "foo")
                return "custom string";
            else if (name == "bar")
                return "custom string";
            else
                return OperationFailed.Value;
        }

        [SpecialName]
        public bool SetMemberAfter(string name, object value)
        {
            if (name == "spamsetter")
            {
                _spam = (string)value;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class TestAfterMemberHijack
    {
        Dictionary<string, object> _dict = new Dictionary<string, object>();

        private string _foo = "original string";
        public string foo
        {
            get { return _foo; }
            set { _foo = value; }
        }

        [SpecialName]
        public object GetBoundMember(string name)
        {
            if (name == "_dict")
                return _dict;
            else // if the attribute is missing, throw a KeyError from the dictionary
                return _dict[name];
        }

        [SpecialName]
        public void SetMemberAfter(string name, object value)
        {
            _dict[name] = value;
        }

        [SpecialName]
        public void DeleteMember(string name)
        {
            _dict.Remove(name);
        }

        [SpecialName]
        public IEnumerable<string> GetMemberNames()
        {
            return _dict.Keys;
        }
    }
}
