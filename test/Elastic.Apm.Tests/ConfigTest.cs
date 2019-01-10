using System;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Tests the configuration through environment variables
	/// </summary>
	public class EnvVarConfigTest
	{
		public EnvVarConfigTest() => TestHelper.ResetAgentAndEnvVars();

		[Fact]
		public void ServerUrlsSimpleTest()
		{
			var serverUrl = "http://myServer.com:1234";
			var agent = new ApmAgent(new TestAgentConfiguration(serverUrls: serverUrl));
			Assert.Equal(serverUrl, agent.Config.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl.ToLower() + "/", agent.Config.ServerUrls[0].ToString().ToLower());
		}

		[Fact]
		public void ServerUrlsInvalidUrlTest()
		{
			var serverUrl = "InvalidUrl";
			var agent = new ApmAgent(new TestAgentConfiguration(serverUrls: serverUrl));
			Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), agent.Config.ServerUrls[0].ToString());
		}

		[Fact]
		public void ServerUrlInvalidUrlLogTest()
		{
			var serverUrl = "InvalidUrl";
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentConfiguration(logger: logger, serverUrls: serverUrl));
			Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), agent.Config.ServerUrls[0].ToString());

			Assert.Equal($"Error {nameof(TestAgentConfiguration)}: Failed parsing server URL from test: {EnvVarConsts.ServerUrls}, value: {serverUrl}",
				logger.Lines[0]);
		}

		/// <summary>
		/// Sets 2 servers and makes sure that they are all parsed
		/// </summary>
		[Fact]
		public void ServerUrlsMultipleUrlsTest()
		{
			var serverUrl1 = "http://myServer1.com:1234";
			var serverUrl2 = "http://myServer2.com:1234";
			var serverUrls = $"{serverUrl1},{serverUrl2}";

			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentConfiguration(logger: logger, serverUrls: serverUrls));

			Assert.Equal(serverUrl1, agent.Config.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl1.ToLower() + "/", agent.Config.ServerUrls[0].ToString().ToLower());

			Assert.Equal(serverUrl2, agent.Config.ServerUrls[1].OriginalString);
			Assert.Equal(serverUrl2.ToLower() + "/", agent.Config.ServerUrls[1].ToString().ToLower());
		}

		/// <summary>
		/// Sets 3 server urls, 2 of them are valid, 1 is invalid
		/// Makes sure that the 2 valid urls are parsed and there is a log line for the invalid server url
		/// </summary>
		[Fact]
		public void ServerUrlsMultipleUrlsWith1InvalidUrlTest()
		{

			var serverUrl1 = "http://myServer1.com:1234";
			var serverUrl2 = "invalidUrl";
			var serverUrl3 = "http://myServer2.com:1234";
			var serverUrls = $"{serverUrl1},{serverUrl2},{serverUrl3}";
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentConfiguration(logger: logger, serverUrls: serverUrls));

			Assert.Equal(serverUrl1, agent.Config.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl1.ToLower() + "/", agent.Config.ServerUrls[0].ToString().ToLower());

			Assert.Equal(serverUrl3, agent.Config.ServerUrls[1].OriginalString);
			Assert.Equal(serverUrl3.ToLower() + "/", agent.Config.ServerUrls[1].ToString().ToLower());

			Assert.Equal($"Error {nameof(TestAgentConfiguration)}: Failed parsing server URL from test: {EnvVarConsts.ServerUrls}, value: {serverUrl2}",
				logger.Lines[0]);
		}

		[Fact]
		public void DefaultLogLevelTest() => Assert.Equal(LogLevel.Error, Agent.Config.LogLevel);

		[Fact]
		public void SetDebugLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentConfiguration(logLevel: "Debug"));
			Assert.Equal(LogLevel.Debug, agent.Config.LogLevel);
		}

		[Fact]
		public void SetErrorLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentConfiguration(logLevel: "Error"));
			Assert.Equal(LogLevel.Error, agent.Config.LogLevel);
		}

		[Fact]
		public void SetInfoLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentConfiguration(logLevel: "Info"));
			Assert.Equal(LogLevel.Info, agent.Config.LogLevel);
		}

		[Fact]
		public void SetWarningLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentConfiguration(logLevel: "Warning"));
			Assert.Equal(LogLevel.Warning, agent.Config.LogLevel);
		}

		[Fact]
		public void SetInvalidLogLevelTest()
		{
			var logLevelValue = "InvalidLogLevel";
			var agent = new ApmAgent(new TestAgentConfiguration(logLevel: logLevelValue));
			var logger = agent.Config.Logger as TestLogger;

			Assert.Equal(LogLevel.Error, agent.Config.LogLevel);
			Assert.Equal(
				$"Error Config: Failed parsing log level from test: {EnvVarConsts.LogLevel}, value: {logLevelValue}. Defaulting to log level 'Error'",
				logger.Lines[0]);
		}
	}
}
