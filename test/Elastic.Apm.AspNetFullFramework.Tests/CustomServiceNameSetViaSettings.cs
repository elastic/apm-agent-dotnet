using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class CustomServiceNameSetViaSettings : TestsBase
	{
		private const string CustomServiceName = "AspNetFullFramework.Tests.CustomServiceName";

		public CustomServiceNameSetViaSettings(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string> { { ConfigConsts.EnvVarNames.ServiceName, CustomServiceName } }) =>
			AgentConfig.ServiceName = AbstractConfigurationReader.AdaptServiceName(CustomServiceName);

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task Test(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);
			await VerifyDataReceivedFromAgent(sampleAppUrlPathData);
		}
	}
}
