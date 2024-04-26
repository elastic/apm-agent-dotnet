// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="ApplicationBuilderExtensions" /> type.
	/// </summary>
	public class ApplicationBuilderExtensionLoggingTest
	{
		[Fact]
		public void UseElasticApmShouldUseAspNetLoggerWhenLoggingIsConfigured()
		{
			var services = new ServiceCollection()
				.AddLogging();

			var logger = NetCoreLogger.GetApmLogger(services.BuildServiceProvider());

			Assert.IsType<NetCoreLogger>(logger);
		}

		[Fact]
		public void UseElasticApmShouldUseConsoleLoggerInstanceWhenLoggingIsNotConfigured()
		{
			var services = new ServiceCollection();

			var logger = NetCoreLogger.GetApmLogger(services.BuildServiceProvider());

			Assert.IsType<ConsoleLogger>(logger);
			Assert.Same(ConsoleLogger.Instance, logger);
		}
	}
}
