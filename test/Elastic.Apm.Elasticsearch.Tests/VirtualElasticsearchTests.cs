using System;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using Elasticsearch.Net;
using Elasticsearch.Net.VirtualizedCluster;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Error = Elastic.Apm.Model.Error;

namespace Elastic.Apm.Elasticsearch.Tests
{
	/// <summary>
	/// Tests the Elasticsearch APM integration against a virtual cluster that can be configured to (mis)behave
	/// </summary>
	public class VirtualElasticsearchTests
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public VirtualElasticsearchTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

		[Fact]
		public async Task FailOverResultsInSpans()
		{
			var payloadSender = new MockPayloadSender();
			var cluster = VirtualClusterWith.Nodes(10)
				.ClientCalls(c => c.FailAlways())
				.ClientCalls(r => r.OnPort(9209).SucceedAlways())
				.StaticConnectionPool()
				.AllDefaults();
			var client = cluster.Client;
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
					   configuration: new MockConfiguration(exitSpanMinDuration: "0", spanCompressionEnabled: "false"))))
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
				var pings = spans.Where(s => s.Action == "Ping");
				pings.Should().HaveCount(10);
				spans.Where(s => s.Action == "CallElasticsearch").Should().HaveCount(10);

				spans.Should().OnlyContain(s => s.Context != null);
				spans.Should().OnlyContain(s => s.Context.Db != null);
				spans.Should().OnlyContain(s => s.Context.Db.Statement != null);

				spans.First(n => n.Subtype == ApiConstants.SubtypeElasticsearch).Context.Destination.Should().NotBeNull();
				spans.First(n => n.Subtype == ApiConstants.SubtypeElasticsearch).Context.Destination.Address.Should().Be("localhost");
				spans.First(n => n.Subtype == ApiConstants.SubtypeElasticsearch).Context.Destination.Port.Should().Be(9200);

				spans.First(n => n.Subtype == ApiConstants.SubtypeElasticsearch).Context.Destination.Service.Should().NotBeNull();
				spans.First(n => n.Subtype == ApiConstants.SubtypeElasticsearch)
					.Context.Destination.Service.Resource.Should()
					.Be(ApiConstants.SubtypeElasticsearch);

				spans.First(n => n.Subtype == ApiConstants.SubtypeElasticsearch).Outcome.Should().Be(Outcome.Success);
			}
		}

		[Fact]
		public async Task ExceptionDoesNotCauseLoseOfSpan()
		{
			var payloadSender = new MockPayloadSender();
			var cluster = VirtualClusterWith.Nodes(2)
				.ClientCalls(r => r.OnPort(9200).FailAlways())
				.ClientCalls(c => c.OnPort(9201).FailAlways(new Exception("boom!")))
				.StaticConnectionPool()
				.AllDefaults();
			var client = cluster.Client;
			using (var agent = new ApmAgent(new TestAgentComponents(logger: new XUnitLogger(LogLevel.Trace, _testOutputHelper),
					   payloadSender: payloadSender,
					   configuration: new MockConfiguration(exitSpanMinDuration: "0", spanCompressionEnabled: "false"))))
			using (agent.Subscribe(new ElasticsearchDiagnosticsSubscriber()))
			{
				try
				{
					var searchResponse = await agent.Tracer.CaptureTransaction("Call Client", ApiConstants.ActionExec,
						async () => await client.SearchAsync<StringResponse>(PostData.Empty)
					);
					searchResponse.Should().NotBeNull();
				}
				catch (Exception)
				{
					// ignored
				}

				var spans = payloadSender.SpansOnFirstTransaction;
				spans.Should().NotBeEmpty();
				spans.Should().Contain(s => s.Context.Db.Statement != null);
				//ensure we the last span is closed even if the listener does not receive a response
				spans.Where(s => s.Action == "Ping").Should().HaveCount(2);
				spans.Where(s => s.Action == "Ping").All(n => n.Outcome == Outcome.Success).Should().BeTrue();

				spans.Where(s => s.Action == "CallElasticsearch").Should().HaveCount(2);
				spans.Where(s => s.Action == "CallElasticsearch").All(n => n.Outcome == Outcome.Failure).Should().BeTrue();

				payloadSender.Errors.Should().Contain(e => ((Error)e).Exception.Message == "boom!");
			}
		}

		[Fact]
		public async Task ElasticsearchClientExceptionIsReported()
		{
			var payloadSender = new MockPayloadSender();
			var cluster = VirtualClusterWith.Nodes(1)
				.ClientCalls(r => r.OnPort(9200).FailAlways())
				.StaticConnectionPool()
				.Settings(s => s.DisablePing());
			var client = cluster.Client;
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
					   configuration: new MockConfiguration(exitSpanMinDuration: "0", spanCompressionEnabled: "false"))))
			using (agent.Subscribe(new ElasticsearchDiagnosticsSubscriber()))
			{
				var searchResponse = await agent.Tracer.CaptureTransaction("Call Client", ApiConstants.ActionExec,
					async () => await client.SearchAsync<StringResponse>(PostData.Empty)
				);
				searchResponse.Should().NotBeNull();

				var spans = payloadSender.SpansOnFirstTransaction;
				var span = spans.Should().NotBeEmpty().And.HaveCount(1).And.Subject.First();
				span.Outcome.Should().Be(Outcome.Failure);
				var error = (Error)payloadSender.Errors.Should().HaveCount(1).And.Subject.First();

				error.Culprit.Should().StartWith("Elasticsearch.Net.VirtualizedCluster.VirtualClusterConnection");
				error.Exception.Should().NotBeNull();
				error.Exception.Message.Should().Contain("System.Net.Http.HttpRequestException");
				error.Exception.StackTrace.Should().NotBeEmpty();
			}
		}

		[Fact]
		public async Task ServerErrorIsReported()
		{
			var payloadSender = new MockPayloadSender();
			var cluster = VirtualClusterWith.Nodes(1)
				.ClientCalls(r => r
					.FailAlways(500)
					.ReturnResponse(new
					{
						error = new
						{
							root_cause =
								new object[]
								{
									new
									{
										type = "script_exception",
										reason = "runtime error",
										script_stack = new string[] { },
										script = "ctx._source",
										lang = "painless"
									}
								},
							type = "script_exception",
							reason = "runtime error",
							script_stack = new string[] { },
							script = "ctx._source",
							lang = "painless",
							caused_by = new { type = "null_pointer_exception", reason = (object)null }
						},
						status = 500
					})
				)
				.StaticConnectionPool()
				.Settings(s => s.DisablePing());
			var client = cluster.Client;
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			using (agent.Subscribe(new ElasticsearchDiagnosticsSubscriber()))
			{
				var searchResponse = await agent.Tracer.CaptureTransaction("Call Client", ApiConstants.ActionExec,
					async () => await client.SearchAsync<StringResponse>(PostData.Empty)
				);
				searchResponse.Should().NotBeNull();

				var spans = payloadSender.SpansOnFirstTransaction;
				var span = spans.Should().NotBeEmpty().And.HaveCount(1).And.Subject.First();
				span.Outcome.Should().Be(Outcome.Failure);
				var error = (Error)payloadSender.Errors.Should().HaveCount(1).And.Subject.First();

				error.Culprit.Should().StartWith("Elasticsearch Server Error: script_exception");
				error.Exception.Should().NotBeNull();
				error.Exception.Message.Should().Contain("null_pointer_exception");
				error.Exception.Message.Should().Contain("(500)");
			}
		}
	}
}
