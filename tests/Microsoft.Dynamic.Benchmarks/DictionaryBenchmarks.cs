// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
#if !NET462
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Microsoft.Scripting.Utils;

namespace Microsoft.Dynamic.Benchmarks;

/// <summary>
/// Compares thread-safety approaches for single-writer / multiple-readers workloads on string→string maps.
/// Each scenario uses <see cref="ReaderCount"/> reader threads plus one writer thread (where applicable).
/// </summary>
///
/// <remarks>
/// Collections under test
/// ──────────────────────
///   ConcurrentDictionary         – lock-striped generic dictionary; the modern .NET recommendation
///   Hashtable                    – BCL non-generic; concurrent reads are safe, writes require external sync
///   lock+Dictionary              – lock-guarded generic Dictionary (same pattern as SynchronizedDictionary)
///   SynchronizedDictionary       – project-local lock-based wrapper (candidate for replacement)
///   FrozenDictionary             – immutable snapshot; fastest reads, no write support
///   FrozenSet                    – immutable set of keys only, no values; included for completeness, not a direct comparison
///   HashSet                      – set of keys only, no values; included for completeness, not a direct comparison
///   ImmutableHashSet             – immutable set of keys only, no values; included for completeness, not a direct comparison  
///
/// Scenarios
/// ─────────
///   ReadOnly       – all readers, no writes; the hot path in production (cache populated once)
///   WriteOnly      – single-threaded sequential writes; baseline for mutation cost
///   SWMR           – Single Writer + Multiple Readers running concurrently
/// </remarks>

#if NET // ThreadingDiagnoser supports only .NET Core 3.0+
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[ThreadingDiagnoser]
#else
[SimpleJob(RuntimeMoniker.Net481)]
#endif
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class DictionaryBenchmarks {
    // ── parameters ──────────────────────────────────────────────────────────

    [Params(4, 8)]
    public int ReaderCount { get; set; }

    [Params(100, 500)]
    public int KeyCount { get; set; }

    // ── operations per scenario invocation ──────────────────────────────────

    private const int ReaderOpsPerThread = 10_000;
    private const int WriterOps          =  1_000;

    // ── pre-computed key/value arrays (avoids string allocation in hot path)

    private string[] _keys = null!;
    private string[] _values = null!;

    // ── collections ─────────────────────────────────────────────────────────

    private ConcurrentDictionary<string, string>   _concurrent    = null!;
    private Hashtable                              _hashtable     = null!;
    private Dictionary<string, string>             _dictionary    = null!;
    private SynchronizedDictionary<string, string> _synchronized  = null!;
#if !NET462
    private FrozenDictionary<string, string>       _frozen        = null!;
    private FrozenSet<string>                      _frozenSet     = null!;
#endif
    private HashSet<string>                        _hashSet       = null!;
    private ImmutableHashSet<string>               _immutableSet  = null!;

    // ── setup ────────────────────────────────────────────────────────────────

    [GlobalSetup]
    public void Setup() {
        _keys   = new string[KeyCount];
        _values = new string[KeyCount];
        for (int i = 0; i < KeyCount; i++) {
            _keys[i]   = $"key_{i}";
            _values[i] = $"value_{i}";
        }

        var seed = new Dictionary<string, string>(KeyCount);
        for (int i = 0; i < KeyCount; i++)
            seed[_keys[i]] = _values[i];

        _concurrent    = new ConcurrentDictionary<string, string>(seed);
        _hashtable     = new Hashtable(seed);
        _dictionary    = new Dictionary<string, string>(seed);
        _synchronized  = new SynchronizedDictionary<string, string>(new Dictionary<string, string>(seed));
#if !NET462
        _frozen        = seed.ToFrozenDictionary();
        _frozenSet     = seed.Keys.ToFrozenSet();
#endif
        _hashSet       = new HashSet<string>(seed.Keys);
        _immutableSet  = seed.Keys.ToImmutableHashSet();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Scenario 1 – Read-only (concurrent reads, no writes)
    // ════════════════════════════════════════════════════════════════════════

    [Benchmark(Description = "ReadOnly – ConcurrentDictionary")]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_ConcurrentDictionary() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            _concurrent.TryGetValue(key, out _);
        });
    }

    [Benchmark(Description = "ReadOnly – Hashtable")]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_Hashtable() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            _ = _hashtable[key];
        });
    }

    [Benchmark(Description = "ReadOnly – lock+Dictionary")]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_LockDictionary() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            lock (_dictionary) {
                _dictionary.TryGetValue(key, out _);
            }
        });
    }

    [Benchmark(Description = "ReadOnly – SynchronizedDictionary")]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_SynchronizedDictionary() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            _synchronized.TryGetValue(key, out _);
        });
    }

#if !NET462
    [Benchmark(Description = "ReadOnly – FrozenDictionary", Baseline = true)]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_FrozenDictionary() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            _frozen.TryGetValue(key, out _);
        });
    }

    [Benchmark(Description = "ReadOnly – FrozenSet")]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_FrozenSet() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            _frozenSet.Contains(key);
        });
    }
#endif

    [Benchmark(Description = "ReadOnly – HashSet")]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_HashSet() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            _hashSet.Contains(key);
        });
    }

    [Benchmark(Description = "ReadOnly – ImmutableHashSet")]
    [BenchmarkCategory("ReadOnly")]
    public void ReadOnly_ImmutableHashSet() {
        RunReaders(i => {
            string key = _keys[i % KeyCount];
            _immutableSet.Contains(key);
        });
    }


    // ════════════════════════════════════════════════════════════════════════
    // Scenario 2 – Write-only (single-threaded sequential writes)
    //   FrozenDictionary is excluded — it is immutable by design.
    // ════════════════════════════════════════════════════════════════════════

    [Benchmark(Description = "WriteOnly – ConcurrentDictionary")]
    [BenchmarkCategory("WriteOnly")]
    public void WriteOnly_ConcurrentDictionary() {
        for (int i = 0; i < WriterOps; i++) {
            string key   = _keys[i % KeyCount];
            string value = _values[(i + 1) % KeyCount];
            _concurrent[key] = value;
        }
    }

    [Benchmark(Description = "WriteOnly – lock+Hashtable")]
    [BenchmarkCategory("WriteOnly")]
    public void WriteOnly_Hashtable() {
        for (int i = 0; i < WriterOps; i++) {
            string key   = _keys[i % KeyCount];
            string value = _values[(i + 1) % KeyCount];
            lock (_hashtable) {
                _hashtable[key] = value;
            }
        }
    }

    [Benchmark(Description = "WriteOnly – lock+Dictionary", Baseline = true)]
    [BenchmarkCategory("WriteOnly")]
    public void WriteOnly_LockDictionary() {
        for (int i = 0; i < WriterOps; i++) {
            string key   = _keys[i % KeyCount];
            string value = _values[(i + 1) % KeyCount];
            lock (_dictionary) {
                _dictionary[key] = value;
            }
        }
    }

    [Benchmark(Description = "WriteOnly – SynchronizedDictionary")]
    [BenchmarkCategory("WriteOnly")]
    public void WriteOnly_SynchronizedDictionary() {
        for (int i = 0; i < WriterOps; i++) {
            string key   = _keys[i % KeyCount];
            string value = _values[(i + 1) % KeyCount];
            _synchronized[key] = value;
        }
    }

    [Benchmark(Description = "WriteOnly – lock+HashSet")]
    [BenchmarkCategory("WriteOnly")]
    public void WriteOnly_HashSet() {
        for (int i = 0; i < WriterOps; i++) {
            string key   = _keys[i % KeyCount];
            string _ = _values[(i + 1) % KeyCount];
            lock (_hashSet) {
                _hashSet.Add(key);
            }
        }
    }


    // ════════════════════════════════════════════════════════════════════════
    // Scenario 3 – Single Writer + Multiple Readers (SWMR)
    //   Writer holds a CountdownEvent; readers start first, writer fires last.
    //   FrozenDictionary is excluded — it is immutable by design.
    //   The writer replaces existing keys only (same key set), so Hashtable
    //   stays thread-safe per its documented contract.
    // ════════════════════════════════════════════════════════════════════════

    [Benchmark(Description = "SWMR – ConcurrentDictionary", Baseline = true)]
    [BenchmarkCategory("SWMR")]
    public void SWMR_ConcurrentDictionary() {
        RunSingleWriterMultipleReaders(
            readerOp: i => {
                string key = _keys[i % KeyCount];
                _concurrent.TryGetValue(key, out _);
            },
            writerOp: i => {
                string key   = _keys[i % KeyCount];
                string value = _values[(i + 1) % KeyCount];
                _concurrent[key] = value;
            }
        );
    }

    [Benchmark(Description = "SWMR – Hashtable")]
    [BenchmarkCategory("SWMR")]
    public void SWMR_Hashtable() {
        RunSingleWriterMultipleReaders(
            readerOp: i => {
                string key = _keys[i % KeyCount];
                _ = _hashtable[key];
            },
            writerOp: i => {
                string key   = _keys[i % KeyCount];
                string value = _values[(i + 1) % KeyCount];
                _hashtable[key] = value; // unsafe without external lock, done intentionally to measure raw perf
            }
        );
    }

    [Benchmark(Description = "SWMR – lock+Dictionary")]
    [BenchmarkCategory("SWMR")]
    public void SWMR_LockDictionary() {
        object syncRoot = _dictionary;
        RunSingleWriterMultipleReaders(
            readerOp: i => {
                string key = _keys[i % KeyCount];
                lock (syncRoot) {
                    _dictionary.TryGetValue(key, out _);
                }
            },
            writerOp: i => {
                string key   = _keys[i % KeyCount];
                string value = _values[(i + 1) % KeyCount];
                lock (syncRoot) {
                    _dictionary[key] = value;
                }
            }
        );
    }

    [Benchmark(Description = "SWMR – SynchronizedDictionary")]
    [BenchmarkCategory("SWMR")]
    public void SWMR_SynchronizedDictionary() {
        RunSingleWriterMultipleReaders(
            readerOp: i => {
                string key = _keys[i % KeyCount];
                _synchronized.TryGetValue(key, out _);
            },
            writerOp: i => {
                string key   = _keys[i % KeyCount];
                string value = _values[(i + 1) % KeyCount];
                _synchronized[key] = value;
            }
        );
    }

    [Benchmark(Description = "SWMR – HashSet")]
    [BenchmarkCategory("SWMR")]
    public void SWMR_HashSet() {
        RunSingleWriterMultipleReaders(
            readerOp: i => {
                string key = _keys[i % KeyCount];
                _hashSet.Contains(key);
            },
            writerOp: i => {
                string key   = _keys[i % KeyCount];
                string _ = _values[(i + 1) % KeyCount];
                _hashSet.Add(key);
            }
        );
    }


    // ════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Spawns <see cref="ReaderCount"/> tasks, each performing <see cref="ReaderOpsPerThread"/> operations,
    /// and waits for all of them to complete.
    /// </summary>
    private void RunReaders(Action<int> readerOp) {
        var tasks = new Task[ReaderCount];
        for (int t = 0; t < ReaderCount; t++) {
            tasks[t] = Task.Run(() => {
                for (int i = 0; i < ReaderOpsPerThread; i++)
                    readerOp(i);
            });
        }
        Task.WaitAll(tasks);
    }

    /// <summary>
    /// Runs one writer task and <see cref="ReaderCount"/> reader tasks concurrently.
    /// A <see cref="CountdownEvent"/> is used so that all threads start executing
    /// their hot loop at the same time, minimising ramp-up skew.
    /// </summary>
    private void RunSingleWriterMultipleReaders(Action<int> readerOp, Action<int> writerOp) {
        int totalThreads = ReaderCount + 1; // readers + 1 writer
        using var gate = new CountdownEvent(totalThreads);

        var tasks = new Task[totalThreads];

        // Reader tasks
        for (int t = 0; t < ReaderCount; t++) {
            tasks[t] = Task.Run(() => {
                gate.Signal();
                gate.Wait();
                for (int i = 0; i < ReaderOpsPerThread; i++)
                    readerOp(i);
            });
        }

        // Writer task
        tasks[ReaderCount] = Task.Run(() => {
            gate.Signal();
            gate.Wait();
            for (int i = 0; i < WriterOps; i++)
                writerOp(i);
        });

        Task.WaitAll(tasks);
    }
}
