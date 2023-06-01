using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class LabelsTests : TestsBase
	{
		public LabelsTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task Test()
		{
			await SendGetRequestToSampleAppAndVerifyResponse(SampleAppUrlPaths.LabelsTest.Uri, SampleAppUrlPaths.LabelsTest.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(SampleAppUrlPaths.LabelsTest, receivedData);

				var transaction = receivedData.Transactions.First();
				transaction.Context.Labels.MergedDictionary.Should().HaveCount(3);
				transaction.Context.Labels.MergedDictionary["TransactionStringLabel"].Value.Should().Be("foo");
				transaction.Context.Labels.MergedDictionary["TransactionNumberLabel"].Value.Should().Be(42);
				transaction.Context.Labels.MergedDictionary["TransactionBoolLabel"].Value.Should().Be(true);

				var span = receivedData.Spans.First();
				span.Context.Labels.MergedDictionary.Should().HaveCount(3);
				span.Context.Labels.MergedDictionary["SpanStringLabel"].Value.Should().Be("foo");
				span.Context.Labels.MergedDictionary["SpanNumberLabel"].Value.Should().Be(42);
				span.Context.Labels.MergedDictionary["SpanBoolLabel"].Value.Should().Be(true);
			});
		}
	}
}
