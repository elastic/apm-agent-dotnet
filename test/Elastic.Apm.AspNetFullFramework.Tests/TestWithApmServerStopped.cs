using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public class TestWithApmServerStopped : TestsBase
	{
		public TestWithApmServerStopped() => _mockApmServerSingleton.StopServer();

		[Theory]
		[InlineData("", 200)]
		[InlineData(Consts.SampleApp.contactPageRelativePath, 200)]
		[InlineData("Dummy_nonexistent_path", 404)]
		public async Task SampleAppShouldBeAvailableEvenWhenApmServerStopped(string urlPathToTest, int expectedResponseStatusCode)
		{
			var httpClient = new HttpClient();
			var response = await httpClient.GetAsync(Consts.SampleApp.rootUri + "/" + urlPathToTest);
			response.StatusCode.Should().Be(expectedResponseStatusCode);
		}
	}
}
