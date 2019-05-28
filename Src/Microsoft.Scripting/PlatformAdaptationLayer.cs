// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {

#if !FEATURE_PROCESS
    public class ExitProcessException : Exception {

        public int ExitCode { get { return exitCode; } }
        int exitCode;

        public ExitProcessException(int exitCode) {
            this.exitCode = exitCode;
        }
    }
#endif

    /// <summary>
    /// Abstracts system operations that are used by DLR and could potentially be platform specific.
    /// The host can implement its PAL to adapt DLR to the platform it is running on.
    /// For example, the Silverlight host adapts some file operations to work against files on the server.
    /// </summary>
    [Serializable]
    public class PlatformAdaptationLayer {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PlatformAdaptationLayer Default = new PlatformAdaptationLayer();

        [Obsolete("This will be removed in the the future.")]
        public static readonly bool IsCompactFramework = false;

        public static bool IsNativeModule { get; } = _IsNativeModule();

        private static bool _IsNativeModule() {
            return typeof(void).Assembly.Modules.FirstOrDefault()?.ToString() == "<Unknown>";
        }

        #region Assembly Loading

#if NETCOREAPP2_1
        static PlatformAdaptationLayer() {
            // https://github.com/dotnet/coreclr/issues/11498
            // attempt to resolve dependencies in the requesting directory
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                if (args.RequestingAssembly == null) return null;
                var path = Path.Combine(Path.GetDirectoryName(args.RequestingAssembly.Location), new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(path)) {
                    try { return Assembly.LoadFrom(path); }
                    catch { }
                }
                return null;
            };
        }
#endif

        public virtual Assembly LoadAssembly(string name) {
            return Assembly.Load(name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile")]
        public virtual Assembly LoadAssemblyFromPath(string path) {
#if FEATURE_FILESYSTEM
            return Assembly.LoadFile(path);
#else
            throw new NotImplementedException();
#endif
        }

        public virtual void TerminateScriptExecution(int exitCode) {
#if FEATURE_PROCESS
            System.Environment.Exit(exitCode);
#else
            throw new ExitProcessException(exitCode);
#endif
        }

        #endregion

        #region Virtual File System

        public virtual bool IsSingleRootFileSystem {
            get {
#if FEATURE_FILESYSTEM
                return Environment.OSVersion.Platform == PlatformID.Unix
                    || Environment.OSVersion.Platform == PlatformID.MacOSX;
#else
                return true;
#endif
            }
        }

        public virtual StringComparer PathComparer {
            get {
#if FEATURE_FILESYSTEM
                return Environment.OSVersion.Platform == PlatformID.Unix ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
#else
                return StringComparer.OrdinalIgnoreCase;
#endif
            }
        }

        public virtual bool FileExists(string path) {
#if FEATURE_FILESYSTEM
            return File.Exists(path);
#else
            throw new NotImplementedException();
#endif
        }

        public virtual bool DirectoryExists(string path) {
#if FEATURE_FILESYSTEM
            return Directory.Exists(path);
#else
            throw new NotImplementedException();
#endif
        }

        // TODO: better APIs
        public virtual Stream OpenFileStream(string path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read, int bufferSize = 8192) {
#if FEATURE_FILESYSTEM
            if (string.Equals("nul", path, StringComparison.InvariantCultureIgnoreCase)) {
                return Stream.Null;
            }
            return new FileStream(path, mode, access, share, bufferSize);
#else
            throw new NotImplementedException();
#endif
        }

        // TODO: better APIs
        public virtual Stream OpenInputFileStream(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read, int bufferSize = 8192) {
            return OpenFileStream(path, mode, access, share, bufferSize);
        }

        // TODO: better APIs
        public virtual Stream OpenOutputFileStream(string path) {
            return OpenFileStream(path, FileMode.Create, FileAccess.Write);
        }

        public virtual void DeleteFile(string path, bool deleteReadOnly) {
#if FEATURE_FILESYSTEM
            FileInfo info = new FileInfo(path);

            if (deleteReadOnly && info.IsReadOnly) {
                info.IsReadOnly = false;
            }

            info.Delete();
#else
            throw new NotImplementedException();
#endif
        }

        public string[] GetFiles(string path, string searchPattern) {
            return GetFileSystemEntries(path, searchPattern, true, false);
        }

        public string[] GetDirectories(string path, string searchPattern) {
            return GetFileSystemEntries(path, searchPattern, false, true);
        }

        public string[] GetFileSystemEntries(string path, string searchPattern) {
            return GetFileSystemEntries(path, searchPattern, true, true);
        }

        public virtual string[] GetFileSystemEntries(string path, string searchPattern, bool includeFiles, bool includeDirectories) {
#if FEATURE_FILESYSTEM
            if (includeFiles && includeDirectories) {
                return Directory.GetFileSystemEntries(path, searchPattern);
            }
            if (includeFiles) {
                return Directory.GetFiles(path, searchPattern);
            }
            if (includeDirectories) {
                return Directory.GetDirectories(path, searchPattern);
            }
            return ArrayUtils.EmptyStrings;
#else
            throw new NotImplementedException();
#endif
        }

        /// <exception cref="ArgumentException">Invalid path.</exception>
        public virtual string GetFullPath(string path) {
#if FEATURE_FILESYSTEM
            try {
                return Path.GetFullPath(path);
            } catch (Exception) {
                throw Error.InvalidPath();
            }
#else
            throw new NotImplementedException();
#endif
        }

        public virtual string CombinePaths(string path1, string path2) {
            return Path.Combine(path1, path2);
        }

        public virtual string GetFileName(string path) {
            return Path.GetFileName(path);
        }

        public virtual string GetDirectoryName(string path) {
            return Path.GetDirectoryName(path);
        }

        public virtual string GetExtension(string path) {
            return Path.GetExtension(path);
        }

        public virtual string GetFileNameWithoutExtension(string path) {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <exception cref="ArgumentException">Invalid path.</exception>
        public virtual bool IsAbsolutePath(string path) {
#if FEATURE_FILESYSTEM
            // GetPathRoot returns either :
            // "" -> relative to the current dir
            // "\" -> relative to the drive of the current dir
            // "X:" -> relative to the current dir, possibly on a different drive
            // "X:\" -> absolute
            if (IsSingleRootFileSystem) {
                return Path.IsPathRooted(path);
            }
            var root = Path.GetPathRoot(path);
            return root.EndsWith(@":\") || root.EndsWith(@":/");
#else
            throw new NotImplementedException();
#endif
        }

        public virtual string CurrentDirectory {
            get {
#if FEATURE_FILESYSTEM
                return Directory.GetCurrentDirectory();
#else
                throw new NotImplementedException();
#endif
            }
            set {
#if FEATURE_FILESYSTEM
                Directory.SetCurrentDirectory(value);
#else
                throw new NotImplementedException();
#endif
            }
        }

        public virtual void CreateDirectory(string path) {
#if FEATURE_FILESYSTEM
            Directory.CreateDirectory(path);
#else
            throw new NotImplementedException();
#endif
        }

        public virtual void DeleteDirectory(string path, bool recursive) {
#if FEATURE_FILESYSTEM
            Directory.Delete(path, recursive);
#else
            throw new NotImplementedException();
#endif
        }

        public virtual void MoveFileSystemEntry(string sourcePath, string destinationPath) {
#if FEATURE_FILESYSTEM
            Directory.Move(sourcePath, destinationPath);
#else
            throw new NotImplementedException();
#endif
        }

        #endregion

        #region Environmental Variables

        public virtual string GetEnvironmentVariable(string key) {
#if FEATURE_PROCESS
            return Environment.GetEnvironmentVariable(key);
#else
            throw new NotImplementedException();
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public virtual void SetEnvironmentVariable(string key, string value) {
#if FEATURE_PROCESS
            if (value != null && value.Length == 0) {
                SetEmptyEnvironmentVariable(key);
            } else {
                Environment.SetEnvironmentVariable(key, value);
            }
#else
            throw new NotImplementedException();
#endif
        }

#if FEATURE_PROCESS
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2149:TransparentMethodsMustNotCallNativeCodeFxCopRule")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetEmptyEnvironmentVariable(string key) {
            // System.Environment.SetEnvironmentVariable interprets an empty value string as 
            // deleting the environment variable. So we use the native SetEnvironmentVariable 
            // function here which allows setting of the value to an empty string.
            // This will require high trust and will fail in sandboxed environments
            if (!NativeMethods.SetEnvironmentVariable(key, String.Empty)) {
                throw new ExternalException("SetEnvironmentVariable failed", Marshal.GetLastWin32Error());
            }
        }
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual Dictionary<string, string> GetEnvironmentVariables() {
#if FEATURE_PROCESS
            var result = new Dictionary<string, string>();

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                result.Add((string)entry.Key, (string)entry.Value);
            }

            return result;
#else
            throw new NotImplementedException();
#endif
        }

        #endregion
    }
}
