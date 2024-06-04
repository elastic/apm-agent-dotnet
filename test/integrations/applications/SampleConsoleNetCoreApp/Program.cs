// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
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
				.ConfigureServices((_, services) => { services.AddElasticApm(new HttpDiagnosticsSubscriber()).AddHostedService<HostedService>(); })
				.ConfigureLogging((_, logging) =>
				{
					logging.ClearProviders();
					logging.AddConsole(options => options.IncludeScopes = true);
				});
	}
}
