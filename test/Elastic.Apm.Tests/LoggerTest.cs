using Elastic.Apm.Logging;
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
			logger.Lines[0].Should().EndWith("[Error] - Error log");
		}

		[Fact]
		public void TestLogWarning()
		{
			var logger = LogWithLevel(LogLevel.Warning);

			logger.Lines.Count.Should().Be(2);
			logger.Lines[0].Should().EndWith("[Error] - Error log");
			logger.Lines[1].Should().EndWith("[Warning] - Warning log");
		}

		[Fact]
		public void TestLogInfo()
		{
			var logger = LogWithLevel(LogLevel.Information);

			logger.Lines.Count.Should().Be(3);
			logger.Lines[0].Should().EndWith("[Error] - Error log");
			logger.Lines[1].Should().EndWith("[Warning] - Warning log");
			logger.Lines[2].Should().EndWith("[Info] - Info log");
		}

		[Fact]
		public void TestLogDebug()
		{
			var logger = LogWithLevel(LogLevel.Debug);

			logger.Lines.Count.Should().Be(4);
			logger.Lines[0].Should().EndWith("[Error] - Error log");
			logger.Lines[1].Should().EndWith("[Warning] - Warning log");
			logger.Lines[2].Should().EndWith("[Info] - Info log");
			logger.Lines[3].Should().EndWith("[Debug] - Debug log");
		}

		private TestLogger LogWithLevel(LogLevel logLevel)
		{
			var logger = new TestLogger(logLevel);

			logger.Error()?.Log("Error log");
			logger.Warning()?.Log("Warning log");
			logger.Info()?.Log("Info log");
			logger.Debug()?.Log("Debug log");
			return logger;
		}
	}
}
