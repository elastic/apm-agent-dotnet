using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.MockApmServer;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class ErrorsTests : TestsBase
	{
		public ErrorsTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task CustomSpanThrowsTest()
		{
			var errorPageData = SampleAppUrlPaths.CustomSpanThrowsExceptionPage;
			await SendGetRequestToSampleAppAndVerifyResponse(errorPageData.RelativeUrlPath, errorPageData.StatusCode);

			await VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(errorPageData, receivedData);

				var transaction = receivedData.Transactions.First();
				transaction.Context.Request.Url.Search.Should().BeNull();
				transaction.IsSampled.Should().BeTrue();

				var span = receivedData.Spans.First();
				span.Type.Should().Be(HomeController.TestSpanType);
				span.Subtype.Should().Be(HomeController.TestSpanSubtype);
				span.Action.Should().Be(HomeController.TestSpanAction);
				span.Name.Should().Be(HomeController.TestSpanName);
				span.TraceId.Should().Be(transaction.TraceId);
				span.TransactionId.Should().Be(transaction.Id);
				span.ParentId.Should().Be(transaction.Id);
				span.ShouldOccurBetween(transaction);

				var stackTraceFrame = span.StackTrace.Single(f => f.Function == HomeController.CustomSpanThrowsMethodName);
				stackTraceFrame.Module.Should().StartWith("AspNetFullFrameworkSampleApp, Version=");

				receivedData.Errors.Count.Should().Be(1);
				var error = receivedData.Errors.First();
				error.Exception.Message.Should().Be(HomeController.ExceptionMessage);
				error.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
				error.Exception.Stacktrace.Should().Contain(f => f.Function == HomeController.CustomSpanThrowsInternalMethodName);
				error.TraceId.Should().Be(transaction.TraceId);
				error.TransactionId.Should().Be(transaction.Id);
				error.Transaction.Type.Should().Be(ApiConstants.TypeRequest);
				error.Transaction.IsSampled.Should().BeTrue();
				error.ParentId.Should().Be(span.Id);
				error.ShouldOccurBetween(span);
			});
		}
	}
}
