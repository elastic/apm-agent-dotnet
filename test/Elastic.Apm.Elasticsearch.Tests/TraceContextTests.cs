// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Tests.Utilities;
using Elasticsearch.Net;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public class TraceContextTests
	{
		[Fact]
		public void Call_to_Elasticsearch_propagates_Trace_Context_when_HttpDiagnosticsSubscriber_subscribed()
		{
			using var localServer = LocalServer.Create(context =>
			{
				var traceparent = context.Request.Headers.Get("traceparent");
				traceparent.Should().NotBeNullOrEmpty();

				var elasticTraceparent = context.Request.Headers.Get("elastic-apm-traceparent");
				elasticTraceparent.Should().NotBeNullOrEmpty().And.Be(traceparent);

				var tracestate = context.Request.Headers.Get("tracestate");
				tracestate.Should().NotBeNullOrEmpty().And.Contain("es=s:1");

				context.Response.StatusCode = 200;
			});

			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: new MockPayloadSender(), configuration: new MockConfiguration(exitSpanMinDuration:"0", spanCompressionEnabled: "false")));
			using var subscribe = agent.Subscribe(new ElasticsearchDiagnosticsSubscriber(), new HttpDiagnosticsSubscriber());

			var client = new ElasticLowLevelClient(new ConnectionConfiguration(new Uri(localServer.Uri)));
			agent.Tracer.CaptureTransaction("Transaction", ApiConstants.TypeDb, t =>
			{
				var response = client.Cat.Indices<StringResponse>();
			});
		}
	}
}
