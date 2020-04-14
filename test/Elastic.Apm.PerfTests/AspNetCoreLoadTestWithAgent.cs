using System.Net.Http;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;

namespace Elastic.Apm.PerfTests {

	/// <summary>
	/// A Test which triggers a simple ASP.NET Core endpoint and measures the response time while the agent is active in the process.
	/// </summary>
	public class AspNetCoreLoadTestWithAgent
	{
		[GlobalSetup]
		public void Setup()
		{
			var aspNetCoreTest = new AspNetCoreWithAgentAppRunner();
			aspNetCoreTest.StartSampleAppWithAgent();

			var httpClient = new HttpClient();
			httpClient.GetAsync("http://localhost:5901").Wait();
		}

		[Benchmark]
		public void LoadWithAgent()
		{
			var httpClient = new HttpClient();
			httpClient.GetAsync("http://localhost:5901/Home/EmptyWebRequest").Wait();
		}
	}

	public class AspNetCoreWithAgentAppRunner
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
					app.UseElasticApm(subscribers: new IDiagnosticsSubscriber[]{new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber()});
					Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5901")
				.Build()
				.RunAsync(_cancellationTokenSource.Token);
	}
}
