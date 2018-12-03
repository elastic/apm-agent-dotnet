using System;
using System.Reflection;
using Elastic.Agent.Core.Config;
using Elastic.Agent.Core.Logging;
using Elastic.Agent.Core.Tests.Mocks;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Elastic.Agent.Core.Tests
{
    /// <summary>
    /// Tests the configuration through environment variables
    /// </summary>
    public class EnvVarConfigTest
    {
        public EnvVarConfigTest()
        {
            //reset the static type APM.Agent
            Type staticType = typeof(Apm.Agent);
            ConstructorInfo ci = staticType.TypeInitializer;
            object[] parameters = new object[0];
            ci.Invoke(null, parameters);
          
            //unset environment variables
            Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, null);
            Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, null);
            
            Console.WriteLine("Ctor runs");
        }

        [Fact]
        public void ServerUrlsSimpleTest()
        {
            var serverUrl = "http://myServer.com:1234";
            Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
            Assert.Equal(serverUrl, Apm.Agent.Config.ServerUrls[0].OriginalString);
            Assert.Equal(serverUrl.ToLower() + "/", Apm.Agent.Config.ServerUrls[0].ToString().ToLower());
        }

        [Fact]
        public void ServerUrlsInvalidUrlTest()
        {
            var serverUrl = "InvalidUrl";
            Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
            Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), Apm.Agent.Config.ServerUrls[0].ToString());
        }

        [Fact]
        public void ServerUrlInvalidUrlLogTest()
        {
            var serverUrl = "InvalidUrl";
            Apm.Agent.SetLoggerType<TestLogger>();
            Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
            Assert.Equal(ConfigConsts.DefaultServerUri.ToString(), Apm.Agent.Config.ServerUrls[0].ToString());

            Assert.Equal($"Error Config: Failed parsing server URL from environment variable: {EnvVarConsts.ServerUrls}, value: {serverUrl}",
                         (Apm.Agent.Config.Logger as TestLogger).Lines[0]);
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


            Assert.Equal(serverUrl1, Apm.Agent.Config.ServerUrls[0].OriginalString);
            Assert.Equal(serverUrl1.ToLower() + "/", Apm.Agent.Config.ServerUrls[0].ToString().ToLower());

            Assert.Equal(serverUrl2, Apm.Agent.Config.ServerUrls[1].OriginalString);
            Assert.Equal(serverUrl2.ToLower() + "/", Apm.Agent.Config.ServerUrls[1].ToString().ToLower());
        }

        /// <summary>
        /// Sets 3 serverurls, 2 of them are valid, 1 is invalid
        /// Makes sure that the 2 valid urls are parsed and there is a logline for the invalid serverurl
        /// </summary>
        [Fact]
        public void ServerUrlsMultipleUrlsWith1InvalidUrlTest()
        {
            Apm.Agent.SetLoggerType<TestLogger>();

            var serverUrl1 = "http://myServer1.com:1234";
            var serverUrl2 = "invalidUrl";
            var serverUrl3 = "http://myServer2.com:1234";
            Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, $"{serverUrl1},{serverUrl2},{serverUrl3}");


            Assert.Equal(serverUrl1, Apm.Agent.Config.ServerUrls[0].OriginalString);
            Assert.Equal(serverUrl1.ToLower() + "/", Apm.Agent.Config.ServerUrls[0].ToString().ToLower());

            Assert.Equal(serverUrl3, Apm.Agent.Config.ServerUrls[1].OriginalString);
            Assert.Equal(serverUrl3.ToLower() + "/", Apm.Agent.Config.ServerUrls[1].ToString().ToLower());

            Assert.Equal($"Error Config: Failed parsing server URL from environment variable: {EnvVarConsts.ServerUrls}, value: {serverUrl2}",
                        (Apm.Agent.Config.Logger as TestLogger).Lines[0]);
        }

        [Fact]
        public void DefaultLogLevelTest()
        {
            Assert.Equal(LogLevel.Error, Apm.Agent.Config.LogLevel);
        }

        [Fact]
        public void SetDebugLogLevelTest()
        {
            Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Debug");
            Assert.Equal(LogLevel.Debug, Apm.Agent.Config.LogLevel);
        }

        [Fact]
        public void SetErrorLogLevelTest()
        {
            Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Error");
            Assert.Equal(LogLevel.Error, Apm.Agent.Config.LogLevel);
        }

        [Fact]
        public void SetInfoLogLevelTest()
        {
            Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Info");
            Assert.Equal(LogLevel.Info, Apm.Agent.Config.LogLevel);
        }

        [Fact]
        public void SetWarningLogLevelTest()
        {
            Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, $"Warning");
            Assert.Equal(LogLevel.Warning, Apm.Agent.Config.LogLevel);
        }
        [Fact]
        public void SetInvalidLogLevelTest()
        {
            var logLevelValue = "InvalidLogLevel";
            Apm.Agent.SetLoggerType<TestLogger>();
            Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, logLevelValue);

            Assert.Equal(LogLevel.Error, Apm.Agent.Config.LogLevel);
            Assert.Equal($"Error Config: Failed parsing log level from environment variable: {EnvVarConsts.LogLevel}, value: {logLevelValue}. Defaulting to log level 'Error'", (Apm.Agent.Config.Logger as TestLogger).Lines[0]);

        }
    }
}
