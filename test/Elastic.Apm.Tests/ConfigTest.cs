using System;
using System.Threading;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Tests the configuration through environment variables
	/// </summary>
	public class EnvVarConfigTest
	{
		[Fact]
		public void ServerUrlsSimpleTest()
		{
			var serverUrl = "http://myServer.com:1234";
			var agent = new ApmAgent(new TestAgentComponents(serverUrls: serverUrl));
			Assert.Equal(serverUrl, agent.ConfigurationReader.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl.ToLower() + "/", agent.ConfigurationReader.ServerUrls[0].ToString().ToLower());
		}

		[Fact]
		public void ServerUrlsInvalidUrlTest()
		{
			var serverUrl = "InvalidUrl";
			var agent = new ApmAgent(new TestAgentComponents(serverUrls: serverUrl));
			Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), agent.ConfigurationReader.ServerUrls[0].ToString());
		}

		[Fact]
		public void ServerUrlInvalidUrlLogTest()
		{
			var serverUrl = "InvalidUrl";
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger, serverUrl));
			Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), agent.ConfigurationReader.ServerUrls[0].ToString());

			Assert.Equal(
				$"Error {nameof(TestAgentConfigurationReader)}: Failed parsing server URL from {TestAgentConfigurationReader.Origin}: {ConfigConsts.ConfigKeys.Urls}, value: {serverUrl}",
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
			var agent = new ApmAgent(new TestAgentComponents(logger, serverUrls));

			Assert.Equal(serverUrl1, agent.ConfigurationReader.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl1.ToLower() + "/", agent.ConfigurationReader.ServerUrls[0].ToString().ToLower());

			Assert.Equal(serverUrl2, agent.ConfigurationReader.ServerUrls[1].OriginalString);
			Assert.Equal(serverUrl2.ToLower() + "/", agent.ConfigurationReader.ServerUrls[1].ToString().ToLower());
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
			var agent = new ApmAgent(new TestAgentComponents(logger, serverUrls));

			Assert.Equal(serverUrl1, agent.ConfigurationReader.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl1.ToLower() + "/", agent.ConfigurationReader.ServerUrls[0].ToString().ToLower());

			Assert.Equal(serverUrl3, agent.ConfigurationReader.ServerUrls[1].OriginalString);
			Assert.Equal(serverUrl3.ToLower() + "/", agent.ConfigurationReader.ServerUrls[1].ToString().ToLower());

			Assert.Equal(
				$"Error {nameof(TestAgentConfigurationReader)}: Failed parsing server URL from {TestAgentConfigurationReader.Origin}: {ConfigConsts.ConfigKeys.Urls}, value: {serverUrl2}",
				logger.Lines[0]);
		}

		[Fact]
		public void DefaultLogLevelTest() => Assert.Equal(LogLevel.Error, Agent.Config.LogLevel);

		[Fact]
		public void SetDebugLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Debug"));
			Assert.Equal(LogLevel.Debug, agent.ConfigurationReader.LogLevel);
		}

		[Fact]
		public void SetErrorLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Error"));
			Assert.Equal(LogLevel.Error, agent.ConfigurationReader.LogLevel);
		}

		[Fact]
		public void SetInfoLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Info"));
			Assert.Equal(LogLevel.Info, agent.ConfigurationReader.LogLevel);
		}

		[Fact]
		public void SetWarningLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Warning"));
			Assert.Equal(LogLevel.Warning, agent.ConfigurationReader.LogLevel);
		}

		[Fact]
		public void SetInvalidLogLevelTest()
		{
			var logLevelValue = "InvalidLogLevel";
			var agent = new ApmAgent(new TestAgentComponents(logLevelValue));
			var logger = agent.Logger as TestLogger;

			Assert.Equal(LogLevel.Error, agent.ConfigurationReader.LogLevel);
			Assert.Equal(
				$"Error Config: Failed parsing log level from {TestAgentConfigurationReader.Origin}: {ConfigConsts.ConfigKeys.Level}, value: {logLevelValue}. Defaulting to log level 'Error'",
				logger.Lines[0]);
		}

		/// <summary>
		/// The server doesn't accept services with '.' in it.
		/// This test makes sure we don't have '.' in the default service name.
		/// </summary>
		[Fact]
		public void DefaultServiceNameTest()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

			//By default XUnit uses 'testhost' as the entry assembly, and that is what the
			//agent reports if we don't set it to anything:
			Assert.False(string.IsNullOrEmpty(payloadSender.Payloads[0].Service.Name));
			Assert.False(payloadSender.Payloads[0].Service.Name.Contains('.'));
		}

		/// <summary>
		/// Sets the ELASTIC_APM_SERVICE_NAME environment variable and makes sure that
		/// when the agent sends data to the server it has the value from the
		/// ELASTIC_APM_SERVICE_NAME environment variable as service name.
		/// </summary>
		[Fact]
		public void ReadServiceNameViaEnvironmentVariable()
		{
			var serviceName = "MyService123";
			Environment.SetEnvironmentVariable(ConfigConsts.ConfigKeys.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

			Assert.Equal(serviceName, payloadSender.Payloads[0].Service.Name);
		}

		/// <summary>
		/// Sets the ELASTIC_APM_SERVICE_NAME environment variable to a value that contains a '.'
		/// Makes sure that when the agent sends data to the server it has the value from the
		/// ELASTIC_APM_SERVICE_NAME environment variable as service name and also makes sure that
		/// the '.' is replaced.
		/// </summary>
		[Fact]
		public void ReadServiceNameWithDotViaEnvironmentVariable()
		{
			var serviceName = "My.Service.Test";
			Environment.SetEnvironmentVariable(ConfigConsts.ConfigKeys.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

			Assert.Equal(serviceName.Replace('.', '_'), payloadSender.Payloads[0].Service.Name);
			Assert.False(payloadSender.Payloads[0].Service.Name.Contains('.'));
		}

		/// <summary>
		/// In case the user does not provide us a service name we try to calculate it based on the callstack.
		/// This test makes sure we recognize mscorlib and our own assemblies correctly in the
		/// <see cref="AbstractConfigurationReader.IsMsOrElastic(byte[])" /> method.
		/// </summary>
		[Fact]
		public void TestAbstractConfigurationReaderIsMsOrElastic()
		{
			var elasticToken = new byte[] { 174, 116, 0, 210, 193, 137, 207, 34 };
			var mscorlibToken = new byte[] { 183, 122, 92, 86, 25, 52, 224, 137 };

			Assert.True(AbstractConfigurationReader.IsMsOrElastic(elasticToken));
			Assert.True(AbstractConfigurationReader.IsMsOrElastic(elasticToken));

			Assert.False(AbstractConfigurationReader.IsMsOrElastic(new byte[] { 0 }));
			Assert.False(AbstractConfigurationReader.IsMsOrElastic(new byte[] { }));

			Assert.False(AbstractConfigurationReader
				.IsMsOrElastic(new[]
				{
					elasticToken[0], mscorlibToken[1], elasticToken[2],
					mscorlibToken[3], elasticToken[4], mscorlibToken[5], elasticToken[6], mscorlibToken[7]
				}));
		}

		/// <summary>
		/// Makes sure that even if the <see cref="EnvironmentConfigurationReader" /> is initialized without a logger
		/// it still defaults to some kind of logger.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			var config = new EnvironmentConfigurationReader();
			Assert.NotNull(config.Logger);
		}
	}
}
