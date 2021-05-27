// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using Elasticsearch.Net;
using Elasticsearch.Net.VirtualizedCluster;
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
			_client = new ElasticLowLevelClient(settings);
		}

		[DockerFact]
		public async Task Elasticsearch_Span_Does_Not_Have_Http_Child_Span()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
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
