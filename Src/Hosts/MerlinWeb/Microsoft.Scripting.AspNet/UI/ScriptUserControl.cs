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
using System.Runtime.CompilerServices;
using System.Web.UI;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.AspNet.MembersInjectors;

namespace Microsoft.Scripting.AspNet.UI {
    public class ScriptUserControl : UserControl, IScriptTemplateControl {
        private ScriptTemplateControl _scriptTemplateControl;

        protected override void OnInit(EventArgs e) {
            // Make sure we're initialized before calling the base so that any OnInit handler
            // is properly registered in time.
            // Don't do anything if we don't have a ScriptLanguage (i.e. it's not a dynamic page)
            if (ScriptLanguage != null)
                EnsureScriptTemplateControl();

            base.OnInit(e);
        }

        /// <summary>
        /// Gets or sets the language to be specified in the page directive (e.g. &lt;%@ Page scriptlanguage="IronPython" %&gt;)
        /// </summary>
        public string ScriptLanguage { get; set; }

        [SpecialName]
        public object GetBoundMember(string name) {
            object res = ControlMembersInjector.GetBoundMember(this, name);
            if (res != OperationFailed.Value) {
                return res;
            }

            return EnsureScriptTemplateControl().MemberProxy.GetBoundMember(name);
        }

        [SpecialName]
        public void SetMemberAfter(string name, object value) {
            EnsureScriptTemplateControl().MemberProxy.SetMemberAfter(name, value);
        }

        [SpecialName]
        public bool DeleteMember(string name) {
            return EnsureScriptTemplateControl().MemberProxy.DeleteMember(name);
        }

        ScriptTemplateControl EnsureScriptTemplateControl() {
            // Initialize it on demand
            if (_scriptTemplateControl == null) {
                _scriptTemplateControl = new ScriptTemplateControl(this);
                _scriptTemplateControl.HookUpScript(ScriptLanguage);
            }

            return _scriptTemplateControl;
        }

        #region IScriptTemplateControl Members
        ScriptTemplateControl IScriptTemplateControl.ScriptTemplateControl {
            get { return this.EnsureScriptTemplateControl(); }
        }

        public virtual string InlineScript { get; set; }

        public virtual int InlineScriptLine { get; set; }

        #endregion
    }
}
