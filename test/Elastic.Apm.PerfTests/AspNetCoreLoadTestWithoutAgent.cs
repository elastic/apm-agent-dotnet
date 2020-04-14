using System.Net.Http;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;

namespace Elastic.Apm.PerfTests
{
	/// <summary>
	/// A Test which triggers a simple ASP.NET Core endpoint and measures the response WITHOUT agent.
	/// </summary>
	public class AspNetCoreLoadTestWithoutAgent
	{
		[GlobalSetup]
		public void Setup()
		{
			var aspNetCoreTest = new AspNetCoreWithoutAgentAppRunner();
			aspNetCoreTest.StartSampleAppWithAgent();

			var httpClient = new HttpClient();
			httpClient.GetAsync("http://localhost:5902").Wait();
		}

		[Benchmark]
		public void LoadWithoutAgent()
		{
			var httpClient = new HttpClient();
			httpClient.GetAsync("http://localhost:5902/Home/EmptyWebRequest").Wait();
		}
	}

	public class AspNetCoreWithoutAgentAppRunner
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		public void StartSampleAppWithAgent() =>
			SampleAspNetCoreApp.Program.CreateWebHostBuilder(null)
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
					//app.UseElasticApm(_agent1, new TestLogger(),
					//	new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
					Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5902")
				.Build()
				.RunAsync(_cancellationTokenSource.Token);
	}
}
