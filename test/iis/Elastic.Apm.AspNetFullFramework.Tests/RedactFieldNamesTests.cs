// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class RedactFieldNamesTests : TestsBase
	{
		public RedactFieldNamesTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper)
		{
		}

		[AspNetFullFrameworkFact]
		public async Task SanitizesCookieHeaders()
		{
			var pathData = SampleAppUrlPaths.CookiesPage;

			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode, false);
			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				receivedData.Transactions.Should().ContainSingle();
				receivedData.Transactions[0].Context.Should().NotBeNull();
				receivedData.Transactions[0].Context.Request.Should().NotBeNull();

				receivedData.Transactions[0].Context.Response.Headers["Set-Cookie"].Should().Be(Apm.Consts.Redacted);
			}, false);

			ClearState();

			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode, false);
			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				receivedData.Transactions.Should().ContainSingle();
				receivedData.Transactions[0].Context.Should().NotBeNull();
				receivedData.Transactions[0].Context.Request.Should().NotBeNull();

				receivedData.Transactions[0].Context.Request.Headers["Cookie"].Should().Be($"password={Apm.Consts.Redacted}; SafeCookie=This is safe to record and should not be redacted.");
			}, false);
		}
	}
}
