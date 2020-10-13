using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleConsoleNetCoreApp;
using Xunit;

namespace Elastic.Apm.Extensions.Hosting.Tests
{
	public class HostBuilderExtensionTests
	{
		/// <summary>
		/// Makes sure in case of 2 IHostBuilder insatnces when both call UseElasticApm no exception is thrown
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task TwoHostBuildersNoException()
		{
			var hostBuilder1 = CreateHostBuilder().Build();
			var hostBuilder2 = CreateHostBuilder().Build();
			var builder1Task = hostBuilder1.StartAsync();
			var builder2Task = hostBuilder2.StartAsync();

			await Task.WhenAll(builder1Task, builder2Task);
			await Task.WhenAll(hostBuilder1.StopAsync(), hostBuilder2.StopAsync());
		}

		private static IHostBuilder CreateHostBuilder() =>
				Host.CreateDefaultBuilder()
					.ConfigureServices((context, services) => { services.AddHostedService<HostedService>(); })
					.UseElasticApm();
	}
}
