/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using Microsoft.Scripting.AspNet.Util;

namespace Microsoft.Scripting.AspNet.UI.Controls {
    public class BaseCodeControl: Control {
        public string Code { get; set; }

        public int Line { get; set; }

        protected override void OnInit(EventArgs e) {
            base.OnInit(e);

            // This ensures that the template control is properly initialized
            ScriptTemplateControl.GetScriptTemplateControl(this);
        }

        internal new Control FindControl(string id) {
            Control control = base.FindControl(id);
            if (control == null) {
                Misc.ThrowException("Can't find control '" + id + "'", null,
                    TemplateControl.AppRelativeVirtualPath, Line);
                return null;
            }

            return control;
        }
    }
}
