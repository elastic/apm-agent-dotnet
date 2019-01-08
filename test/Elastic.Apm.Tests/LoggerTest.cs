using System;
using System.Reflection;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Xunit;

namespace Elastic.Apm.Tests
{
    public class LoggerTest
    {
        readonly AbstractLogger _logger;

        public LoggerTest()
        {
            TestHelper.ResetAgentAndEnvVars();

            Apm.Agent.SetLoggerType<TestLogger>();
            _logger = Apm.Agent.CreateLogger("Test");
        }

        [Fact]
        public void TestLogError()
        {
            LogWithLevel(LogLevel.Error);

            Assert.Single((_logger as TestLogger).Lines);
            Assert.Equal("Error Test: Error log", (_logger as TestLogger).Lines[0]);
        }

        [Fact]
        public void TestLogWarning()
        {
            LogWithLevel(LogLevel.Warning);

            Assert.Equal(2, (_logger as TestLogger).Lines.Count);
            Assert.Equal("Error Test: Error log", (_logger as TestLogger).Lines[0]);
            Assert.Equal("Warning Test: Warning log", (_logger as TestLogger).Lines[1]);
        }

        [Fact]
        public void TestLogInfo()
        {
            LogWithLevel(LogLevel.Info);

            Assert.Equal(3, (_logger as TestLogger).Lines.Count);
            Assert.Equal("Error Test: Error log", (_logger as TestLogger).Lines[0]);
            Assert.Equal("Warning Test: Warning log", (_logger as TestLogger).Lines[1]);
            Assert.Equal("Info Test: Info log", (_logger as TestLogger).Lines[2]);
        }

        [Fact]
        public void TestLogDebug()
        {
            LogWithLevel(LogLevel.Debug);

            Assert.Equal(4, (_logger as TestLogger).Lines.Count);
            Assert.Equal("Error Test: Error log", (_logger as TestLogger).Lines[0]);
            Assert.Equal("Warning Test: Warning log", (_logger as TestLogger).Lines[1]);
            Assert.Equal("Info Test: Info log", (_logger as TestLogger).Lines[2]);
            Assert.Equal("Debug Test: Debug log", (_logger as TestLogger).Lines[3]);
        }

        private void LogWithLevel(LogLevel logLevel)
        {
            Apm.Agent.Config.LogLevel = logLevel;

            _logger.LogError("Error log");
            _logger.LogWarning("Warning log");
            _logger.LogInfo("Info log");
            _logger.LogDebug("Debug log");
        }
    }
}
