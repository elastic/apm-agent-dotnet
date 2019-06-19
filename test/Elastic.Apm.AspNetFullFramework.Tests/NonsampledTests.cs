using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class NonsampledTests : TestsBase
	{
		public NonsampledTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper,
			envVarsToSetForSampleAppPool: new Dictionary<string, string>() { { ConfigConsts.EnvVarNames.TransactionSampleRate, "0" } }) { }

		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task SampleRate0(SampleAppUrlPathData sampleAppUrlPathDataForSampled)
		{
			var sampleAppUrlPathData = sampleAppUrlPathDataForSampled.Clone(spansCount: 0);
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(sampleAppUrlPathData.RelativeUrlPath, sampleAppUrlPathData.StatusCode);

			VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(sampleAppUrlPathData, receivedData);

				foreach (var transaction in receivedData.Transactions)
				{
					transaction.Context.Should().BeNull();
					transaction.SpanCount.Started.Should().Be(0);
					transaction.IsSampled.Should().BeFalse();
				}

				foreach (var error in receivedData.Errors)
				{
					error.Context.Should().BeNull();
					error.Transaction.IsSampled.Should().BeFalse();
				}
			});
		}
	}
}
