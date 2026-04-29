// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET // FrozenDictionary is .NET 8+ only; net462 build skips this file.

using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Microsoft.Scripting.Actions;

namespace Microsoft.Dynamic.Benchmarks;

/// <summary>
/// Reading-speed benchmarks for the dictionary built by Microsoft.Scripting.Actions.OperatorInfo.MakeOperatorTable.
/// </summary>
///
/// <remarks>
/// On .NET 8+ the production type is FrozenDictionary&lt;ExpressionType, OperatorInfo&gt;.
/// OperatorInfo is internal to Microsoft.Dynamic and MakeOperatorTable is internal as well; both
/// are reached directly through the InternalsVisibleTo grant declared in Microsoft.Dynamic's
/// AssemblyInfo. A Dictionary&lt;ExpressionType, OperatorInfo&gt; with the same content is built for
/// comparison. Both sides are accessed as their concrete generic types in the hot path.
/// </remarks>

[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class OperatorBenchmarks {
    // ── collections under test ──────────────────────────────────────────────

    private FrozenDictionary<ExpressionType, OperatorInfo> _frozen     = null!;
    private Dictionary<ExpressionType, OperatorInfo> _dictionary       = null!;
    private KeyValuePair<ExpressionType, OperatorInfo>[] _array        = null!;

    // Cached key array for the all-keys scan (avoids enumerator allocation in the hot loop).
    private ExpressionType[] _keys = null!;

    // A binary ExpressionType that is intentionally not present in the operator table.
    private const ExpressionType MissingKey = ExpressionType.Block;

    // A binary ExpressionType that is always present in the operator table.
    private const ExpressionType ExistingKey = ExpressionType.Add;

    // ── setup ───────────────────────────────────────────────────────────────

    [GlobalSetup]
    public void Setup() {

        // Using IDictionary so the benchmark is independent of the concrete type returned by MakeOperatorTable
        var entries = (IDictionary)OperatorInfo.MakeOperatorTable();

        _dictionary = new Dictionary<ExpressionType, OperatorInfo>(entries.Count);
        foreach (DictionaryEntry entry in entries) {
            _dictionary[(ExpressionType)entry.Key] = (OperatorInfo)entry.Value!;
        }

        _frozen = _dictionary.ToFrozenDictionary();

        _array = _dictionary.ToArray();

        _keys = _dictionary.Keys.ToArray();

        if (_frozen.ContainsKey(MissingKey))
            throw new InvalidOperationException($"Sanity check failed: {MissingKey} is unexpectedly present.");
        if (!_frozen.ContainsKey(ExistingKey))
            throw new InvalidOperationException($"Sanity check failed: {ExistingKey} is unexpectedly absent.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Scenario 1 — single-key lookup (key present)
    // ════════════════════════════════════════════════════════════════════════

    [Benchmark(Description = "Hit – Dictionary", Baseline = true)]
    [BenchmarkCategory("Operator-Hit")]
    public object Hit_Dictionary() {
        _dictionary.TryGetValue(ExistingKey, out OperatorInfo? value);
        return value!;
    }

    [Benchmark(Description = "Hit – FrozenDictionary")]
    [BenchmarkCategory("Operator-Hit")]
    public object Hit_FrozenDictionary() {
        _frozen.TryGetValue(ExistingKey, out OperatorInfo? value);
        return value!;
    }

    [Benchmark(Description = "Hit – Array")]
    [BenchmarkCategory("Operator-Hit")]
    public object Hit_Array() {
        var array = _array;
        for (int i = 0; i < array.Length; i++) {
            if (array[i].Key == ExistingKey) return array[i].Value;
        }
        return null!;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Scenario 2 — single-key lookup (key absent)
    // ════════════════════════════════════════════════════════════════════════

    [Benchmark(Description = "Miss – Dictionary", Baseline = true)]
    [BenchmarkCategory("Operator-Miss")]
    public bool Miss_Dictionary() {
        return _dictionary.TryGetValue(MissingKey, out _);
    }

    [Benchmark(Description = "Miss – FrozenDictionary")]
    [BenchmarkCategory("Operator-Miss")]
    public bool Miss_FrozenDictionary() {
        return _frozen.TryGetValue(MissingKey, out _);
    }

    [Benchmark(Description = "Miss – Array")]
    [BenchmarkCategory("Operator-Miss")]
    public bool Miss_Array() {
        var array = _array;
        for (int i = 0; i < array.Length; i++) {
            if (array[i].Key == MissingKey) return true;
        }
        return false;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Scenario 3 — sweep over every key in the table
    // ════════════════════════════════════════════════════════════════════════

    [Benchmark(Description = "All – Dictionary", Baseline = true)]
    [BenchmarkCategory("Operator-All")]
    public int All_Dictionary() {
        int hits = 0;
        var keys = _keys;
        for (int i = 0; i < keys.Length; i++) {
            if (_dictionary.TryGetValue(keys[i], out _)) hits++;
        }
        return hits;
    }

    [Benchmark(Description = "All – FrozenDictionary")]
    [BenchmarkCategory("Operator-All")]
    public int All_FrozenDictionary() {
        int hits = 0;
        var keys = _keys;
        for (int i = 0; i < keys.Length; i++) {
            if (_frozen.TryGetValue(keys[i], out _)) hits++;
        }
        return hits;
    }

    [Benchmark(Description = "All – Array")]
    [BenchmarkCategory("Operator-All")]
    public int All_Array() {
        int hits = 0;
        var array = _array;
        var keys = _keys;
        for (int k = 0; k < keys.Length; k++) {
            ExpressionType key = keys[k];
            for (int i = 0; i < array.Length; i++) {
                if (array[i].Key == key) {
                    hits++;
                    break;
                }
            }
        }
        return hits;
    }
}

#endif
