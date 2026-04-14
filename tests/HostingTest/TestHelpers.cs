// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;
using NUnit.Framework;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace HostingTest {
    using Assert = NUnit.Framework.Assert;

    internal class TestHelpers {

        /// <summary>
        /// A stream that wraps a TextWriter, converting byte writes to text.
        /// </summary>
        private sealed class TextWriterStream : Stream {
            private readonly TextWriter _writer;
            private readonly Encoding _encoding;

            public TextWriterStream(TextWriter writer, Encoding encoding = null) {
                _writer = writer;
                _encoding = encoding ?? Encoding.UTF8;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => _writer.Flush();

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) {
                var text = _encoding.GetString(buffer, offset, count);
                _writer.Write(text);
            }
        }

        /// <summary>
        /// Config file containing the tested languages - py,rb,ts
        /// </summary>
        public static string StandardConfigFile { get; private set; }

        /// <summary>
        ///Directory where tests execute and binaries are loaded from 
        /// </summary>
        public static string BinDirectory { get; private set; }

        /// <summary>
        /// Path to IronPython's standard library, or empty if not found.
        /// </summary>
        public static string IronPythonPath { get; private set; }

        static TestHelpers() {
            BinDirectory = Path.GetDirectoryName(typeof(HAPITestBase).Assembly.Location);
            StandardConfigFile = GetStandardConfigFile();
            IronPythonPath = GetIronPythonPath();
        }

        /// <summary>
        /// Finds the root of the IronPython repository by walking up from the assembly location.
        /// </summary>
        private static string FindIronPythonRepositoryRoot() {
            // We start at the current assembly location and look up until we find the "src/core/IronPython.StdLib/lib" directory
            var current = typeof(HAPITestBase).Assembly.Location;
            while (!string.IsNullOrEmpty(current)) {
                var test = Path.Combine(current, "src", "core", "IronPython.StdLib", "lib");
                if (Directory.Exists(test)) {
                    return current;
                }
                current = Path.GetDirectoryName(current);
            }
            return string.Empty;
        }

        private static string GetIronPythonPath() {
            var root = FindIronPythonRepositoryRoot();
            if (!string.IsNullOrEmpty(root)) {
                var path = Path.Combine(root, "src", "core", "IronPython.StdLib", "lib");
                if (Directory.Exists(path)) {
                    return path;
                }
            }
            return string.Empty;
        }

        private static string GetStandardConfigFile() {
            var configFile = typeof(HAPITestBase).Assembly.Location + ".config";
            Debug.Assert(File.Exists(configFile), configFile);
            return configFile;
        }

        internal static T AssertExceptionThrown<T>(System.Action f) where T : Exception {
            try {
                f();
            }
            catch (T ex) {
                return ex;
            }

            Assert.Fail("Expecting exception '" + typeof(T) + "'.");
            return null;
        }

        internal static void RedirectOutput(ScriptRuntime runTime, TextWriter output, System.Action f) {
            var stream = new TextWriterStream(output);
            runTime.IO.SetOutput(stream, output);
            runTime.IO.SetErrorOutput(stream, output);

            try {
                f();
            }
            finally {
                runTime.IO.RedirectToConsole();
            }
        }
        
        [Flags]
        internal enum OutputFlags {
            None = 0,
            Raw = 1
        }

        internal static void AssertOutput(ScriptRuntime runTime, System.Action f, string expectedOutput) {
            AssertOutput(runTime, f, expectedOutput, OutputFlags.None);
        }


        internal static void AssertOutput(ScriptRuntime runTime, System.Action f, string expectedOutput, OutputFlags flags) {
            StringBuilder builder = new StringBuilder();

            using (StringWriter output = new StringWriter(builder)) {
                RedirectOutput(runTime, output, f);
            }

            string actualOutput = builder.ToString();

            if ((flags & OutputFlags.Raw) == 0) {
                actualOutput = actualOutput.Trim();
                expectedOutput = expectedOutput.Trim();
            }

            Assert.IsTrue(actualOutput == expectedOutput, "Unexpected output: '" +
                builder.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "'.");
        }
        
        internal static void AreEqualArrays<T>(IList<T> expected, IList<T> actual) {
            Assert.AreEqual(actual.Count, expected.Count);
            for (int i = 0; i < actual.Count; i++) {
                Assert.AreEqual(actual[i], expected[i]);
            }
        }

        internal static void AreEqualIEnumerables<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
            TestHelpers.AreEqualArrays(  new List<T>(expected).ToArray(), new List<T>(actual).ToArray());
        }

        internal static void AreEqualCollections<T>(T[] expected, IEnumerable<T> actual) {
            TestHelpers.AreEqualArrays(expected, (new List<T>(actual).ToArray()));
        }

        /// <summary>
        /// Create a temp file
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        internal static string CreateTempFile(string contents) {
            // TODO: Add temp file to a list for tear down(deletion)
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }

        /// <summary>
        /// Create a temp source file
        /// </summary>
        /// <param name="contents">Contents of code</param>
        /// <param name="extention">File extension like ".py" or ".js"</param>
        /// <returns></returns>
        internal static string CreateTempSourceFile(string contents, string extention) {
            // TODO: Add temp file to a list for tear down(deletion)
            string tempFile = Path.GetTempFileName();
            string newFile = Path.ChangeExtension(tempFile, extention);
            File.WriteAllText(newFile, contents);
            return newFile;
        }


        public static AppDomain CreateAppDomain(string name) {
            var setup = new AppDomainSetup {
                ApplicationBase = BinDirectory,
                PrivateBinPath = BinDirectory,
                ConfigurationFile = StandardConfigFile
            };
            return AppDomain.CreateDomain(name, null, setup);
        }

        public class EnvSetupTearDown {
            string _envName;
            string _oldEnvEntry;

            public EnvSetupTearDown(string name, string newValue) {
                _envName = name;
                _oldEnvEntry = Environment.GetEnvironmentVariable(name);
                
                Environment.SetEnvironmentVariable(name, newValue);
            }
            ~EnvSetupTearDown() {
                //Rest old values
                Environment.SetEnvironmentVariable(_envName, _oldEnvEntry);
            }
        }
    }
}