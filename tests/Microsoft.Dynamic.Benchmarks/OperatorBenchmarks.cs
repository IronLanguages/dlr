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
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Microsoft.Scripting.Utils;

namespace Microsoft.Dynamic.Benchmarks;

/// <summary>
/// Reading-speed benchmarks for the dictionary built by the private static method
/// Microsoft.Scripting.Actions.OperatorInfo.MakeOperatorTable.
/// </summary>
///
/// <remarks>
/// On .NET 8+ the production type is FrozenDictionary&lt;ExpressionType, OperatorInfo&gt;.
/// The method is private and OperatorInfo is internal, so the dictionary is reached via
/// reflection. IReadOnlyDictionary&lt;TKey, TValue&gt; is invariant in TValue (TryGetValue
/// exposes TValue through an out parameter), so we enumerate the result through the
/// non-generic IDictionary interface and rebuild a FrozenDictionary&lt;ExpressionType, object&gt;
/// alongside a Dictionary&lt;ExpressionType, object&gt; with the same content. Both sides are
/// then accessed as concrete generic types in the hot path.
/// </remarks>

[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class OperatorBenchmarks {
    // ── collections under test ──────────────────────────────────────────────

    private FrozenDictionary<ExpressionType, object> _frozen     = null!;
    private Dictionary<ExpressionType, object> _dictionary       = null!;

    // Cached key array for the all-keys scan (avoids enumerator allocation in the hot loop).
    private ExpressionType[] _keys = null!;

    // A binary ExpressionType that is intentionally not present in the operator table.
    private const ExpressionType MissingKey = ExpressionType.Block;

    // A binary ExpressionType that is always present in the operator table.
    private const ExpressionType ExistingKey = ExpressionType.Add;

    // ── setup ───────────────────────────────────────────────────────────────

    [GlobalSetup]
    public void Setup() {
        // Reach OperatorInfo (internal) via reflection on a public neighbour from the same assembly.
        Assembly assembly = typeof(SynchronizedDictionary<,>).Assembly;
        Type operatorInfoType = assembly.GetType("Microsoft.Scripting.Actions.OperatorInfo")
            ?? throw new InvalidOperationException("Microsoft.Scripting.Actions.OperatorInfo not found");

        MethodInfo makeMethod = operatorInfoType.GetMethod(
            "MakeOperatorTable",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("MakeOperatorTable not found");

        object table = makeMethod.Invoke(null, null)
            ?? throw new InvalidOperationException("MakeOperatorTable returned null");

        // IReadOnlyDictionary<TKey, TValue> is invariant in TValue, so we cannot cast to
        // IReadOnlyDictionary<ExpressionType, object>. FrozenDictionary<,> also implements the
        // non-generic IDictionary, which lets us enumerate without knowing the (internal)
        // OperatorInfo type at compile time.
        var entries = (IDictionary)table;

        _dictionary = new Dictionary<ExpressionType, object>(entries.Count);
        foreach (DictionaryEntry entry in entries) {
            _dictionary[(ExpressionType)entry.Key] = entry.Value!;
        }

        _frozen = _dictionary.ToFrozenDictionary();

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
        _dictionary.TryGetValue(ExistingKey, out object? value);
        return value!;
    }

    [Benchmark(Description = "Hit – FrozenDictionary")]
    [BenchmarkCategory("Operator-Hit")]
    public object Hit_FrozenDictionary() {
        _frozen.TryGetValue(ExistingKey, out object? value);
        return value!;
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
}

#endif
