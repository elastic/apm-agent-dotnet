using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Logging;
using Elastic.Apm.ServerInfo;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class ApmServerInfoProviderTests
{
	[Fact]
	public void FillApmServerInfo_warns_on_missing_server_version()
	{
		var logger = new InMemoryBlockingLogger(LogLevel.Warning);
		var configReader = new MockConfiguration(logger);
		var service = Service.GetDefaultService(configReader, logger);
		var httpMessageHandler = new MockHttpMessageHandler((r, c) => Task.FromResult(
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8)
			}));
		var httpClient =  BackendCommUtils.BuildHttpClient(logger, configReader, service, nameof(ApmServerInfoProviderTests), httpMessageHandler);

		var callbackWasCalled = false;
		ApmServerInfoProvider.FillApmServerInfo(null, logger, configReader, httpClient, ((status, _) =>
		{
			status.Should().Be(false);
			callbackWasCalled = true;
		}));
		callbackWasCalled.Should().Be(true);
		logger.Lines.Should().Contain(line => line.Contains("Failed parsing APM Server version - version string not available"));
	}
}
