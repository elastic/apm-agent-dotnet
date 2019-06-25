using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class TestsWithSampleAppLogDisabled : TestsBase
	{
		public TestsWithSampleAppLogDisabled(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper, sampleAppLogEnabled: false) { }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task TestVariousPages(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);

			VerifyDataReceivedFromAgent(sampleAppUrlPathData);
		}
	}
}
