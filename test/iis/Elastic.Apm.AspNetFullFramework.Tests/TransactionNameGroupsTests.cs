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
public class TransactionNameGroupsTests : TestsBase
{
	// NOTE: The main situation where ASP.NET instrumentation may produce high-cardinality transaction names is when the application
	// is using WCF. In such cases, the transaction name will default to being the path of the request. We can't easily spin up WCF in
	// CI so this test simply ensures that any transaction name group configuration is working as expected by setting a transaction
	// group name that matches the transaction name of a request that hits an MVC controller action from an Area.

	private const string GroupName = "GET MyArea/*";

	public TransactionNameGroupsTests(ITestOutputHelper xUnitOutputHelper)
		: base(xUnitOutputHelper,
			envVarsToSetForSampleAppPool: new Dictionary<string, string>
			{
				{ TransactionNameGroups.ToEnvironmentVariable(), GroupName }
			})
	{ }

	[AspNetFullFrameworkFact]
	public async Task TransactionName_ShouldUseMatchedTransactionGroupName()
	{
		var pathData = SampleAppUrlPaths.MyAreaHomePage;
		await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

		await WaitAndCustomVerifyReceivedData(receivedData =>
		{
			receivedData.Transactions.Count.Should().Be(1);
			var transaction = receivedData.Transactions.Single();
			transaction.Name.Should().Be(GroupName);
		});
	}
}
