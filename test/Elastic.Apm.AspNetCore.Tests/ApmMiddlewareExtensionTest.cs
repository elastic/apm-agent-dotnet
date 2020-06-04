// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="Elastic.Apm.AspNetCore.ApmMiddlewareExtension" /> type.
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

			Assert.IsType<ConsoleLogger>(logger);
			Assert.Same(ConsoleLogger.Instance, logger);
		}
	}
}
