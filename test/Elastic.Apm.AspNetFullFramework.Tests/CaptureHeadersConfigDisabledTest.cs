using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class CaptureHeadersConfigDisabledTest : TestsBase
	{
		public CaptureHeadersConfigDisabledTest(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string> { { ConfigConsts.EnvVarNames.CaptureHeaders, "false" } }) =>
			AgentConfig.CaptureHeaders = false;

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task Test(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);

			VerifyDataReceivedFromAgent(sampleAppUrlPathData);
		}
	}
}
