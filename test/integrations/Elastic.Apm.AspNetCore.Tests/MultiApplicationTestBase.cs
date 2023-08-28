// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests;

/// <summary>
/// Base class to encapsulate 2 <see cref="ApmAgent" /> instances injected into HostBuilder instances.
/// </summary>
public abstract class MultiApplicationTestBase : IAsyncLifetime
{
	internal readonly MockPayloadSender _payloadSender1 = new();
	internal readonly MockPayloadSender _payloadSender2 = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	internal ApmAgent _agent1;
	internal ApmAgent _agent2;

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

	public async Task DisposeAsync()
	{
		_cancellationTokenSource.Cancel();
		await Task.WhenAll(_taskForApp1, _taskForApp2);

		_agent1?.Dispose();
		_agent2?.Dispose();
	}
}
