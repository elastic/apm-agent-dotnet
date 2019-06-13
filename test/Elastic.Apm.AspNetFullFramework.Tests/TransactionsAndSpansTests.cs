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
		[MemberData(nameof(GenerateSampleAppUrlPathsData))]
		public async Task VerifyExpectedNumberOfTransactionsAndSpansForEachPage(SampleAppUrlPathData sampleAppUrlPathData)
		{
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(sampleAppUrlPathData.UrlPath, sampleAppUrlPathData.Status);

			VerifyPayloadFromAgent(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(sampleAppUrlPathData.TransactionsCount);
				receivedData.Spans.Count.Should().Be(sampleAppUrlPathData.SpansCount);
			});
		}
	}
}
