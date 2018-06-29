// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_FULL_CONSOLE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell {
    /// <summary>
    /// Core functionality to implement an interactive console. This should be derived for concrete implementations
    /// </summary>
    public abstract class ConsoleHost {
        private int _exitCode;
        private ConsoleHostOptionsParser _optionsParser;
        private ScriptRuntime _runtime;
        private ScriptEngine _engine;
        private ConsoleOptions _consoleOptions;
        private IConsole _console;
        private CommandLine _commandLine;

        public ConsoleHostOptions Options { get { return _optionsParser.Options; } }
        public ScriptRuntimeSetup RuntimeSetup { get { return _optionsParser.RuntimeSetup; } }

        public ScriptEngine Engine { get { return _engine; } protected set { _engine = value; } }
        public ScriptRuntime Runtime { get { return _runtime; } protected set { _runtime = value; } }
        protected int ExitCode { get { return _exitCode; } set { _exitCode = value; } }
        protected ConsoleHostOptionsParser ConsoleHostOptionsParser { get { return _optionsParser; } set { _optionsParser = value; } }
        protected IConsole ConsoleIO { get { return _console; } set { _console = value; } }
        protected CommandLine CommandLine { get { return _commandLine; } }

        /// <summary>
        /// Console Host entry-point .exe name.
        /// </summary>
        protected virtual string ExeName {
            get {
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                
                // Can be null if called from unmanaged code (VS integration scenario)
                return entryAssembly != null ? entryAssembly.GetName().Name : "ConsoleHost";
            }
        }

        #region Customization

        protected virtual void ParseHostOptions(string[] args) {
            _optionsParser.Parse(args);
        }

        protected virtual ScriptRuntimeSetup CreateRuntimeSetup() {
            var setup = ScriptRuntimeSetup.ReadConfiguration();

            string provider = Provider.AssemblyQualifiedName;

            if (!setup.LanguageSetups.Any(s => s.TypeName == provider)) {
                var languageSetup = CreateLanguageSetup();
                if (languageSetup != null) {
                    setup.LanguageSetups.Add(languageSetup);
                }
            }

            return setup;
        }

        protected virtual LanguageSetup CreateLanguageSetup() {
            return null;
        }

        protected virtual PlatformAdaptationLayer PlatformAdaptationLayer {
            get { return PlatformAdaptationLayer.Default; }
        }

        protected virtual Type Provider {
            get { return null; }
        }

        private string GetLanguageProvider(ScriptRuntimeSetup setup) {
            var providerType = Provider;
            if (providerType != null) {
                return providerType.AssemblyQualifiedName;
            }
            
            if (Options.HasLanguageProvider) {
                return Options.LanguageProvider;
            }

            if (Options.RunFile != null) {
                string ext = Path.GetExtension(Options.RunFile);
                foreach (var lang in setup.LanguageSetups) {
                    if (lang.FileExtensions.Any(e => DlrConfiguration.FileExtensionComparer.Equals(e, ext))) {
                        return lang.TypeName;
                    }
                }
            }

            throw new InvalidOptionException("No language specified.");
        }

        protected virtual CommandLine CreateCommandLine() {
            return new CommandLine();
        }

        protected virtual OptionsParser CreateOptionsParser() {
            return new OptionsParser<ConsoleOptions>();
        }

        protected virtual IConsole CreateConsole(ScriptEngine engine, CommandLine commandLine, ConsoleOptions options) {
            ContractUtils.RequiresNotNull(options, nameof(options));

            if (options.TabCompletion) {
                return CreateSuperConsole(commandLine, options.ColorfulConsole);
            } else {
                return new BasicConsole(options.ColorfulConsole);
            }
        }

        // The advanced console functions are in a special non-inlined function so that 
        // dependencies are pulled in only if necessary.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static IConsole CreateSuperConsole(CommandLine commandLine, bool isColorful) {
            return new SuperConsole(commandLine, isColorful);
        }

        #endregion

        /// <summary>
        /// Request (from another thread) the console REPL loop to terminate
        /// </summary>
        /// <param name="exitCode">The caller can specify the exitCode corresponding to the event triggering
        /// the termination. This will be returned from CommandLine.Run</param>
        public virtual void Terminate(int exitCode) {
            _commandLine.Terminate(exitCode);
        }

        /// <summary>
        /// To be called from entry point.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public virtual int Run(string[] args) {
            var runtimeSetup = CreateRuntimeSetup();
            var options = new ConsoleHostOptions();
            _optionsParser = new ConsoleHostOptionsParser(options, runtimeSetup);

            try {
                ParseHostOptions(args);
            } catch (InvalidOptionException e) {
                Console.Error.WriteLine("Invalid argument: " + e.Message);
                return _exitCode = 1;
            }

            SetEnvironment();

            string provider = GetLanguageProvider(runtimeSetup);

            LanguageSetup languageSetup = null;
            foreach (var language in runtimeSetup.LanguageSetups) {
                if (language.TypeName == provider) {
                    languageSetup = language;
                }
            }
            if (languageSetup == null) {
                // the language doesn't have a setup -> create a default one:
                languageSetup = new LanguageSetup(Provider.AssemblyQualifiedName, Provider.Name);
                runtimeSetup.LanguageSetups.Add(languageSetup);
            }

            // inserts search paths for all languages (/paths option):
            InsertSearchPaths(runtimeSetup.Options, Options.SourceUnitSearchPaths);

            _consoleOptions = ParseOptions(Options.IgnoredArgs.ToArray(), runtimeSetup, languageSetup);
            if (_consoleOptions == null) {
                return _exitCode = 1;
            }

            _runtime = new ScriptRuntime(runtimeSetup);

            try {
                _engine = _runtime.GetEngineByTypeName(provider);
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                return _exitCode = 1;
            }

            Execute();
            return _exitCode;
        }

        protected virtual ConsoleOptions ParseOptions(string/*!*/[]/*!*/ args, ScriptRuntimeSetup/*!*/ runtimeSetup, LanguageSetup/*!*/ languageSetup) {
            var languageOptionsParser = CreateOptionsParser();

            try {
                languageOptionsParser.Parse(args, runtimeSetup, languageSetup, PlatformAdaptationLayer);
            } catch (InvalidOptionException e) {
                ReportInvalidOption(e);
                return null;
            }

            return languageOptionsParser.CommonConsoleOptions;            
        }

        protected virtual void ReportInvalidOption(InvalidOptionException e) {
            Console.Error.WriteLine(e.Message);
        }

        private static void InsertSearchPaths(IDictionary<string, object> options, ICollection<string> paths) {
            if (options != null && paths != null && paths.Count > 0) {
                var existingPaths = new List<string>(LanguageOptions.GetSearchPathsOption(options) ?? (IEnumerable<string>)ArrayUtils.EmptyStrings);
                existingPaths.InsertRange(0, paths);
                options["SearchPaths"] = existingPaths;
            }
        }

        #region Printing help

        protected virtual void PrintHelp() {
            Console.WriteLine(GetHelp());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        protected virtual string GetHelp() {
            StringBuilder sb = new StringBuilder();

            string[,] optionsHelp = Options.GetHelp();

            sb.AppendLine($"Usage: {ExeName}.exe [<dlr-options>] [--] [<language-specific-command-line>]");
            sb.AppendLine();

            sb.AppendLine("DLR options (both slash or dash could be used to prefix options):");
            ArrayUtils.PrintTable(sb, optionsHelp);
            sb.AppendLine();

            sb.AppendLine("Language specific command line:");
            PrintLanguageHelp(sb);
            sb.AppendLine();

            return sb.ToString();
        }

        public void PrintLanguageHelp(StringBuilder output) {
            ContractUtils.RequiresNotNull(output, nameof(output));

            CreateOptionsParser().GetHelp(out string commandLine, out string[,] options, out string[,] environmentVariables, out string comments);

            // only display language specific options if one or more optinos exists.
            if (commandLine != null || options != null || environmentVariables != null || comments != null) {
                if (commandLine != null) {
                    output.AppendLine(commandLine);
                    output.AppendLine();
                }

                if (options != null) {
                    output.AppendLine("Options:");
                    ArrayUtils.PrintTable(output, options);
                    output.AppendLine();
                }

                if (environmentVariables != null) {
                    output.AppendLine("Environment variables:");
                    ArrayUtils.PrintTable(output, environmentVariables);
                    output.AppendLine();
                }

                if (comments != null) {
                    output.Append(comments);
                    output.AppendLine();
                }

                output.AppendLine();
            }
        }

        #endregion

        private void Execute() {
#if FEATURE_APARTMENTSTATE
            if (_consoleOptions.IsMta) {
                Thread thread = new Thread(ExecuteInternal);
                thread.SetApartmentState(ApartmentState.MTA);
                thread.Start();
                thread.Join();
                return;
            }
#endif
            ExecuteInternal();
        }

        protected virtual void ExecuteInternal() {
            Debug.Assert(_engine != null);

            if (_consoleOptions.PrintVersion){
                PrintVersion();
            }

            if (_consoleOptions.PrintUsage) {
                PrintUsage();
            }

            if (_consoleOptions.Exit) {
                _exitCode = 0;
                return;
            }

            switch (Options.RunAction) {
                case ConsoleHostOptions.Action.None:
                case ConsoleHostOptions.Action.RunConsole:
                    _exitCode = RunCommandLine();
                    break;

                case ConsoleHostOptions.Action.RunFile:
                    _exitCode = RunFile();
                    break;

                default:
                    throw Assert.Unreachable;
            }
        }

        private void SetEnvironment() {
            Debug.Assert(Options.EnvironmentVars != null);

            foreach (string env in Options.EnvironmentVars) {
                if (!String.IsNullOrEmpty(env)) {
                    string[] var_def = env.Split('=');
                    System.Environment.SetEnvironmentVariable(var_def[0], (var_def.Length > 1) ? var_def[1] : "");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private int RunFile() {
            Debug.Assert(_engine != null);

            int result = 0;
            try {
                return _engine.CreateScriptSourceFromFile(Options.RunFile).ExecuteProgram();
#if !FEATURE_PROCESS 
            } catch (ExitProcessException e) {
                result = e.ExitCode;
#endif
            } catch (Exception e) {
                UnhandledException(Engine, e);
                result = 1;
            } finally {
#if FEATURE_REFEMIT
                try {
                    Snippets.SaveAndVerifyAssemblies();
                } catch (Exception) {
                    result = 1;
                }
#endif
            }

            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private int RunCommandLine() {
            Debug.Assert(_engine != null);

            _commandLine = CreateCommandLine();

            if (_console == null) {
                _console = CreateConsole(Engine, _commandLine, _consoleOptions);
            }

            int? exitCodeOverride = null;

            try {
                if (_consoleOptions.HandleExceptions) {
                    try {
                        _commandLine.Run(Engine, _console, _consoleOptions);
                    } catch (Exception e) {
                        if (CommandLine.IsFatalException(e)) {
                            // Some exceptions are too dangerous to try to catch
                            throw;
                        }
                        UnhandledException(Engine, e);
                    }
                } else {
                    _commandLine.Run(Engine, _console, _consoleOptions);
                }
            } finally {
#if FEATURE_REFEMIT
                try {
                    Snippets.SaveAndVerifyAssemblies();
                } catch (Exception) {
                    exitCodeOverride = 1;
                }
#endif
            }

            if (exitCodeOverride == null) {
                return _commandLine.ExitCode;
            } else {
                return exitCodeOverride.Value;
            }
        }

        private void PrintUsage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Usage: {0}.exe ", ExeName);
            PrintLanguageHelp(sb);
            Console.Write(sb.ToString());
        }

        protected  void PrintVersion() {
            Console.WriteLine("{0} {1} on {2}", Engine.Setup.DisplayName, Engine.LanguageVersion, GetRuntime());
        }

        private static string GetRuntime() {
            Type mono = typeof(object).Assembly.GetType("Mono.Runtime");
            return mono != null ?
                (string)mono.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null)
                : String.Format(CultureInfo.InvariantCulture, ".NET {0}", Environment.Version);
        }

        protected virtual void UnhandledException(ScriptEngine engine, Exception e) {
            Console.Error.Write("Unhandled exception");
            Console.Error.WriteLine(':');

            ExceptionOperations es = engine.GetService<ExceptionOperations>();
            Console.Error.WriteLine(es.FormatException(e));
        }

        protected static void PrintException(TextWriter output, Exception e) {
            Debug.Assert(output != null);
            ContractUtils.RequiresNotNull(e, nameof(e));

            while (e != null) {
                output.WriteLine(e);
                e = e.InnerException;
            }
        }
    }
}

#endif