// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class MvcAreasTest : TestsBase
	{
		public MvcAreasTest(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task TransactionName_Should_Include_Area()
		{
			var sampleAppUrlPathData = SampleAppUrlPaths.MyAreaHomePage;
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.Uri, sampleAppUrlPathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET MyArea/Home/Index");
			});
		}
	}
}
