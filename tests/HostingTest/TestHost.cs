// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

using Microsoft.Scripting.Hosting;

namespace HostingTest {
    public class TestHost : ScriptHost {
        public TestHost() {
        }

        protected override void RuntimeAttached() {
            Runtime.LoadAssembly(Assembly.GetExecutingAssembly());
        }
    }

    public class Negative : Attribute {
    }
}
