using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.AspNetCore.Tests.Services;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")]
	public class DistributedTracingAspNetCoreTests : IAsyncLifetime, IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;

		private readonly ApmAgent _agent1 = new ApmAgent(new TestAgentComponents(payloadSender: new MockPayloadSender()));
		private readonly ApmAgent _agent2 = new ApmAgent(new TestAgentComponents(payloadSender: new MockPayloadSender()));

		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private Task _webApiTask;

		public DistributedTracingAspNetCoreTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		public Task InitializeAsync()
		{
			_webApiTask = WebHost.CreateDefaultBuilder()
				.ConfigureTestServices(services =>
				{
					services.AddSingleton(new StartupConfigService(_agent2));
					services.AddMvc().AddApplicationPart(typeof(WebApiSample.Startup).Assembly);
				})
				.UseUrls("http://localhost:5050")
				.UseStartup<FakeWebApiSampleStartup>()
				.Build()
				.RunAsync(_cancellationTokenSource.Token);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Distributed tracing integration test.
		/// It starts <see cref="AspNetCoreSampleApp" /> with one agent and <see cref="WebApiSample" /> with another agent.
		/// It calls the 'DistributedTracingMiniSample' in <see cref="AspNetCoreSampleApp" />, which generates a simple HTTP
		/// response by calling <see cref="WebApiSample" /> via HTTP.
		/// Makes sure that all spans and transactions across the 2 services have the same trace ID.
		/// </summary>
		[Fact]
		public async Task DistributedTraceAcross2Service()
		{
			await ExecuteAndCheckDistributedCall();

			var capturedPayload1 = (MockPayloadSender)_agent1.PayloadSender;
			var capturedPayload2 = (MockPayloadSender)_agent2.PayloadSender;

			capturedPayload1.FirstTransaction.IsSampled.Should().BeTrue();
			capturedPayload2.FirstTransaction.IsSampled.Should().BeTrue();

			// Make sure all spans have the same trace ID.
			capturedPayload1.Spans.Should().NotContain(n => n.TraceId != capturedPayload1.FirstTransaction.TraceId);
			capturedPayload2.Spans.Should().NotContain(n => n.TraceId != capturedPayload1.FirstTransaction.TraceId);

			// Make sure the parent of the second transaction is the span ID of the outgoing HTTP request from the first transaction.
			capturedPayload2.FirstTransaction.ParentId.Should().Be(capturedPayload1.Spans.First(n => n.Context.Http.Url.Contains("api/values")).Id);
		}

		/// <summary>
		/// The same as <see cref="DistributedTraceAcross2Service" /> except that the first agent is configured with sampling rate 0
		/// (to non-sample all the transactions) causing both transactions to be non-sampled (since is-sampled decision is passed
		/// via distributed tracing to downstream agents).
		/// Makes sure that both transactions are non-sampled so there should be no spans and parent ID passed to the downstream
		/// agent (is transaction ID, not span ID as it was in sampled case).
		/// </summary>
		[Fact]
		public async Task NonSampledDistributedTraceAcross2Service()
		{
			_agent1.TracerInternal.Sampler = new Sampler(0);

			await ExecuteAndCheckDistributedCall();

			var capturedPayload1 = (MockPayloadSender)_agent1.PayloadSender;
			var capturedPayload2 = (MockPayloadSender)_agent2.PayloadSender;

			// Since the trace is non-sampled both transactions should me marked as not sampled.
			capturedPayload1.FirstTransaction.IsSampled.Should().BeFalse();
			capturedPayload2.FirstTransaction.IsSampled.Should().BeFalse();

			// Since the trace is not sampled there should NOT be any spans.
			capturedPayload1.Spans.Should().BeEmpty();
			capturedPayload2.Spans.Should().BeEmpty();

			// Since the trace is non-sampled the parent ID of the second transaction is the first transaction ID (not span ID as it was in sampled case).
			capturedPayload2.FirstTransaction.ParentId.Should().Be(capturedPayload1.FirstTransaction.Id);
		}

		public async Task DisposeAsync()
		{
			_cancellationTokenSource.Cancel();
			await Task.WhenAll(_webApiTask);

			_agent1.Dispose();
			_agent2.Dispose();
			_factory.Dispose();
		}

		private async Task ExecuteAndCheckDistributedCall()
		{
			using (var client = TestHelper.GetClient(_factory, _agent1))
			{
				var result = await client.GetAsync("/Home/DistributedTracingMiniSample");
				result.IsSuccessStatusCode.Should().BeTrue();
			}

			var capturedPayload1 = (MockPayloadSender)_agent1.PayloadSender;
			var capturedPayload2 = (MockPayloadSender)_agent2.PayloadSender;

			capturedPayload1.Transactions.Should().ContainSingle();
			capturedPayload2.Transactions.Should().ContainSingle();

			// Make sure the two transactions have the same trace ID.
			capturedPayload2.FirstTransaction.TraceId.Should().Be(capturedPayload1.FirstTransaction.TraceId);
		}
	}
}
