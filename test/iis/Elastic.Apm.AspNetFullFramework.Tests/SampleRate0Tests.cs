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
	public class SampleRate0Tests : TestsBase
	{
		public SampleRate0Tests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper,
			envVarsToSetForSampleAppPool: new Dictionary<string, string> { { TransactionSampleRate.ToEnvironmentVariable(), "0" } })
		{ }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task Test(SampleAppUrlPathData sampleAppUrlPathDataForSampled)
		{
			var sampleAppUrlPathData = sampleAppUrlPathDataForSampled.Clone(spansCount: 0);
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.Uri, sampleAppUrlPathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(sampleAppUrlPathData, receivedData);

				AssertReceivedDataSampledStatus(receivedData, /* isSampled */ false);
			});
		}
	}
}
