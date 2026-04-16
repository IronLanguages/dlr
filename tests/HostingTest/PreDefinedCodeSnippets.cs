// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace HostingTest {

    public class CodeSnippet(string id, string description, string code) {
        public string ID = id;
        public string Code = code;
        public string Description = description;
    }

    internal class CodeSnippetCollection {
        internal CodeSnippet[] AllSnippets;

        internal CodeSnippet GetCodeSnippetByID(string id) =>
            Array.Find(AllSnippets, cs => cs.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

        internal string this[string id] => GetCodeSnippetByID(id)?.Code;
    }

    internal class PreDefinedCodeSnippets {

        internal CodeSnippetCollection langCollection;

        internal PreDefinedCodeSnippets() {
            langCollection = GetLanguage() switch {
                "python" => new PythonCodeSnippets(),
                "ironruby" => new RubyCodeSnippets(),
                _ => null
            };
        }

        private string GetLanguage() {
            return "python";
        }

        public string this[string id] => langCollection[id];

        internal CodeSnippet[] AllSnippets => langCollection.AllSnippets;
    }

}
