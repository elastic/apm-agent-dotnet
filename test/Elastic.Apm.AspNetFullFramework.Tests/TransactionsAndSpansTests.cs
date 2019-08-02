using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class TransactionsAndSpansTests : TestsBase
	{
		public TransactionsAndSpansTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task WithDefaultSettings(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);

			VerifyDataReceivedFromAgent(sampleAppUrlPathData);
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
			var homePageAndQueryString = SampleAppUrlPaths.HomePage.Clone(SampleAppUrlPaths.HomePage.RelativeUrlPath + $"?{queryString}");
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(homePageAndQueryString.RelativeUrlPath, homePageAndQueryString.StatusCode);

			VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(homePageAndQueryString, receivedData);

				receivedData.Transactions.Count.Should().Be(1);

				receivedData.Transactions.First().Context.Request.Url.Search.Should().Be(queryString);
			});
		}
	}
}
