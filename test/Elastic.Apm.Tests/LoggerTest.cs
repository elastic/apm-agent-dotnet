﻿using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class LoggerTest
	{
		[Fact]
		public void TestLogError()
		{
			var logger = LogWithLevel(LogLevel.Error);

			logger.Lines.Should().ContainSingle();
			logger.Lines[0].Should().Be("Error log");
		}

		[Fact]
		public void TestLogWarning()
		{
			var logger = LogWithLevel(LogLevel.Warning);

			logger.Lines.Count.Should().Be(2);
			logger.Lines[0].Should().Be("Error log");
			logger.Lines[1].Should().Be("Warning log");
		}

		[Fact]
		public void TestLogInfo()
		{
			var logger = LogWithLevel(LogLevel.Information);

			logger.Lines.Count.Should().Be(3);
			logger.Lines[0].Should().Be("Error log");
			logger.Lines[1].Should().Be("Warning log");
			logger.Lines[2].Should().Be("Info log");
		}

		[Fact]
		public void TestLogDebug()
		{
			var logger = LogWithLevel(LogLevel.Debug);

			logger.Lines.Count.Should().Be(4);
			logger.Lines[0].Should().Be("Error log");
			logger.Lines[1].Should().Be("Warning log");
			logger.Lines[2].Should().Be("Info log");
			logger.Lines[3].Should().Be("Debug log");
		}

		private TestLogger LogWithLevel(LogLevel logLevel)
		{
			var logger = new TestLogger(logLevel);

			logger.LogError("Error log");
			logger.LogWarning("Warning log");
			logger.LogInfo("Info log");
			logger.LogDebug("Debug log");
			return logger;
		}
	}
}
