// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Config.ConfigurationOption;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class CustomServiceNameSetViaSettings : TestsBase
	{
		private const string CustomServiceName = "AspNetFullFramework.Tests.CustomServiceName";

		public CustomServiceNameSetViaSettings(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string> { { ServiceName.ToEnvironmentVariable(), CustomServiceName } }) =>
			AgentConfig.ServiceName = AbstractConfigurationReader.AdaptServiceName(CustomServiceName);

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task Test(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.Uri, sampleAppUrlPathData.StatusCode);
			await WaitAndVerifyReceivedDataSharedConstraints(sampleAppUrlPathData);
		}
	}
}
