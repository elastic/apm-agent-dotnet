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
        readonly AbstractLogger logger;

        public LoggerTest()
        {
            TestHelper.ResetAgentAndEnvVars();

            Apm.Agent.SetLoggerType<TestLogger>();
            logger = Apm.Agent.CreateLogger("Test");
        }

        [Fact]
        public void TestLogError()
        {
            LogWithLevel(LogLevel.Error);

            Assert.Single((logger as TestLogger).Lines);
            Assert.Equal("Error Test: Error log", (logger as TestLogger).Lines[0]);
        }

        [Fact]
        public void TestLogWarning()
        {
            LogWithLevel(LogLevel.Warning);

            Assert.Equal(2, (logger as TestLogger).Lines.Count);
            Assert.Equal("Error Test: Error log", (logger as TestLogger).Lines[0]);
            Assert.Equal("Warning Test: Warning log", (logger as TestLogger).Lines[1]);
        }

        [Fact]
        public void TestLogInfo()
        {
            LogWithLevel(LogLevel.Info);

            Assert.Equal(3, (logger as TestLogger).Lines.Count);
            Assert.Equal("Error Test: Error log", (logger as TestLogger).Lines[0]);
            Assert.Equal("Warning Test: Warning log", (logger as TestLogger).Lines[1]);
            Assert.Equal("Info Test: Info log", (logger as TestLogger).Lines[2]);
        }

        [Fact]
        public void TestLogDebug()
        {
            LogWithLevel(LogLevel.Debug);

            Assert.Equal(4, (logger as TestLogger).Lines.Count);
            Assert.Equal("Error Test: Error log", (logger as TestLogger).Lines[0]);
            Assert.Equal("Warning Test: Warning log", (logger as TestLogger).Lines[1]);
            Assert.Equal("Info Test: Info log", (logger as TestLogger).Lines[2]);
            Assert.Equal("Debug Test: Debug log", (logger as TestLogger).Lines[3]);
        }

        private void LogWithLevel(LogLevel logLevel)
        {
            Apm.Agent.Config.LogLevel = logLevel;

            logger.LogError("Error log");
            logger.LogWarning("Warning log");
            logger.LogInfo("Info log");
            logger.LogDebug("Debug log");
        }
    }
}
