using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class CustomEnvironmentViaSettings : TestsBase
	{
		private const string CustomEnvironment = "Staging";

		public CustomEnvironmentViaSettings(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string> { { ConfigConsts.EnvVarNames.Environment, CustomEnvironment } }) =>
			AgentConfig.Environment = CustomEnvironment;

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task Test(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);
			await VerifyDataReceivedFromAgent(sampleAppUrlPathData);
		}
	}
}
