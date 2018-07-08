// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell {

    [Serializable]
    public class InvalidOptionException : Exception {
        public InvalidOptionException() { }
        public InvalidOptionException(string message) : base(message) { }
        public InvalidOptionException(string message, Exception inner) : base(message, inner) { }

#if FEATURE_SERIALIZATION
        protected InvalidOptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }

    public abstract class OptionsParser {
        private ScriptRuntimeSetup _runtimeSetup;
        private LanguageSetup _languageSetup;
        private PlatformAdaptationLayer _platform;

        private List<string> _ignoredArgs = new List<string>();
        private string[] _args;
        private int _current = -1;

        protected OptionsParser() {
        }

        public ScriptRuntimeSetup RuntimeSetup {
            get { return _runtimeSetup; }
        }

        public LanguageSetup LanguageSetup {
            get { return _languageSetup; }
        }

        public PlatformAdaptationLayer Platform {
            get { return _platform; }
        }

#if FEATURE_FULL_CONSOLE
        public abstract ConsoleOptions CommonConsoleOptions { 
            get; 
        }
#endif
        public IList<string> IgnoredArgs {
            get { return _ignoredArgs; }
        } 

        /// <exception cref="InvalidOptionException">On error.</exception>
        public void Parse(string[] args, ScriptRuntimeSetup setup, LanguageSetup languageSetup, PlatformAdaptationLayer platform) {
            ContractUtils.RequiresNotNull(args, nameof(args));
            ContractUtils.RequiresNotNull(setup, nameof(setup));
            ContractUtils.RequiresNotNull(languageSetup, nameof(languageSetup));
            ContractUtils.RequiresNotNull(platform, nameof(platform));

            _args = args;
            _runtimeSetup = setup;
            _languageSetup = languageSetup;
            _platform = platform;
            _current = 0;
            try {
                BeforeParse();
                while (_current < args.Length) {
                    ParseArgument(args[_current++]);
                }
                AfterParse();
            } finally {
                _args = null;
                _runtimeSetup = null;
                _languageSetup = null;
                _platform = null;
                _current = -1;
            }
        }

        protected virtual void BeforeParse() {
            // nop
        }

        protected virtual void AfterParse() {
        }

        protected abstract void ParseArgument(string arg);

        protected void IgnoreRemainingArgs() {
            while (_current < _args.Length) {
                _ignoredArgs.Add(_args[_current++]);
            }
        }

        protected string[] PopRemainingArgs() {
            string[] result = ArrayUtils.ShiftLeft(_args, _current);
            _current = _args.Length;
            return result;
        }

        protected string PeekNextArg() {
            if (_current < _args.Length)
                return _args[_current];

            throw new InvalidOptionException(
                String.Format(
                    CultureInfo.CurrentCulture,
                    "Argument expected for the {0} option.",
                    _current > 0 ? _args[_current - 1] : string.Empty));
        }

        protected string PopNextArg() {
            string result = PeekNextArg();
            _current++;
            return result;
        }

        protected void PushArgBack() {
            _current--;
        }

        protected static Exception InvalidOptionValue(string option, string value) {
            return new InvalidOptionException($"'{value}' is not a valid value for option '{option}'");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")] // TODO: fix
        public abstract void GetHelp(out string commandLine, out string[,] options, out string[,] environmentVariables, out string comments);
    }

#if FEATURE_FULL_CONSOLE
    public class OptionsParser<TConsoleOptions> : OptionsParser
        where TConsoleOptions : ConsoleOptions, new() {
       
        private TConsoleOptions _consoleOptions;

#if FEATURE_REFEMIT
        private bool _saveAssemblies = false;
        private string _assembliesDir = null;
#endif

        public TConsoleOptions ConsoleOptions {
            get {
                if (_consoleOptions == null) {
                    _consoleOptions = new TConsoleOptions();
                } 
                
                return _consoleOptions; 
            }
            set {
                ContractUtils.RequiresNotNull(value, nameof(value));
                _consoleOptions = value; 
            }
        }

        public sealed override ConsoleOptions CommonConsoleOptions {
            get { return ConsoleOptions; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        protected override void ParseArgument(string arg) {
            ContractUtils.RequiresNotNull(arg, nameof(arg));

            // the following extension switches are in alphabetic order
            switch (arg) {
                case "-h":
                case "-help":
                case "-?":
                case "/?":
                    ConsoleOptions.PrintUsage = true;
                    ConsoleOptions.Exit = true;
                    IgnoreRemainingArgs();
                    break;

                case "-D": 
                    RuntimeSetup.DebugMode = true; 
                    break;
                
                case "-X:PrivateBinding": 
                    RuntimeSetup.PrivateBinding = true; 
                    break;

                case "-X:PassExceptions": ConsoleOptions.HandleExceptions = false; break;
                // TODO: #if !IRONPYTHON_WINDOW
                case "-X:ColorfulConsole": ConsoleOptions.ColorfulConsole = true; break;
                case "-X:TabCompletion": ConsoleOptions.TabCompletion = true; break;
                case "-X:AutoIndent": ConsoleOptions.AutoIndent = true; break;
                //#endif

#if DEBUG
#if FEATURE_REFEMIT
                case "-X:AssembliesDir":
                    _assembliesDir = PopNextArg();
                    break;

                case "-X:SaveAssemblies":
                    _saveAssemblies = true;
                    break;
#endif

                case "-X:TrackPerformance":
                    SetDlrOption(arg.Substring(3));
                    break;
#endif
                // TODO: remove
                case "-X:Interpret":
                    LanguageSetup.Options["InterpretedMode"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-X:NoAdaptiveCompilation":
                    LanguageSetup.Options["NoAdaptiveCompilation"] = true;
                    break;

                case "-X:CompilationThreshold":
                    LanguageSetup.Options["CompilationThreshold"] = Int32.Parse(PopNextArg());
                    break;

                case "-X:ExceptionDetail":
                case "-X:ShowClrExceptions":
#if DEBUG
                case "-X:PerfStats":
#endif
                    // TODO: separate options dictionary?
                    LanguageSetup.Options[arg.Substring(3)] = ScriptingRuntimeHelpers.True; 
                    break;

#if FEATURE_REMOTING
                case Remote.RemoteRuntimeServer.RemoteRuntimeArg:
                    ConsoleOptions.RemoteRuntimeChannel = PopNextArg();
                    break;
#endif
                default:
                    ConsoleOptions.FileName = arg.Trim();
                    break;
            }

#if FEATURE_REFEMIT
            if (_saveAssemblies) {
                Snippets.SetSaveAssemblies(true, _assembliesDir);
            }
#endif
        }

        internal static void SetDlrOption(string option) {
            SetDlrOption(option, "true");
        }

        // Note: this works because it runs before the compiler picks up the
        // environment variable
        internal static void SetDlrOption(string option, string value) {
            Environment.SetEnvironmentVariable("DLR_" + option, value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")] // TODO: fix
        public override void GetHelp(out string commandLine, out string[,] options, out string[,] environmentVariables, out string comments) {

            commandLine = "[options] [file|- [arguments]]";

            options = new string[,] {
                { "-c cmd",                      "Program passed in as string (terminates option list)" },
                { "-h",                          "Display usage" },
                { "-i",                          "Inspect interactively after running script" },
                { "-V",                          "Print the version number and exit" },
                { "-D",                          "Enable application debugging" },

                { "-X:AutoIndent",               "Enable auto-indenting in the REPL loop" },
                { "-X:ExceptionDetail",          "Enable ExceptionDetail mode" },
                { "-X:NoAdaptiveCompilation",    "Disable adaptive compilation" },
                { "-X:CompilationThreshold",     "The number of iterations before the interpreter starts compiling" },
                { "-X:PassExceptions",           "Do not catch exceptions that are unhandled by script code" },
                { "-X:PrivateBinding",           "Enable binding to private members" },
                { "-X:ShowClrExceptions",        "Display CLS Exception information" },
                { "-X:TabCompletion",            "Enable TabCompletion mode" },
                { "-X:ColorfulConsole",          "Enable ColorfulConsole" },
#if DEBUG
                { "-X:AssembliesDir <dir>",      "Set the directory for saving generated assemblies [debug only]" },
                { "-X:SaveAssemblies",           "Save generated assemblies [debug only]" },
                { "-X:TrackPerformance",         "Track performance sensitive areas [debug only]" },
                { "-X:PerfStats",                "Print performance stats when the process exists [debug only]" },
#if FEATURE_REMOTING
                { Remote.RemoteRuntimeServer.RemoteRuntimeArg + " <channel_name>", 
                                                 "Start a remoting server for a remote console session." },
#endif
#endif
           };

            environmentVariables = new string[0, 0];

            comments = null;
        }
    }
#endif
}
