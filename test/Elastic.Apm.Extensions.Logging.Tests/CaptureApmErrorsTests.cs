using System.Threading.Tasks;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleConsoleNetCoreApp;
using Xunit;

namespace Elastic.Apm.Extensions.Logging.Tests
{
	public class CaptureApmErrorsTests
	{
		[Fact]
		public async Task CaptureErrorLogsAsApmError()
		{
			var payloadSender = new MockPayloadSender();
			using var hostBuilder = CreateHostBuilder(payloadSender).Build();

			await hostBuilder.StartAsync();

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Log.Message.Should().Be("This is a sample error log message, with a sample value: 42");
			payloadSender.FirstError.Log.ParamMessage.Should().Be("This is a sample error log message, with a sample value: {intParam}");

			await hostBuilder.StopAsync();
		}

		private static IHostBuilder CreateHostBuilder(MockPayloadSender payloadSender = null) =>
		Host.CreateDefaultBuilder()
			.ConfigureServices(n => n.AddSingleton<IPayloadSender, MockPayloadSender>(serviceProvider => payloadSender))
			.ConfigureServices((context, services) => { services.AddHostedService<HostedService>(); })
			.ConfigureLogging((hostingContext, logging) =>
			{
				logging.ClearProviders();
#if NET5_0
				logging.AddSimpleConsole(o => o.IncludeScopes = true);
#else
				logging.AddConsole(options => options.IncludeScopes = true);
#endif
			})
			.UseElasticApm();
	}
}
