// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell {

    public class BasicConsole : IConsole, IDisposable {
        private TextWriter _output;
        private TextWriter _errorOutput;

        public TextWriter Output {
            get => _output;
            set {
                ContractUtils.RequiresNotNull(value, nameof(value));
                _output = value;
            }
        }

        public TextWriter ErrorOutput {
            get => _errorOutput;
            set {
                ContractUtils.RequiresNotNull(value, nameof(value));
                _errorOutput = value;
            }
        }

        protected AutoResetEvent CtrlCEvent { get; set; }

        protected Thread CreatingThread { get; set; }

        public ConsoleCancelEventHandler ConsoleCancelEventHandler { get; set; }
        private ConsoleColor _promptColor;
        private ConsoleColor _outColor;
        private ConsoleColor _errorColor;
        private ConsoleColor _warningColor;

        private BasicConsole(bool colorful, bool? isDarkConsole) {
            _output = Console.Out;
            _errorOutput = Console.Error;
            SetupColors(colorful, isDarkConsole);

            CreatingThread = Thread.CurrentThread;

            // Create the default handler
            ConsoleCancelEventHandler = delegate(object sender, ConsoleCancelEventArgs e) {
                if (e.SpecialKey == ConsoleSpecialKey.ControlC) {
                    e.Cancel = true;
                    CtrlCEvent.Set();
#pragma warning disable SYSLIB0006 // Type or member is obsolete
                    CreatingThread.Abort(new KeyboardInterruptException(""));
#pragma warning restore SYSLIB0006 // Type or member is obsolete
                }
            };

            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
                // Dispatch the registered handler
                ConsoleCancelEventHandler handler = ConsoleCancelEventHandler;
                if (handler != null) {
                    ConsoleCancelEventHandler(sender, e);
                }
            };

            CtrlCEvent = new AutoResetEvent(false);
        }

        public BasicConsole(bool colorful) : this(colorful, null) { }

        public BasicConsole(ConsoleOptions options) : this(options.ColorfulConsole, options.DarkConsole) { }

        private void SetupColors(bool colorful, bool? isDarkConsole) {

            if (colorful) {
                bool darkBackground = HasDarkBackground(isDarkConsole);
                _promptColor = PickColor(ConsoleColor.White, Console.ForegroundColor, darkBackground);
                _outColor = PickColor(ConsoleColor.Cyan, Console.ForegroundColor, darkBackground);
                _errorColor = PickColor(ConsoleColor.Red, Console.ForegroundColor, darkBackground);
                _warningColor = PickColor(ConsoleColor.Yellow, Console.ForegroundColor, darkBackground);
            } else {
                _promptColor = _outColor = _errorColor = _warningColor = Console.ForegroundColor;
            }
        }

        private static ConsoleColor PickColor(ConsoleColor best, ConsoleColor other, bool darkBackground) {
            best = darkBackground ? MakeLight(best) : MakeDark(best);
            other = darkBackground ? MakeLight(other) : MakeDark(other);

            if (Console.BackgroundColor != best) {
                return best;
            }

            return other;
        }

        private static bool HasDarkBackground(bool? isDarkConsole) {
            // Use preference if specified
            if (isDarkConsole.HasValue) {
                return isDarkConsole.Value;
            }

            // Try autodetect
            if (Enum.IsDefined(typeof(ConsoleColor), Console.BackgroundColor)) {
                return IsDark(Console.BackgroundColor);
            }

            // On Unix, Console.BackgroundColor may be undefined (-1)
            // Assume it's dark, which is a fair guess on Linux but poor on macOS
            return true;
        }

        private static bool IsDark(ConsoleColor color) {
            // The dark colours are < 8 and the light are > 8,
            // but the two grays are a bit special
            return color < ConsoleColor.Gray || color == ConsoleColor.DarkGray;
        }

        private static ConsoleColor MakeLight(ConsoleColor color) {
            if (!IsDark(color)) return color;

            return color switch {
                ConsoleColor.DarkGray => ConsoleColor.White, // DarkGray would stay dark gray, which would be hard to read on a dark background
                ConsoleColor.Black => ConsoleColor.Gray,     // Black would turn into DarkGray, which would be hard to read on a dark background
                _ => (ConsoleColor)(((int)color) | 0b1000)   // The light colours all have their 4th bit set
            };
        }

        private static ConsoleColor MakeDark(ConsoleColor color) {
            if (IsDark(color)) return color;

            return color switch {
                ConsoleColor.Gray => ConsoleColor.Black,     // Gray would stay gray, which would be hard to read on a light background
                ConsoleColor.White => ConsoleColor.DarkGray, // White would turn into Gray, which would be hard to read on a light background
                _ => (ConsoleColor)(((int)color) & ~0b1000)  // The dark colours all have their 4th bit unset
            };
        }

        protected void WriteColor(TextWriter output, string str, ConsoleColor c) {
            ConsoleColor origColor = Console.ForegroundColor;
            Console.ForegroundColor = c;
      
            output.Write(str);
            output.Flush();

            Console.ForegroundColor = origColor;
        }

        #region IConsole Members

        public virtual string ReadLine(int autoIndentSize) {
            Write("".PadLeft(autoIndentSize), Style.Prompt);

            string res = Console.In.ReadLine();
            if (res == null) {
                // we have a race - the Ctrl-C event is delivered
                // after ReadLine returns.  We need to wait for a little
                // bit to see which one we got.  This will cause a slight
                // delay when shutting down the process via ctrl-z, but it's
                // not really perceptible.  In the ctrl-C case we will return
                // as soon as the event is signaled.
                if (CtrlCEvent != null && CtrlCEvent.WaitOne(100, false)) {
                    // received ctrl-C
                    return "";
                }
                    
                // received ctrl-Z
                return null;
            }
            return "".PadLeft(autoIndentSize) + res;
        }

        public virtual void Write(string text, Style style) {
            switch (style) {
                case Style.Prompt: WriteColor(_output, text, _promptColor); break;
                case Style.Out: WriteColor(_output, text, _outColor); break;
                case Style.Error: WriteColor(_errorOutput, text, _errorColor); break;
                case Style.Warning: WriteColor(_errorOutput, text, _warningColor); break;
            }
        }

        public void WriteLine(string text, Style style) {
            Write(text + Environment.NewLine, style);
        }

        public void WriteLine() {
            Write(Environment.NewLine, Style.Out);
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            CtrlCEvent?.Close();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
