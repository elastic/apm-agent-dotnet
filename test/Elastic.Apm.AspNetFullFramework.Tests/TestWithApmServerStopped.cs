using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class TestWithApmServerStopped : AspNetFullFrameworkTestsBase
	{
		public TestWithApmServerStopped(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper, false) { }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(GenerateSampleAppUrlPathsData))]
		public async Task SampleAppShouldBeAvailableEvenWhenApmServerStopped(SampleAppUrlPathData sampleAppUrlPathData) =>
			(await SendGetRequestToSampleApp(sampleAppUrlPathData.UrlPath)).StatusCode.Should().Be(sampleAppUrlPathData.Status);
	}
}
