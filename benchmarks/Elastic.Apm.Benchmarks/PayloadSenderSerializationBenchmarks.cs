// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Tests.Utilities;

namespace Elastic.Apm.Benchmarks;

/// <summary>
/// Measures the allocation cost of the serialization buffer strategy used by
/// <see cref="Elastic.Apm.Report.PayloadSenderV2"/> when flushing a batch of APM events.
///
/// <para>
/// <b>Old approach</b>: <c>new MemoryStream(1024)</c> allocated on every batch.
/// As the stream grows it doubles its internal <c>byte[]</c>, producing a chain of
/// short-lived arrays.  For batches larger than 85 KB the final array lands in the
/// Large Object Heap (LOH), which is not compacted during ordinary GC cycles.
/// Over a long-running deployment this causes LOH fragmentation and sustained
/// process-level memory growth.
/// </para>
///
/// <para>
/// <b>New approach</b>: a single <c>MemoryStream</c> is allocated once and reset with
/// <c>SetLength(0)</c> before each batch.  The internal buffer is reused, so no new
/// heap objects are created per batch.
/// </para>
///
/// <para>
/// Key metrics to watch in the results:
/// <list type="bullet">
///   <item><b>Allocated</b> – bytes per operation.
///         OldApproach ≈ serialised-payload-size (plus MemoryStream overhead).
///         NewApproach ≈ 0 (no buffer allocation).</item>
///   <item><b>Gen0 / Gen1 / Gen2</b> – GC pressure.
///         For <c>SpanCount = 50</c> the old approach generates Gen2 collections
///         (LOH objects); the new approach should show none.</item>
/// </list>
/// </para>
/// </summary>
[MemoryDiagnoser]
public class PayloadSenderSerializationBenchmarks
{
	private static readonly UTF8Encoding Utf8Encoding = new(encoderShouldEmitUTF8Identifier: false);

	private PayloadItemSerializer _serializer;
	private string _cachedMetadataLine;
	private object[] _batch;
	private MemoryStream _reusableBuffer;

	/// <summary>
	/// Number of spans captured per transaction.
	/// <list type="bullet">
	///   <item><b>5</b>  – small batch, well under the 85 KB LOH threshold.</item>
	///   <item><b>20</b> – medium batch, may approach the threshold.</item>
	///   <item><b>50</b> – large batch that comfortably exceeds the threshold;
	///                     simulates high-throughput production load.</item>
	/// </list>
	/// </summary>
	[Params(5, 20, 50)]
	public int SpanCount { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_serializer = new PayloadItemSerializer();
		_reusableBuffer = new MemoryStream(4096);

		var logger = new NoopLogger();
		var config = new MockConfiguration(logger);
		var service = Service.GetDefaultService(config, logger);
		var process = ProcessInformation.Create();
		var metadata = new Metadata { Service = service, System = new Api.System(), Process = process };
		_cachedMetadataLine = _serializer.Serialize(metadata);

		// Capture real agent objects so the serialization exercises a fully-populated
		// payload — the same data that PayloadSenderV2.ProcessQueueItems would handle.
		var mockSender = new MockPayloadSender();
		using var agent = new ApmAgent(new AgentComponents(
			payloadSender: mockSender,
			configurationReader: config,
			logger: logger));

		agent.Tracer.CaptureTransaction("BenchmarkTransaction", "benchmark", t =>
		{
			for (var i = 0; i < SpanCount; i++)
			{
				t.CaptureSpan($"SELECT * FROM BenchTable{i}", ApiConstants.TypeDb,
					() => { }, ApiConstants.SubtypeMssql);
			}
		});

		// MockPayloadSender stores items synchronously, so all items are ready here.
		var items = new List<object>();
		if (mockSender.FirstTransaction != null)
			items.Add(mockSender.FirstTransaction);
		foreach (var span in mockSender.Spans)
			items.Add(span);

		_batch = items.ToArray();
	}

	[GlobalCleanup]
	public void Cleanup() => _reusableBuffer.Dispose();

	/// <summary>
	/// Original implementation: allocates a new <see cref="MemoryStream"/> for every batch.
	/// </summary>
	[Benchmark(Baseline = true, Description = "Old: new MemoryStream(1024) per batch")]
	public long OldApproach()
	{
		using var stream = new MemoryStream(1024);
		SerializeBatch(stream);
		return stream.Length; // consumed to prevent dead-code elimination
	}

	/// <summary>
	/// Optimised implementation: reuses a single <see cref="MemoryStream"/>.
	/// <c>SetLength(0)</c> resets both the logical length and the write cursor
	/// without releasing the underlying buffer.
	/// </summary>
	[Benchmark(Description = "New: reuse MemoryStream via SetLength(0)")]
	public long NewApproach()
	{
		_reusableBuffer.SetLength(0);
		SerializeBatch(_reusableBuffer);
		return _reusableBuffer.Length;
	}

	private void SerializeBatch(MemoryStream stream)
	{
		// Mirror the exact structure written by PayloadSenderV2.ProcessQueueItems.
		using var writer = new StreamWriter(stream, Utf8Encoding, bufferSize: 1024, leaveOpen: true);

		writer.Write("{\"metadata\":");
		writer.Write(_cachedMetadataLine);
		writer.Write("}\n");

		foreach (var item in _batch)
		{
			var eventType = item switch
			{
				Transaction => "transaction",
				Span        => "span",
				Error       => "error",
				MetricSet   => "metricset",
				_           => null
			};

			if (eventType is null)
				continue;

			writer.Write("{\"");
			writer.Write(eventType);
			writer.Write("\":");
			writer.Write(_serializer.Serialize(item));
			writer.Write("}\n");
		}
	}
}
