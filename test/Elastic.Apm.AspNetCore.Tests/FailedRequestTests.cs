// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
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
	public class FailedRequestTests : IAsyncLifetime
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly MockPayloadSender _payloadSender1 = new MockPayloadSender();
		private ApmAgent _agent1;

		private Task _taskForApp1;

		public Task InitializeAsync()
		{
			_agent1 = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender1, configuration: new MockConfiguration(exitSpanMinDuration:"0")));

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

			return Task.CompletedTask;
		}


		/// <summary>
		/// Calls Home/TriggerError (which throws an exception) and makes sure the result and the outcome are captured
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task DistributedTraceAcross2Service()
		{
			var client = new HttpClient();
			var res = await client.GetAsync("http://localhost:5901/Home/TriggerError");
			res.IsSuccessStatusCode.Should().BeFalse();

			_payloadSender1.Transactions.Count.Should().Be(1);
			_payloadSender1.FirstTransaction.Should().NotBeNull();
			_payloadSender1.FirstTransaction.Result.Should().Be("HTTP 5xx");
			_payloadSender1.FirstTransaction.Outcome.Should().Be(Outcome.Failure);
		}

		public async Task DisposeAsync()
		{
			_cancellationTokenSource.Cancel();
			await Task.WhenAll(_taskForApp1);

			_agent1?.Dispose();
		}
	}
}
