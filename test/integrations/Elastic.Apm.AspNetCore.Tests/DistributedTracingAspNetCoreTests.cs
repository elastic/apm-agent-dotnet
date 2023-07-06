// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")]
	public class DistributedTracingAspNetCoreTests : IAsyncLifetime
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly MockPayloadSender _payloadSender1 = new MockPayloadSender();
		private readonly MockPayloadSender _payloadSender2 = new MockPayloadSender();
		private ApmAgent _agent1;
		private ApmAgent _agent2;

		private Task _taskForApp1;
		private Task _taskForApp2;

		public Task InitializeAsync()
		{
			_agent1 = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender1,
				configuration: new MockConfiguration(exitSpanMinDuration: "0")));
			_agent2 = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender2,
				configuration: new MockConfiguration(exitSpanMinDuration: "0")));

			_taskForApp1 = Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
					{
						Startup.ConfigureServicesExceptMvc(services);

						services
							.AddMvc()
							.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))));
					}
				)
				.Configure(app =>
				{
					app.UseElasticApm(_agent1, new TestLogger(),
						new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
					Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5901")
				.Build()
				.RunAsync(_cancellationTokenSource.Token);

			_taskForApp2 = WebApiSample.Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
				{
					services.AddMvc()
						.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(WebApiSample))));
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
		/// Makes sure that in case ELASTIC_APM_USE_ELASTIC_TRACEPARENT_HEADER is set to true
		/// both "traceparent" and "elastic-apm-traceparent" HTTP headers are set
		/// </summary>
		[Fact]
		public async Task DistributedTraceAcross2ServicesWithUseElasticTraceParentTrue()
		{
			_agent1.ConfigurationStore.CurrentSnapshot = new MockConfiguration(useElasticTraceparentHeader: "true");
			await ExecuteAndCheckDistributedCall();

			_payloadSender2.FirstTransaction.Context.Request.Headers.Keys.Should().Contain(TraceContext.TraceParentHeaderName);
			_payloadSender2.FirstTransaction.Context.Request.Headers.Keys.Should().Contain(TraceContext.TraceParentHeaderNamePrefixed);
		}

		/// <summary>
		/// Makes sure that in case ELASTIC_APM_USE_ELASTIC_TRACEPARENT_HEADER is set to false
		/// only "traceparent" HTTP header is set and no "elastic-apm-traceparent" header is set
		/// </summary>
		[Fact]
		public async Task DistributedTraceAcross2ServicesWithUseElasticTraceParentFalse()
		{
			_agent1.ConfigurationStore.CurrentSnapshot = new MockConfiguration(useElasticTraceparentHeader: "false");
			await ExecuteAndCheckDistributedCall();

			_payloadSender2.FirstTransaction.Context.Request.Headers.Keys.Should().Contain(TraceContext.TraceParentHeaderName);
			_payloadSender2.FirstTransaction.Context.Request.Headers.Keys.Should().NotContain(TraceContext.TraceParentHeaderNamePrefixed);
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
			_agent1.ConfigurationStore.CurrentSnapshot = new MockConfiguration(transactionSampleRate: "0");

			await ExecuteAndCheckDistributedCall(false);

			// Since the trace is non-sampled both transactions should me marked as not sampled
			_payloadSender1.FirstTransaction.IsSampled.Should().BeFalse();
			_payloadSender2.FirstTransaction.IsSampled.Should().BeFalse();

			// Since the trace is not sampled there should NOT be any spans
			_payloadSender1.Spans.Should().BeEmpty();
			_payloadSender2.Spans.Should().BeEmpty();

			// Since the trace is non-sampled the parent ID of the 2nd transaction is the 1st transaction ID (not span ID as it was in sampled case)
			_payloadSender2.FirstTransaction.ParentId.Should().Be(_payloadSender1.FirstTransaction.Id);
		}

		/// <summary>
		/// Starts 2 services and sends a request to the 1. service with a tracestate header set
		/// Makes sure that the tracestate is available in all services
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task DistributedTraceAcross2ServicesWithTraceState()
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Add("tracestate", "rojo=00f067aa0ba902b7,congo=t61rcWkgMzE");
			var res = await client.GetAsync("http://localhost:5901/Home/DistributedTracingMiniSample");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender2.FirstTransaction.IsSampled.Should().BeTrue();

			_payloadSender1.FirstTransaction.Context.Request.Headers.Should().ContainKey("tracestate");
			_payloadSender1.FirstTransaction.Context.Request.Headers["tracestate"].Should().Be("rojo=00f067aa0ba902b7,congo=t61rcWkgMzE");

			_payloadSender2.FirstTransaction.Context.Request.Headers.Should().ContainKey("tracestate");
			_payloadSender2.FirstTransaction.Context.Request.Headers["tracestate"].Should().Be("rojo=00f067aa0ba902b7,congo=t61rcWkgMzE");
		}

		/// <summary>
		/// Makes sure that the header `traceparent` is used when both `traceparent` and `elastic-apm-traceparent` are present.
		/// </summary>
		[Fact]
		public async Task PreferW3CTraceHeaderOverElasticTraceHeader()
		{
			var client = new HttpClient();
			var expectedTraceId = "0af7651916cd43dd8448eb211c80319c";
			var expectedParentId = "b7ad6b7169203331";
			client.DefaultRequestHeaders.Add("traceparent", $"00-{expectedTraceId}-{expectedParentId}-01");
			client.DefaultRequestHeaders.Add("elastic-apm-traceparent", "00-000000000000000000000000000019c-0000000000000001-01");
			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.TraceId.Should().Be(expectedTraceId);
			_payloadSender1.FirstTransaction.ParentId.Should().Be(expectedParentId);
		}

		/// <summary>
		/// Sets the TraceContextIgnoreSampledFalse config and makes sure that the traceparent header is ignored
		/// </summary>
		[Fact]
		public async Task TraceContextIgnoreSampledFalse_WithNoTraceState()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContextIgnoreSampledFalse: "true", transactionSampleRate: "1");

			var client = new HttpClient();

			// Send traceparent with sampled=false
			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			// Assert that the transaction is sampled and the traceparent header was ignored
			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();

			// Assert that the transaction is a root transaction
			_payloadSender1.FirstTransaction.ParentId.Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Sets the TraceContextIgnoreSampledFalse config and sends a traceparent with es vendor part.
		/// Makes sure in that case the sampled flag from traceparent is honored.
		/// </summary>
		[Fact]
		public async Task TraceContextIgnoreSampledFalse_WithEsTraceState_NotSampled()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContextIgnoreSampledFalse: "true", transactionSampleRate: "1");

			var client = new HttpClient();

			// Send traceparent with sampled=false and tracestate with a valid es flag
			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00");
			client.DefaultRequestHeaders.Add("tracestate", $"es=s:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			// Assert that the transaction is not sampled, so the traceparent header was not ignored
			_payloadSender1.FirstTransaction.IsSampled.Should().BeFalse();

			// Assert that the transaction is not a root transaction
			_payloadSender1.FirstTransaction.ParentId.Should().NotBeNullOrEmpty();
		}

		/// <summary>
		/// Sets the TraceContextIgnoreSampledFalse config and sends a traceparent without es vendor part.
		/// Makes sure in that case the sampled flag from traceparent is ignored.
		/// </summary>
		[Fact]
		public async Task TraceContextIgnoreSampledFalse_WithNonEsTraceState_NotSampled()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContextIgnoreSampledFalse: "true", transactionSampleRate: "1");

			var client = new HttpClient();

			// Send traceparent with sampled=false and a tracestate with no es vendor flag
			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00");
			client.DefaultRequestHeaders.Add("tracestate", $"foo=bar:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			// Assert that the transaction is sampled and the traceparent header was ignored
			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();

			// Assert that the transaction is a root transaction
			_payloadSender1.FirstTransaction.ParentId.Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Does not set the TraceContextIgnoreSampledFalse config and makes sure that the traceparent header is not ignored.
		/// This tests the default case when the config is not set, so the agent just follows W3C and takes the sampled flag as it
		/// is.
		/// </summary>
		[Fact]
		public async Task TraceContextIgnoreSampledFalse_NotSet_WithNonEsTraceState_NotSampled()
		{
			// Set TraceContextIgnoreSampledFalse to default (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContextIgnoreSampledFalse: "false", transactionSampleRate: "1");

			var client = new HttpClient();

			// Send traceparent with sampled=false
			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00");
			client.DefaultRequestHeaders.Add("tracestate", $"foo=bar:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			// Assert that the transaction is not sampled and the traceparent header was not ignored
			_payloadSender1.FirstTransaction.IsSampled.Should().BeFalse();

			// Assert that the transaction is not a root transaction
			_payloadSender1.FirstTransaction.ParentId.Should().NotBeNullOrEmpty();
		}

		[Fact]
		public async Task TraceContinuationStrategyContinue()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContextIgnoreSampledFalse: "continue");

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Add("tracestate", $"foo=bar:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender1.FirstTransaction.ParentId.Should().Be("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");

			_payloadSender1.FirstTransaction.Links.Should().BeNullOrEmpty();
		}

		[Fact]
		public async Task TraceContinuationStrategyDefault()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger());

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Add("tracestate", $"foo=bar:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender1.FirstTransaction.ParentId.Should().Be("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");

			_payloadSender1.FirstTransaction.Links.Should().BeNullOrEmpty();
		}

		[Fact]
		public async Task TraceContinuationStrategyRestartExternalWithNoEsTag()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContinuationStrategy: "restart_external");

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Add("tracestate", $"foo=bar:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender1.FirstTransaction.ParentId.Should().NotBe("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.TraceId.Should().NotBe("0af7651916cd43dd8448eb211c80319c");

			_payloadSender1.FirstTransaction.Links.Should().HaveCount(1);
			_payloadSender1.FirstTransaction.Links.ElementAt(0).SpanId.Should().Be("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.Links.ElementAt(0).TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		}

		[Fact]
		public async Task TraceContinuationStrategyRestartExternalWithEsTag()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContinuationStrategy: "restart_external");

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Add("tracestate", $"es=s:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender1.FirstTransaction.ParentId.Should().Be("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");

			_payloadSender1.FirstTransaction.Links.Should().BeNullOrEmpty();
		}

		[Fact]
		public async Task TraceContinuationStrategyRestartWithEsTag()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContinuationStrategy: "restart");

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Add("tracestate", $"es=s:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender1.FirstTransaction.ParentId.Should().NotBe("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.TraceId.Should().NotBe("0af7651916cd43dd8448eb211c80319c");

			_payloadSender1.FirstTransaction.Links.Should().HaveCount(1);
			_payloadSender1.FirstTransaction.Links.ElementAt(0).SpanId.Should().Be("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.Links.ElementAt(0).TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		}

		[Fact]
		public async Task TraceContinuationStrategyRestartWithoutEsTag()
		{
			// Set TraceContextIgnoreSampledFalse (and 100% sample rate)
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContinuationStrategy: "restart");

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("traceparent", $"00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Add("tracestate", $"foo=bar:0.5");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();
			_payloadSender1.FirstTransaction.ParentId.Should().NotBe("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.TraceId.Should().NotBe("0af7651916cd43dd8448eb211c80319c");

			_payloadSender1.FirstTransaction.Links.Should().HaveCount(1);
			_payloadSender1.FirstTransaction.Links.ElementAt(0).SpanId.Should().Be("b7ad6b7169203331");
			_payloadSender1.FirstTransaction.Links.ElementAt(0).TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		}

		[Fact]
		public async Task TraceContinuationStrategyRestartExternalAndNoTraceParent()
		{
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContinuationStrategy: "restart_external");

			var client = new HttpClient();

			// HttpClient always seem to add a `traceparent` header - calling `Remove("traceparent")` does not help.
			// Therefore we add a fake value which fails validation and it'll be treated as a `null` traceparent` header.
			client.DefaultRequestHeaders.Add("traceparent", "foo");
			client.DefaultRequestHeaders.Remove("tracestate");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.Transactions.Should().NotBeNullOrEmpty();
		}

		/// <summary>
		/// Sends a request with a valid `traceparent` and without a `tracestate` header with setting `traceContinuationStrategy`
		/// to `restart_external`.
		/// Makes sure the trace is restarted, meaning values from the `traceparent` header aren't used for the transaction.
		/// </summary>
		[Fact]
		public async Task TraceContinuationStrategyRestartExternalAndNoTraceState()
		{
			_agent1.ConfigurationStore.CurrentSnapshot =
				new MockConfiguration(new NoopLogger(), traceContinuationStrategy: "restart_external");

			var client = new HttpClient();

			client.DefaultRequestHeaders.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
			client.DefaultRequestHeaders.Remove("tracestate");

			var res = await client.GetAsync("http://localhost:5901/Home/Index");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.Transactions.Should().NotBeNullOrEmpty();

			// The trace is restarted (due to `traceContinuationStrategy=restart_external`), so assert that the traceId and
			// parentId aren't reused from `traceparent`.
			_payloadSender1.FirstTransaction.Id.Should().NotBe("0af7651916cd43dd8448eb211c80319c");
			_payloadSender1.FirstTransaction.ParentId.Should().NotBe("b7ad6b7169203331");
		}

		public async Task DisposeAsync()
		{
			_cancellationTokenSource.Cancel();
			await Task.WhenAll(_taskForApp1, _taskForApp2);

			_agent1?.Dispose();
			_agent2?.Dispose();
		}

		/// <summary>
		/// Executes the distributed tracing call between the 2 services.
		/// </summary>
		/// <param name="startActivityBeforeHttpCall ">If true, we'll create an actvity on .NET 5</param>
		/// <returns></returns>
		private async Task ExecuteAndCheckDistributedCall(bool startActivityBeforeHttpCall = true)
		{
#if NET5_0_OR_GREATER
			// .NET 5 has built-in W3C TraceContext support and Activity uses the W3C id format by default (pre .NET 5 it was opt-in)
			// This means if there is no active activity, the outgoing HTTP request on HttpClient will add the traceparent header with
			// a flag recorded=false. The agent would pick this up on the incoming call and start an unsampled transaction.
			// To prevent this we start an activity and set the recorded flag to true.
			Activity activity = null;
			if (startActivityBeforeHttpCall)
			{
				activity = new Activity("foo").Start();
				activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
			}
#endif
			var client = new HttpClient();
			var res = await client.GetAsync("http://localhost:5901/Home/DistributedTracingMiniSample");
			res.IsSuccessStatusCode.Should().BeTrue();

			_payloadSender1.Transactions.Count.Should().Be(1);
			_payloadSender2.Transactions.Count.Should().Be(1);

			//make sure the 2 transactions have the same traceid:
			_payloadSender2.FirstTransaction.TraceId.Should().Be(_payloadSender1.FirstTransaction.TraceId);
#if NET5_0
			if (startActivityBeforeHttpCall)
				activity.Dispose();
#endif
		}
	}
}
