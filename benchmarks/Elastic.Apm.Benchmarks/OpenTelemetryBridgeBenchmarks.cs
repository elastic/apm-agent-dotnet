// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.OpenTelemetry;
using Elastic.Apm.Tests.Utilities;

namespace Elastic.Apm.Benchmarks;

[MemoryDiagnoser]
public class OpenTelemetryBridgeBenchmarks
{
	private Activity _activity;
	private Span _span;

	[GlobalSetup]
	public void Setup()
	{
		var noopLogger = new NoopLogger();

		var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: noopLogger,
			configurationReader: new MockConfiguration(noopLogger)));

		_activity = new Activity("cluster.health");
		_activity.Start();
		_activity.SetTag(SemanticConventions.HttpRequestMethod, "GET");
		_activity.SetTag(SemanticConventions.UrlFull, "https://localhost:9200/_cluster/health");
		_activity.SetTag(SemanticConventions.ServerAddress, "localhost");
		_activity.SetTag(SemanticConventions.ServerPort, 9200);
		_activity.SetTag(SemanticConventions.DbSystem, "elasticsearch");
		_activity.SetTag("db.operation", "cluster.health");
		_activity.SetTag("db.elasticsearch.route.template", "_cluster/health");		

		var transaction = ((Tracer)agent.Tracer).StartTransactionInternal(_activity.DisplayName, "unknown",
						TimeUtils.ToTimestamp(_activity.StartTimeUtc), true, _activity.SpanId.ToString());

		_span = transaction?.StartSpanInternal(_activity.DisplayName, "unknown",
							timestamp: TimeUtils.ToTimestamp(_activity.StartTimeUtc), id: _activity.SpanId.ToString());

		_span.Otel = new OTel { SpanKind = _activity.Kind.ToString() };

		if (_activity.Kind == ActivityKind.Internal)
		{
			_span.Type = "app";
			_span.Subtype = "internal";
		}

		_activity.SetTag(SemanticConventions.HttpResponseStatusCode, 200);
		_activity.SetStatus(ActivityStatusCode.Ok);
		_activity.Stop();
	}

	[Benchmark]
	public void UpdateSpan() => ElasticActivityListener.UpdateSpanBenchmark(_activity, _span);
}

