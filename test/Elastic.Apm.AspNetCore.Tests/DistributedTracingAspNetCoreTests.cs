using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")]
	public class DistributedTracingAspNetCoreTests : IAsyncLifetime
	{
		private ApmAgent _agent1;
		private ApmAgent _agent2;

		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly MockPayloadSender _payloadSender1 = new MockPayloadSender();
		private readonly MockPayloadSender _payloadSender2 = new MockPayloadSender();

		private Task _taskForApp1;
		private Task _taskForApp2;

		public Task InitializeAsync()
		{
			_agent1 = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender1));
			_agent2 = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender2));

			_taskForApp1 = Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
					{
						SampleAspNetCoreApp.Startup.ConfigureServicesExceptMvc(services);

						services.AddMvc()
							.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))))
							.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
					}
				)
				.Configure(app =>
				{
					app.UseElasticApm(_agent1, new TestLogger(),
						new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
					SampleAspNetCoreApp.Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5901")
				.Build()
				.RunAsync(_cancellationTokenSource.Token);

			_taskForApp2 = WebApiSample.Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
				{
					services.AddMvc()
						.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(WebApiSample))))
						.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
				})
				.Configure(app =>
				{
					//normally we would also subscribe to HttpDiagnosticsSubscriber and EfCoreDiagnosticsSubscriber,
					//but in this test 2 web apps run in a single process, so subscribing once is enough, and we
					//already do it above when we configure the SampleAspNetCoreApp.
					app.UseElasticApm(_agent2, new TestLogger());
					WebApiSample.Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5050")
				.Build()
				.RunAsync(_cancellationTokenSource.Token);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Distributed tracing integration test.
		/// It starts <see cref="SampleAspNetCoreApp" /> with 1 agent and <see cref="WebApiSample" /> with another agent.
		/// It calls the 'DistributedTracingMiniSample' in <see cref="SampleAspNetCoreApp" />, which generates a simple HTTP
		/// response
		/// by calling <see cref="WebApiSample" /> via HTTP.
		/// Makes sure that all spans and transactions across the 2 services have the same trace id.
		/// </summary>
		[Fact]
		public async Task DistributedTraceAcross2Service()
		{
			await ExecuteAndCheckDistributedCall();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender2.FirstTransaction.IsSampled.Should().BeTrue();

			//make sure all spans have the same traceid:
			_payloadSender1.Spans.Should().NotContain(n => n.TraceId != _payloadSender1.FirstTransaction.TraceId);
			_payloadSender2.Spans.Should().NotContain(n => n.TraceId != _payloadSender1.FirstTransaction.TraceId);

			//make sure the parent of the 2. transaction is the spanid of the outgoing HTTP request from the 1. transaction:
			_payloadSender2.FirstTransaction.ParentId.Should()
				.Be(_payloadSender1.Spans.First(n => n.Context.Http.Url.Contains("api/values")).Id);
		}

		/// <summary>
		/// The same as <see cref="DistributedTraceAcross2Service" /> except that the 1st agent configured with sampling rate 0
		/// (to non-sample all the transaction) causing both transactions to be non-sampled (since is-sampled decision is passed
		/// via distributed tracing to downstream agents).
		/// Makes sure that both transactions are non-sampled so there should be no spans and parent ID passed to the downstream
		/// agent
		/// is transaction ID (not span ID as it was in sampled case).
		/// </summary>
		[Fact]
		public async Task NonSampledDistributedTraceAcross2Service()
		{
			_agent1.TracerInternal.Sampler = new Sampler(0);

			await ExecuteAndCheckDistributedCall();

			// Since the trace is non-sampled both transactions should me marked as not sampled
			_payloadSender1.FirstTransaction.IsSampled.Should().BeFalse();
			_payloadSender2.FirstTransaction.IsSampled.Should().BeFalse();

			// Since the trace is not sampled there should NOT be any spans
			_payloadSender1.Spans.Should().BeEmpty();
			_payloadSender2.Spans.Should().BeEmpty();

			// Since the trace is non-sampled the parent ID of the 2nd transaction is the 1st transaction ID (not span ID as it was in sampled case)
			_payloadSender2.FirstTransaction.ParentId.Should().Be(_payloadSender1.FirstTransaction.Id);
		}

		public async Task DisposeAsync()
		{
			_cancellationTokenSource.Cancel();
			await Task.WhenAll(_taskForApp1, _taskForApp2);

			_agent1?.Dispose();
			_agent2?.Dispose();
		}

		private async Task ExecuteAndCheckDistributedCall()
		{
			var client = new HttpClient();
			var res = await client.GetAsync("http://localhost:5901/Home/DistributedTracingMiniSample");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.Transactions.Count.Should().Be(1);
			_payloadSender2.Transactions.Count.Should().Be(1);

			//make sure the 2 transactions have the same traceid:
			_payloadSender2.FirstTransaction.TraceId.Should().Be(_payloadSender1.FirstTransaction.TraceId);
		}
	}
}
