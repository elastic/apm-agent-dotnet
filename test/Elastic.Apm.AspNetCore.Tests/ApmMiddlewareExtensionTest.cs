using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="Elastic.Apm.AspNetCore.ApmMiddlewareExtension"/> type.
	/// </summary>
	public class ApmMiddlewareExtensionTest
	{
		[Fact]
		public void UseElasticApmShouldUseAspNetLoggerWhenLoggingIsConfigured()
		{
			var services = new ServiceCollection()
				.AddLogging();

			var logger = services.BuildServiceProvider().GetApmLogger();

			Assert.IsType<AspNetCoreLogger>(logger);
		}

		[Fact]
		public void UseElasticApmShouldUseConsoleLoggerInstanceWhenLoggingIsNotConfigured()
		{
			var services = new ServiceCollection();

			var logger = services.BuildServiceProvider().GetApmLogger();

			Assert.IsType<Logging.ConsoleLogger>(logger);
			Assert.Same(Logging.ConsoleLogger.Instance, logger);
		}
	}
}
