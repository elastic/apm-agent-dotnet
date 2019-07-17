using System.Collections.Generic;
using Elastic.Apm.AspNetCore.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="Elastic.Apm.AspNetCore.Extensions.LoggerExtensions"/> type.
	/// </summary>
	public class LoggerExtensionsTests
	{
		[Theory]
		[InlineData(Microsoft.Extensions.Logging.LogLevel.Trace, LogLevel.Trace)]
		[InlineData(Microsoft.Extensions.Logging.LogLevel.Debug, LogLevel.Debug)]
		[InlineData(Microsoft.Extensions.Logging.LogLevel.Information, LogLevel.Information)]
		[InlineData(Microsoft.Extensions.Logging.LogLevel.Warning, LogLevel.Warning)]
		[InlineData(Microsoft.Extensions.Logging.LogLevel.Error, LogLevel.Error)]
		[InlineData(Microsoft.Extensions.Logging.LogLevel.Critical, LogLevel.Critical)]
		[InlineData(Microsoft.Extensions.Logging.LogLevel.None, LogLevel.None)]
		public void GetMinLogLevelTest(Microsoft.Extensions.Logging.LogLevel level, LogLevel expected)
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new[]{new KeyValuePair<string, string>("Logging:LogLevel:Default", level.ToString()), })
				.Build();

			var serviceProvider = new ServiceCollection()
				.AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole())
				.AddOptions()
				.BuildServiceProvider();

			var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<LoggerExtensionsTests>();

			var minLogLevel = logger.GetMinLogLevel();

			Assert.Equal(expected, minLogLevel);
		}
	}
}
