// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class TransactionIgnoreUrlsTest : TestsBase
	{
		public TransactionIgnoreUrlsTest(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string> { { ConfigConsts.EnvVarNames.TransactionIgnoreUrls, "/home" } }) { }

		[AspNetFullFrameworkFact]
		public async Task Test()
		{
			var sampleAppUrlPathData = SampleAppUrlPaths.HomePage;
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Should().BeEmpty();
				receivedData.Spans.Should().BeEmpty();
				receivedData.Errors.Should().BeEmpty();
			});
		}
	}
}
