using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Xunit;
using Elastic.Apm.Tests.Mocks;
using Elasticsearch.Net;
using Elasticsearch.Net.VirtualizedCluster;
using FluentAssertions;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public class UnitTest1
	{
		[Fact]
		public async Task Test1()
		{
			var payloadSender = new MockPayloadSender();
			var cluster = VirtualClusterWith.Nodes(10)
				.ClientCalls(c => c.FailAlways())
				.ClientCalls(r => r.OnPort(9209).SucceedAlways())
				.StaticConnectionPool()
				.AllDefaults();
			var client = cluster.Client;
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			using (agent.Subscribe(new ElasticsearchDiagnosticsSubscriber()))
			{
				var searchResponse = await agent.Tracer.CaptureTransaction("Call Client", ApiConstants.ActionExec,
					async () => await client.SearchAsync<StringResponse>(PostData.Empty)
				);
				searchResponse.Should().NotBeNull();
				searchResponse.Success.Should().BeTrue();
				searchResponse.AuditTrail.Should().NotBeEmpty();

				var failed = searchResponse.AuditTrail.Where(a => a.Event == AuditEvent.BadResponse);
				failed.Should().HaveCount(9);

				var spans = payloadSender.SpansOnFirstTransaction;
				spans.Should().NotBeEmpty();

			}
		}
	}
}
