using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Tests.MockApmServer;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class TestWithApmServerStopped : TestsBase
	{
		public TestWithApmServerStopped(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper, startMockApmServer: false) { }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(GenerateSampleAppUrlPathsData))]
		public async Task SampleAppShouldBeAvailableEvenWhenApmServerStopped(SampleAppUrlPathData sampleAppUrlPathData)
		{
			(await SendGetRequestToSampleApp(sampleAppUrlPathData.UrlPath)).StatusCode.Should().Be(sampleAppUrlPathData.Status);
		}
	}
}
