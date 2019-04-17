using System;
using System.Linq;
using System.Threading;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Tests the configuration through environment variables
	/// </summary>
	public class EnvVarConfigTest : IDisposable
	{
		[Fact]
		public void ServerUrlsSimpleTest()
		{
			var serverUrl = "http://myServer.com:1234";
			var agent = new ApmAgent(new TestAgentComponents(serverUrls: serverUrl));
			agent.ConfigurationReader.ServerUrls[0].OriginalString.Should().Be(serverUrl);
			var rootedUrl = serverUrl + "/";
			rootedUrl.Should().BeEquivalentTo(agent.ConfigurationReader.ServerUrls[0].AbsoluteUri);
		}

		[Fact]
		public void ServerUrlsInvalidUrlTest()
		{
			var serverUrl = "InvalidUrl";
			var agent = new ApmAgent(new TestAgentComponents(serverUrls: serverUrl));
			agent.ConfigurationReader.ServerUrls[0].Should().Be(ConfigConsts.DefaultServerUri);
		}

		[Fact]
		public void ServerUrlInvalidUrlLogTest()
		{
			var serverUrl = "InvalidUrl";
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger, serverUrl));
			agent.ConfigurationReader.ServerUrls[0].Should().Be(ConfigConsts.DefaultServerUri);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					$"{{{nameof(TestAgentConfigurationReader)}}}",
					"Failed parsing server URL from",
					TestAgentConfigurationReader.Origin,
					ConfigConsts.EnvVarNames.ServerUrls,
					serverUrl
				);
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

			var parsedUrls = agent.ConfigurationReader.ServerUrls;
			parsedUrls[0].OriginalString.Should().Be(serverUrl1);
			parsedUrls[0].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl1}/");

			parsedUrls[1].OriginalString.Should().Be(serverUrl2);
			parsedUrls[1].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl2}/");
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

			var parsedUrls = agent.ConfigurationReader.ServerUrls;
			parsedUrls.Should().NotBeEmpty().And.HaveCount(2, "seeded 3 but one was invalid");
			parsedUrls[0].OriginalString.Should().Be(serverUrl1);
			parsedUrls[0].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl1}/");

			parsedUrls[1].OriginalString.Should().Be(serverUrl3);
			parsedUrls[1].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl3}/");

			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					$"{{{nameof(TestAgentConfigurationReader)}}}",
					"Failed parsing server URL from",
					TestAgentConfigurationReader.Origin,
					ConfigConsts.EnvVarNames.ServerUrls,
					serverUrl2
				);
		}

		[Fact]
		public void SecretTokenSimpleTest()
		{
			var secretToken = "secretToken";
			var agent = new ApmAgent(new TestAgentComponents(secretToken: secretToken));
			agent.ConfigurationReader.SecretToken.Should().Be(secretToken);
		}

		[Fact]
		public void DefaultCaptureHeadersTest() => Agent.Config.CaptureHeaders.Should().Be(true);

		[Fact]
		public void SetCaptureHeadersTest()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.CaptureHeaders, "false");
			var config = new EnvironmentConfigurationReader();
			config.CaptureHeaders.Should().Be(false);
		}

		[Fact]
		public void DefaultTransactionSampleRateTest() => Agent.Config.TransactionSampleRate.Should().Be(1.0);

		[Fact]
		public void SetTransactionSampleRateTest()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.TransactionSampleRate, "0.789");
			var config = new EnvironmentConfigurationReader();
			config.TransactionSampleRate.Should().Be(0.789);
		}

		[Fact]
		public void DefaultLogLevelTest() => Agent.Config.LogLevel.Should().Be(LogLevel.Error);

		[Fact]
		public void SetDebugLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Debug"));
			agent.ConfigurationReader.LogLevel.Should().Be(LogLevel.Debug);
			agent.Logger.Level.Should().Be(LogLevel.Debug);
		}

		[Fact]
		public void SetErrorLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Error"));
			agent.ConfigurationReader.LogLevel.Should().Be(LogLevel.Error);
			agent.Logger.Level.Should().Be(LogLevel.Error);
		}

		[Fact]
		public void SetInfoLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Information"));
			agent.ConfigurationReader.LogLevel.Should().Be(LogLevel.Information);
			agent.Logger.Level.Should().Be(LogLevel.Information);
		}

		[Fact]
		public void SetWarningLogLevelTest()
		{
			var agent = new ApmAgent(new TestAgentComponents("Warning"));
			agent.ConfigurationReader.LogLevel.Should().Be(LogLevel.Warning);
			agent.Logger.Level.Should().Be(LogLevel.Warning);
		}

		[Fact]
		public void SetInvalidLogLevelTest()
		{
			var logLevelValue = "InvalidLogLevel";
			var agent = new ApmAgent(new TestAgentComponents(logLevelValue));
			var logger = agent.Logger as TestLogger;

			agent.ConfigurationReader.LogLevel.Should().Be(LogLevel.Error);
			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					$"{{{nameof(TestAgentConfigurationReader)}}}",
					"Failed parsing log level from",
					TestAgentConfigurationReader.Origin,
					ConfigConsts.EnvVarNames.LogLevel,
					"Defaulting to "
				);
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
			var serviceName = agent.Service.Name;
			serviceName.Should().NotBeNullOrWhiteSpace();
			serviceName.Should().NotContain(".");
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
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

			agent.Service.Name.Should().Be(serviceName);
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
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });


			agent.Service.Name.Should().Be(serviceName.Replace('.', '_'));
			agent.Service.Name.Should().NotContain(".");
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

			AbstractConfigurationReader.IsMsOrElastic(elasticToken).Should().BeTrue();

			AbstractConfigurationReader.IsMsOrElastic(new byte[] { 0 }).Should().BeFalse();
			AbstractConfigurationReader.IsMsOrElastic(new byte[] { }).Should().BeFalse();

			AbstractConfigurationReader
				.IsMsOrElastic(new[]
				{
					elasticToken[0], mscorlibToken[1], elasticToken[2],
					mscorlibToken[3], elasticToken[4], mscorlibToken[5], elasticToken[6], mscorlibToken[7]
				})
				.Should()
				.BeFalse();
		}

		/// <summary>
		/// Makes sure that the <see cref="EnvironmentConfigurationReader" /> logs
		/// in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServerUrls, "localhost"); //invalid, it should be "http://localhost"
			var testLogger = new TestLogger();
			var config = new EnvironmentConfigurationReader(testLogger);
			var serverUrl = config.ServerUrls.FirstOrDefault();

			serverUrl.Should().NotBeNull();
			testLogger.Lines.Should().NotBeEmpty();
		}

		public void Dispose() => Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServerUrls, null);
	}
}
