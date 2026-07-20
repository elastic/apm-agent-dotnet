// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Model;

namespace Elastic.Apm.Benchmarks;

/// <summary>
/// Compares the old and new strategies for updating <c>Transaction.SpanTimings</c>
/// on every <c>Span.End()</c>.
/// <para>
/// <b>Old approach</b>: <c>ContainsKey(new SpanTimerKey(...))</c> then either
/// <c>[new SpanTimerKey(...)].IncrementTimer()</c> or <c>TryAdd(new SpanTimerKey(...), ...)</c>.
/// Creates 2–3 <c>SpanTimerKey</c> struct values and performs 2 dictionary lookups per span.
/// </para>
/// <para>
/// <b>New approach</b>: build the key once, then use <c>TryGetValue</c> for a single
/// lookup on the hot path (existing key), with <c>TryAdd</c> only on a miss.
/// 1 key allocation and 1 lookup in steady state.
/// </para>
/// </summary>
[MemoryDiagnoser]
public class SpanTimingsBenchmarks
{
	private ConcurrentDictionary<SpanTimerKey, SpanTimer> _timings;

	/// <summary>
	/// Number of update calls — simulates <see cref="SpanCount"/> spans of the
	/// same type ending sequentially (the steady-state production scenario).
	/// </summary>
	[Params(10, 50, 100)]
	public int SpanCount { get; set; }

	[GlobalSetup]
	public void Setup() => _timings = new ConcurrentDictionary<SpanTimerKey, SpanTimer>();

	[IterationSetup]
	public void IterationSetup() => _timings.Clear();

	/// <summary>
	/// Original implementation: 2–3 key allocations, 2 dictionary lookups per call.
	/// </summary>
	[Benchmark(Baseline = true, Description = "Old: ContainsKey + indexer/TryAdd (2-3 key allocs, 2 lookups)")]
	public void OldApproach()
	{
		const string type = "db";
		const string subtype = "mssql";
		const double duration = 1.5;

		for (var i = 0; i < SpanCount; i++)
		{
			if (_timings.ContainsKey(new SpanTimerKey(type, subtype)))
				_timings[new SpanTimerKey(type, subtype)].IncrementTimer(duration);
			else
				_timings.TryAdd(new SpanTimerKey(type, subtype), new SpanTimer(duration));
		}
	}

	/// <summary>
	/// Optimized implementation: 1 key construction, 1 lookup on hit (steady state).
	/// </summary>
	[Benchmark(Description = "New: TryGetValue + TryAdd (1 key construction, 1 lookup on hit)")]
	public void NewApproach()
	{
		const string type = "db";
		const string subtype = "mssql";
		const double duration = 1.5;

		for (var i = 0; i < SpanCount; i++)
		{
			var key = new SpanTimerKey(type, subtype);
			if (_timings.TryGetValue(key, out var timer))
				timer.IncrementTimer(duration);
			else
				_timings.TryAdd(key, new SpanTimer(duration));
		}
	}
}
