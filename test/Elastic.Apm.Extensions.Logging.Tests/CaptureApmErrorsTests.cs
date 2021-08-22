// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
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

			// Test a log with exception
			var logger = (ILogger)hostBuilder.Services.GetService(typeof(ILogger<object>));

			try
			{
				throw new Exception();
			}
			catch (Exception e)
			{
				logger.LogError(e, "error log with exception");
			}

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();
			payloadSender.Errors.Where(n => n.Log.Message == "error log with exception" &&
					n.Log.StackTrace != null && n.Log.StackTrace.Count > 0)
				.Should()
				.NotBeNullOrEmpty();

			await hostBuilder.StopAsync();
		}

		private static IHostBuilder CreateHostBuilder(MockPayloadSender payloadSender = null) =>
			Host.CreateDefaultBuilder()
				.ConfigureServices(n => n.AddSingleton<IPayloadSender, MockPayloadSender>(_ => payloadSender))
				.ConfigureServices((_, services) => { services.AddHostedService<HostedService>(); })
				.ConfigureLogging((_, logging) =>
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
