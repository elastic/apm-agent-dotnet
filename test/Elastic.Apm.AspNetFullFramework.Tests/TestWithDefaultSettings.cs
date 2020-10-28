// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class TestWithDefaultSettings : TestsBase
	{
		public TestWithDefaultSettings(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task TestVariousPages(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.Uri, sampleAppUrlPathData.StatusCode);

			await WaitAndVerifyReceivedDataSharedConstraints(sampleAppUrlPathData);
		}

		[AspNetFullFrameworkTheory]
		[InlineData("key_1=value_1")] // 1 key/value pair
		[InlineData("key_1=value_1&key_2=value_2")] // 2 key/value pairs
		[InlineData("key_1=value_1&key_2=value_2&key%203=value%203")] // 3 key/value pairs
		[InlineData("")] // empty
		[InlineData("key_1=&key_2=value_2")] // key without value
		[InlineData("=value_1")] // value without key
		[InlineData("?")]
		// key "?" without value
		public async Task QueryStringTests(string queryString)
		{
			var homePageAndQueryString = SampleAppUrlPaths.HomePage.Clone(SampleAppUrlPaths.HomePage.RelativePath + $"?{queryString}");
			await SendGetRequestToSampleAppAndVerifyResponse(homePageAndQueryString.Uri, homePageAndQueryString.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(homePageAndQueryString, receivedData);

				receivedData.Transactions.Count.Should().Be(1);

				receivedData.Transactions.First().Context.Request.Url.Search.Should().Be(queryString);
			});
		}
	}
}
