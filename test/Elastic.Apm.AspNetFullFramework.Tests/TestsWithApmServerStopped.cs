using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class TestsWithApmServerStopped : TestsBase
	{
		public TestsWithApmServerStopped(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper, false) { }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task SampleAppShouldBeAvailableEvenWhenApmServerStopped(SampleAppUrlPathData sampleAppUrlPathData) =>
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);
	}
}
