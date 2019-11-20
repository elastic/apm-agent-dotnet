using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class CustomServiceNodeNameSetViaSettings : TestsBase
	{
		private const string CustomServiceNodeName = "Instance #1";

		public CustomServiceNodeNameSetViaSettings(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string> { { ConfigConsts.EnvVarNames.ServiceNodeName, CustomServiceNodeName } }) =>
			AgentConfig.ServiceNodeName = CustomServiceNodeName;

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task Test(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);
			await WaitAndVerifyReceivedDataSharedConstraints(sampleAppUrlPathData);
		}
	}
}
