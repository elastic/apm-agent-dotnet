using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class LoggerTest
	{
		[Fact]
		public void TestLogError()
		{
			var logger = LogWithLevel(LogLevel.Error);

			Assert.Single(logger.Lines);
			Assert.Equal("Error log", logger.Lines[0]);
		}

		[Fact]
		public void TestLogWarning()
		{
			var logger = LogWithLevel(LogLevel.Warning);

			Assert.Equal(2, logger.Lines.Count);
			Assert.Equal("Error log", logger.Lines[0]);
			Assert.Equal("Warning log", logger.Lines[1]);
		}

		[Fact]
		public void TestLogInfo()
		{
			var logger = LogWithLevel(LogLevel.Information);

			Assert.Equal(3, logger.Lines.Count);
			Assert.Equal("Error log", logger.Lines[0]);
			Assert.Equal("Warning log", logger.Lines[1]);
			Assert.Equal("Info log", logger.Lines[2]);
		}

		[Fact]
		public void TestLogDebug()
		{
			var logger = LogWithLevel(LogLevel.Debug);

			Assert.Equal(4, logger.Lines.Count);
			Assert.Equal("Error log", logger.Lines[0]);
			Assert.Equal("Warning log", logger.Lines[1]);
			Assert.Equal("Info log", logger.Lines[2]);
			Assert.Equal("Debug log", logger.Lines[3]);
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
