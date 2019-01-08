using System;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
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
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
			Assert.Equal(serverUrl, Agent.Config.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl.ToLower() + "/", Agent.Config.ServerUrls[0].ToString().ToLower());
		}

		[Fact]
		public void ServerUrlsInvalidUrlTest()
		{
			var serverUrl = "InvalidUrl";
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
			Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), Agent.Config.ServerUrls[0].ToString());
		}

		[Fact]
		public void ServerUrlInvalidUrlLogTest()
		{
			var serverUrl = "InvalidUrl";
			Agent.SetLoggerType<TestLogger>();
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
			Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), Agent.Config.ServerUrls[0].ToString());

			Assert.Equal($"Error Config: Failed parsing server URL from environment variable: {EnvVarConsts.ServerUrls}, value: {serverUrl}",
				(Agent.Config.Logger as TestLogger).Lines[0]);
		}

		/// <summary>
		/// Sets 2 servers and makes sure that they are all parsed
		/// </summary>
		[Fact]
		public void ServerUrlsMultipleUrlsTest()
		{
			var serverUrl1 = "http://myServer1.com:1234";
			var serverUrl2 = "http://myServer2.com:1234";
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, $"{serverUrl1},{serverUrl2}");


			Assert.Equal(serverUrl1, Agent.Config.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl1.ToLower() + "/", Agent.Config.ServerUrls[0].ToString().ToLower());

			Assert.Equal(serverUrl2, Agent.Config.ServerUrls[1].OriginalString);
			Assert.Equal(serverUrl2.ToLower() + "/", Agent.Config.ServerUrls[1].ToString().ToLower());
		}

		/// <summary>
		/// Sets 3 serverurls, 2 of them are valid, 1 is invalid
		/// Makes sure that the 2 valid urls are parsed and there is a logline for the invalid serverurl
		/// </summary>
		[Fact]
		public void ServerUrlsMultipleUrlsWith1InvalidUrlTest()
		{
			Agent.SetLoggerType<TestLogger>();

			var serverUrl1 = "http://myServer1.com:1234";
			var serverUrl2 = "invalidUrl";
			var serverUrl3 = "http://myServer2.com:1234";
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, $"{serverUrl1},{serverUrl2},{serverUrl3}");


			Assert.Equal(serverUrl1, Agent.Config.ServerUrls[0].OriginalString);
			Assert.Equal(serverUrl1.ToLower() + "/", Agent.Config.ServerUrls[0].ToString().ToLower());

			Assert.Equal(serverUrl3, Agent.Config.ServerUrls[1].OriginalString);
			Assert.Equal(serverUrl3.ToLower() + "/", Agent.Config.ServerUrls[1].ToString().ToLower());

			Assert.Equal($"Error Config: Failed parsing server URL from environment variable: {EnvVarConsts.ServerUrls}, value: {serverUrl2}",
				(Agent.Config.Logger as TestLogger).Lines[0]);
		}

		[Fact]
		public void DefaultLogLevelTest() => Assert.Equal(LogLevel.Error, Agent.Config.LogLevel);

		[Fact]
		public void SetDebugLogLevelTest()
		{
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Debug");
			Assert.Equal(LogLevel.Debug, Agent.Config.LogLevel);
		}

		[Fact]
		public void SetErrorLogLevelTest()
		{
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Error");
			Assert.Equal(LogLevel.Error, Agent.Config.LogLevel);
		}

		[Fact]
		public void SetInfoLogLevelTest()
		{
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Info");
			Assert.Equal(LogLevel.Info, Agent.Config.LogLevel);
		}

		[Fact]
		public void SetWarningLogLevelTest()
		{
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Warning");
			Assert.Equal(LogLevel.Warning, Agent.Config.LogLevel);
		}

		[Fact]
		public void SetInvalidLogLevelTest()
		{
			var logLevelValue = "InvalidLogLevel";
			Agent.SetLoggerType<TestLogger>();
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, logLevelValue);

			Assert.Equal(LogLevel.Error, Agent.Config.LogLevel);
			Assert.Equal(
				$"Error Config: Failed parsing log level from environment variable: {EnvVarConsts.LogLevel}, value: {logLevelValue}. Defaulting to log level 'Error'",
				(Agent.Config.Logger as TestLogger).Lines[0]);
		}
	}
}
