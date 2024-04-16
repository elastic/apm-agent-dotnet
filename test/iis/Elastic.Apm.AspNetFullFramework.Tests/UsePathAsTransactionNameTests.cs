// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Config.ConfigurationOption;

namespace Elastic.Apm.AspNetFullFramework.Tests;

[Collection(Consts.AspNetFullFrameworkTestsCollection)]
public class UsePathAsTransactionNameTests : TestsBase
{
	public UsePathAsTransactionNameTests(ITestOutputHelper xUnitOutputHelper)
		: base(xUnitOutputHelper,
			envVarsToSetForSampleAppPool: new Dictionary<string, string>
			{
				{ UsePathAsTransactionName.ToEnvironmentVariable(), "false" }
			})
	{ }

	[AspNetFullFrameworkFact]
	public async Task Name_ShouldBeUnknown_WhenWebformsPage_AndUsePathAsTransactionName_IsDisabled()
	{
		var pathData = SampleAppUrlPaths.WebformsPage;
		await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

		await WaitAndCustomVerifyReceivedData(receivedData =>
		{
			receivedData.Transactions.Count.Should().Be(1);
			var transaction = receivedData.Transactions.Single();
			transaction.Name.Should().Be("GET unknown route");
		});
	}
}
