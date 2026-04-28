# Running benchmarks

To run all benchmarks, execute the following command from the DLR directory:

```powershell
dotnet run --configuration Release `
           --project tests/Microsoft.Dynamic.Benchmarks `
           --framework net10.0 `
           -- `
           --filter '*'
```

If using bash, zsh: replace backticks with backslashes. Or put the whole command on one line.

Run with option `--help` (after `--`) to obtain a list of all supported options.

Replace `net10.0` with `net462` to get benchmarks on .NET Framework (Windows only).

It can take a long while to run all benchmarks. It is not perfect (it runs "WriteOnly" tests twice for different number of readers, though `ReaderCount` is not used), but good enough.

Possible filters:
 * `*` — all benchmarks
 * `*ReadOnly*` — a given number of key/value pairs, only read, so good at testing `FrozenDictionary` and `FrozenSet`.
 * `*WriteOnly*` — single-threaded sequential writes.
 * `*SWMR*` — _Single Writer, Multiple Readers_: this is the scenario `Hastable` was intended for.
 * `*OperatorBenchmarks*` — reading-speed benchmarks for `OperatorInfo.MakeOperatorTable` (.NET 8+ only).

Project-specific shortcut flags (resolved before the arguments are handed to BenchmarkDotNet):
 * `--operator` — equivalent to `--filter *OperatorBenchmarks*`.
 * `--dictionary` — equivalent to `--filter *DictionaryBenchmarks*`.

Example — run only the operator-table reading benchmarks:

```powershell
dotnet run --configuration Release `
           --project tests/Microsoft.Dynamic.Benchmarks `
           --framework net10.0 `
           -- --operator
```

Remove attributes `SimpleJob` and/or provide additional runtimes with `--runtimes` attribute if more tailored tests are needed.


# Results

The results are placed in directory `artifacts` where the main `Program.cs` file is located. Subdirectory `results` contains a summary of benchmark runs in various formats. This directory is not versioned and overwritten with a new run. Rename it to preserve results.
