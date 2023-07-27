// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using Elastic.Apm.Tests.Utilities.XUnit;
using Elasticsearch.Net;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public class ElasticsearchTests : IClassFixture<ElasticsearchFixture>
	{
		private readonly ElasticLowLevelClient _client;

		public ElasticsearchTests(ElasticsearchFixture fixture)
		{
			var settings = new ConnectionConfiguration(new Uri(fixture.ConnectionString));
			settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

			_client = new ElasticLowLevelClient(settings);
		}

		[DockerFact]
		public async Task Elasticsearch_Span_Should_Align_With_Spec()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, configuration: new MockConfiguration(exitSpanMinDuration: "0", spanCompressionEnabled: "false"))))
			using (agent.Subscribe(new ElasticsearchDiagnosticsSubscriber()))
			{
				var searchResponse = await agent.Tracer.CaptureTransaction("Call Client", ApiConstants.ActionExec,
					async () => await _client.SearchAsync<StringResponse>(PostData.Empty, new SearchRequestParameters { TrackTotalHits = true }));

				searchResponse.Should().NotBeNull();
				searchResponse.Success.Should().BeTrue();
				searchResponse.AuditTrail.Should().NotBeEmpty();

				var spans = payloadSender.SpansOnFirstTransaction;

				var parentSpan = spans.Where(s => s.ParentId == s.TransactionId).Single();

				parentSpan.Name.Should().StartWith("Elasticsearch: ");
				parentSpan.Action.Should().Be("request");

				parentSpan.Context.Db.Should().NotBeNull();
				parentSpan.Context.Db.Type.Should().Be(ApiConstants.SubtypeElasticsearch);
				parentSpan.Context.Db.Instance.Should().BeNull(); // For v7, we don't have a way to retrieve the cluster name
				parentSpan.Context.Db.Statement.Should().NotBeNullOrEmpty();

				parentSpan.Context.Service.Target.Should().NotBeNull();
				parentSpan.Context.Service.Target.Type.Should().Be(ApiConstants.SubtypeElasticsearch);
				parentSpan.Context.Service.Target.Name.Should().BeNull(); // For v7, we don't have a way to retrieve the cluster name

				// The spec requires the full (username redacted) URL on the parent span
				parentSpan.Context.Http.Url.Should().MatchRegex("^https:\\/\\/\\[REDACTED]:\\[REDACTED]@127\\.0\\.0\\.1:[0-9]+\\/_search\\?track_total_hits=true$");

				AssertSpan(parentSpan);

				var childSpans = spans.Where(s => s.ParentId == parentSpan.Id).ToList();
				childSpans.Should().HaveCount(2); // In this configuration we expect two sub-spans.

				foreach (var childSpan in childSpans)
					AssertSpan(childSpan);
			}
		}

		private static void AssertSpan(ISpan span)
		{
			span.Type.Should().Be(ApiConstants.TypeDb);
			span.Subtype.Should().Be(ApiConstants.SubtypeElasticsearch);

			span.Context.Http.Should().NotBeNull();
			span.Context.Http.StatusCode.Should().Be(200);
			span.Context.Http.Method.Should().Be("POST");
			span.Context.Http.Url.Should().EndWith("/_search?track_total_hits=true");

			span.Context.Destination.Should().NotBeNull();
			span.Context.Destination.Address.Should().NotBeNullOrEmpty();
			span.Context.Destination.Port.Should().BeGreaterThan(0).And.BeLessThan(65536);
		}

		[DockerFact]
		public async Task Elasticsearch_Span_Does_Not_Have_Http_Child_Span()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, configuration: new MockConfiguration(exitSpanMinDuration: "0", spanCompressionEnabled: "false"))))
			using (agent.Subscribe(new ElasticsearchDiagnosticsSubscriber(), new HttpDiagnosticsSubscriber()))
			{
				var searchResponse = await agent.Tracer.CaptureTransaction("Call Client", ApiConstants.ActionExec,
					async () => await _client.SearchAsync<StringResponse>(PostData.Empty)
				);
				searchResponse.Should().NotBeNull();
				searchResponse.Success.Should().BeTrue();
				searchResponse.AuditTrail.Should().NotBeEmpty();

				var spans = payloadSender.SpansOnFirstTransaction;
				spans.Should().NotBeEmpty().And.NotContain(s => s.Subtype == ApiConstants.SubtypeHttp);
			}
		}
	}
}
