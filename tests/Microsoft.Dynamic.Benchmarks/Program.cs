// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
