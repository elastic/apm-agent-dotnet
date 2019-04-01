using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp.Data;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Distributed tracing integration test.
	/// It starts <see cref="SampleAspNetCoreApp"/> with 1 agent and <see cref="WebApiSample"/> with another agent.
	/// It calls the 'DistributedTracingMiniSample' in <see cref="SampleAspNetCoreApp"/>, which generates a simple HTTP response
	/// by calling <see cref="WebApiSample"/> via HTTP.
	/// Makes sure that all spans and transactions across the 2 services have the same trace id.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class DistributedTracingAspNetCoreTests
	{
		private readonly MockPayloadSender _payloadSender1 = new MockPayloadSender();
		private readonly MockPayloadSender _payloadSender2 = new MockPayloadSender();

		public DistributedTracingAspNetCoreTests()
		{
			var unused = SampleAspNetCoreApp.Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
					{
						var connection = @"Data Source=blogging.db";
						services.AddDbContext<SampleDataContext>
							(options => options.UseSqlite(connection));
						services.AddMvc()
							.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))))
							.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
					}
				)
				.Configure(app =>
				{
					app.UseElasticApm(new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender1)), new TestLogger(), new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
					SampleAspNetCoreApp.Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5900")
				.Build()
				.RunAsync();

			var unused1 = WebApiSample.Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
				{
					services.AddMvc()
						.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(WebApiSample))))
						.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
				})
				.Configure(app =>
				{
					app.UseElasticApm(new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender2)), new TestLogger(), new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
					WebApiSample.Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5050")
				.Build()
				.RunAsync();
		}

		[Fact]
		public async Task DistributedTraceAcross2Service()
		{
			var client = new HttpClient();
			var res = await client.GetAsync("http://localhost:5900/Home/DistributedTracingMiniSample");
			res.IsSuccessStatusCode.Should().BeTrue();

			//make sure the 2 transactions have the same traceid:
			_payloadSender2.FirstTransaction.TraceId.Should().Be(_payloadSender1.FirstTransaction.TraceId);

			//make sure all spans have the same traceid:
			_payloadSender1.Spans.Should().NotContain(n => n.TraceId != _payloadSender1.FirstTransaction.TraceId);
			_payloadSender2.Spans.Should().NotContain(n => n.TraceId != _payloadSender1.FirstTransaction.TraceId);
		}
	}
}
