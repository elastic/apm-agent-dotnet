// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Api;

namespace Elastic.Apm.Benchmarks;

[MemoryDiagnoser]
public class SpanLinkInsertBenchmarks
{
	private SpanLink[] _linksToInsert;
	private List<SpanLink> _existingLinks;

	[Params(1, 5, 10)]
	public int LinkCount { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_linksToInsert = Enumerable.Range(0, LinkCount)
			.Select(i => new SpanLink($"span{i:x16}", $"trace{i:x32}"))
			.ToArray();

		_existingLinks = new List<SpanLink>
		{
			new("aaaa000000000000", "bbbb00000000000000000000000000000001")
		};
	}

	// ── First-call path (Links == null) ─────────────────────────────────────

	/// <summary>Old Span logic — missing else causes duplicate insertion.</summary>
	[Benchmark(Baseline = true, Description = "Old: Span first-call (null Links) — duplicates items")]
	public int OldSpan_FirstCall()
	{
		// Simulates the old Span.InsertSpanLinkInternal with the missing else bug
		IEnumerable<SpanLink> links = _linksToInsert;
		IEnumerable<SpanLink> currentLinks = null;

		var spanLinks = links as SpanLink[] ?? links.ToArray();
		if (currentLinks == null || !currentLinks.Any())
			currentLinks = spanLinks;

		// falls through (missing else) — always executes
		var newList = new List<SpanLink>(currentLinks);
		newList.AddRange(spanLinks);
		currentLinks = new List<SpanLink>(newList);

		return currentLinks.Count();
	}

	/// <summary>New Span logic — early return, no duplicates, zero extra allocations.</summary>
	[Benchmark(Description = "New: Span first-call (null Links) — correct, one alloc")]
	public int NewSpan_FirstCall()
	{
		IEnumerable<SpanLink> links = _linksToInsert;
		IEnumerable<SpanLink> currentLinks = null;

		if (currentLinks == null || !currentLinks.Any())
		{
			currentLinks = links as SpanLink[] ?? links.ToArray();
			return currentLinks.Count();
		}
		var newList = new List<SpanLink>(currentLinks);
		newList.AddRange(links);
		currentLinks = newList;
		return currentLinks.Count();
	}

	// ── Append path (Links already has entries) ──────────────────────────────

	/// <summary>Old Transaction logic — correct but creates two List instances.</summary>
	[Benchmark(Description = "Old: Transaction append-call — unnecessary second List copy")]
	public int OldTransaction_AppendCall()
	{
		IEnumerable<SpanLink> links = _linksToInsert;
		IEnumerable<SpanLink> currentLinks = _existingLinks;

		var spanLinks = links as SpanLink[] ?? links.ToArray();
		if (currentLinks == null || !currentLinks.Any())
		{
			currentLinks = spanLinks;
		}
		else
		{
			var newList = new List<SpanLink>(currentLinks);
			newList.AddRange(spanLinks);
			currentLinks = new List<SpanLink>(newList); // second pointless copy
		}
		return currentLinks.Count();
	}

	/// <summary>New logic — builds exactly one List, assigns directly.</summary>
	[Benchmark(Description = "New: Transaction append-call — single List, no copy")]
	public int NewTransaction_AppendCall()
	{
		IEnumerable<SpanLink> links = _linksToInsert;
		IEnumerable<SpanLink> currentLinks = _existingLinks;

		if (currentLinks == null || !currentLinks.Any())
		{
			currentLinks = links as SpanLink[] ?? links.ToArray();
			return currentLinks.Count();
		}
		var newList = new List<SpanLink>(currentLinks);
		newList.AddRange(links);
		currentLinks = newList;
		return currentLinks.Count();
	}
}
