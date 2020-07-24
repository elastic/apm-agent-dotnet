using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
				.UseElasticApm(new HttpDiagnosticsSubscriber());
	}
}
