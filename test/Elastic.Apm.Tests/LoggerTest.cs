using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class LoggerTest
	{
		private readonly TestLogger _logger;

		public LoggerTest()
		{
			TestHelper.ResetAgentAndEnvVars();

			Agent.SetLoggerType<TestLogger>();
			_logger = (TestLogger)Agent.CreateLogger("Test");
		}

		[Fact]
		public void TestLogError()
		{
			LogWithLevel(LogLevel.Error);

			Assert.Single(_logger.Lines);
			Assert.Equal("Error Test: Error log", (_logger).Lines[0]);
		}

		[Fact]
		public void TestLogWarning()
		{
			LogWithLevel(LogLevel.Warning);

			Assert.Equal(2, (_logger).Lines.Count);
			Assert.Equal("Error Test: Error log", (_logger).Lines[0]);
			Assert.Equal("Warning Test: Warning log", (_logger).Lines[1]);
		}

		[Fact]
		public void TestLogInfo()
		{
			LogWithLevel(LogLevel.Info);

			Assert.Equal(3, (_logger).Lines.Count);
			Assert.Equal("Error Test: Error log", (_logger).Lines[0]);
			Assert.Equal("Warning Test: Warning log", (_logger).Lines[1]);
			Assert.Equal("Info Test: Info log", (_logger).Lines[2]);
		}

		[Fact]
		public void TestLogDebug()
		{
			LogWithLevel(LogLevel.Debug);

			Assert.Equal(4, (_logger).Lines.Count);
			Assert.Equal("Error Test: Error log", (_logger).Lines[0]);
			Assert.Equal("Warning Test: Warning log", (_logger).Lines[1]);
			Assert.Equal("Info Test: Info log", (_logger).Lines[2]);
			Assert.Equal("Debug Test: Debug log", (_logger).Lines[3]);
		}

		private void LogWithLevel(LogLevel logLevel)
		{
			Agent.Config.LogLevel = logLevel;

			_logger.LogError("Error log");
			_logger.LogWarning("Warning log");
			_logger.LogInfo("Info log");
			_logger.LogDebug("Debug log");
		}
	}
}
