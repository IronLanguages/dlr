// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_FULL_CONSOLE

using System.IO;

namespace Microsoft.Scripting.Hosting.Shell {
    /// <summary>
    /// Handles input and output for the console. It is comparable to System.IO.TextReader, 
    /// System.IO.TextWriter, System.Console, etc
    /// </summary>
    public interface IConsole {
        /// <summary>
        /// Read a single line of interactive input, or a block of multi-line statements.
        /// 
        /// An event-driven GUI console can implement this method by creating a thread that
        /// blocks and waits for an event indicating that input is available
        /// </summary>
        /// <param name="autoIndentSize">The indentation level to be used for the current suite of a compound statement.
        /// The console can ignore this argument if it does not want to support auto-indentation</param>
        /// <returns>null if the input stream has been closed. A string with a command to execute otherwise.
        /// It can be a multi-line string which should be processed as block of statements
        /// </returns>
        string ReadLine(int autoIndentSize);

        void Write(string text, Style style);
        void WriteLine(string text, Style style);
        void WriteLine();

        TextWriter Output { get; set; }
        TextWriter ErrorOutput { get; set; }
    }
}

#endif