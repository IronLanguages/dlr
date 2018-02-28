// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace HostingTest {

    public class CodeSnippet {
        public string ID;
        public string Code;
        public string Description;

        public CodeSnippet(string id, string description, string code) {
            ID = id ; Code = code;
            Description = description;
        }
    }
    internal class CodeSnippetCollection {
        internal CodeSnippet[] AllSnippets = null;
        internal CodeSnippet GetCodeSnippetByID(string id) {
            foreach (CodeSnippet cs in AllSnippets) {
                if (cs.ID.ToLower() == id.ToLower()) {
                    return cs;
                }
            }
            return null;
        }

        internal string this[string id] {
            get {
                CodeSnippet cs = GetCodeSnippetByID( id );
                return cs == null ? null : cs.Code;
            }
        }
    }

    internal class PreDefinedCodeSnippets {

        internal CodeSnippetCollection langCollection;

        internal PreDefinedCodeSnippets() {
            string lang = GetLanguage();
            if (lang == "python") {//check if lang is python)
                langCollection = new PythonCodeSnippets();
            }
            else if (lang == "ironruby"){
                langCollection = new RubyCodeSnippets();
            }

        }

        private string GetLanguage() {
            return "python";
        }

        public string this[string id] {
            get {
                return langCollection[id];
            }
        }

        internal CodeSnippet[] AllSnippets {
            get {
                return langCollection.AllSnippets;
            }
        }
    }

}

