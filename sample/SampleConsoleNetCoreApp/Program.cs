using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleConsoleNetCoreApp
{
	internal static class Program
	{
		private static async Task Main(string[] args)
		{
			var hostBuilder = CreateHostBuilder(args);

			await hostBuilder.RunConsoleAsync();
		}

		private static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices((context, services) => { services.AddHostedService<HostedService>(); })
				.ConfigureLogging((hostingContext, logging) =>
				{
					logging.ClearProviders();
					logging.AddConsole(options => options.IncludeScopes = true);
				})
				.UseElasticApm(new HttpDiagnosticsSubscriber());
	}
}
