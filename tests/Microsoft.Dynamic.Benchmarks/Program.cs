// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// [CallerFilePath] is substituted by the compiler with the absolute path of this
// source file, which co-locates with the .csproj. This is reliable regardless of
// where the compiled output is placed.
static string ProjectDirectory([CallerFilePath] string path = "") =>
    Path.GetDirectoryName(path)!;

string artifactsPath = Path.Combine(ProjectDirectory(), "artifacts");

var config = DefaultConfig.Instance.WithArtifactsPath(artifactsPath);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(ExpandShortcuts(args), config);

// Project-specific shortcut flags. Each is rewritten into the equivalent
// BenchmarkDotNet --filter pair before the args reach BenchmarkSwitcher.
//   --operator   →  --filter *OperatorBenchmarks*   (run only OperatorBenchmarks)
//   --dictionary →  --filter *DictionaryBenchmarks* (run only DictionaryBenchmarks)
static string[] ExpandShortcuts(string[] args) {
    var result = new List<string>(args.Length);
    foreach (var arg in args) {
        switch (arg) {
            case "--operator":
                result.Add("--filter");
                result.Add("*OperatorBenchmarks*");
                break;
            case "--dictionary":
                result.Add("--filter");
                result.Add("*DictionaryBenchmarks*");
                break;
            default:
                result.Add(arg);
                break;
        }
    }
    return result.ToArray();
}
