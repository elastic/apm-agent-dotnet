using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.TestHelpers;
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
				VerifySpanNameTypeSubtypeAction(span, HomeController.TestSpanPrefix);
				span.TraceId.Should().Be(transaction.TraceId);
				span.TransactionId.Should().Be(transaction.Id);
				span.ParentId.Should().Be(transaction.Id);
				span.ShouldOccurBetween(transaction);

				receivedData.Errors.Count.Should().Be(1);
				var error = receivedData.Errors.First();
				error.Exception.Message.Should().Be(HomeController.ExceptionMessage);
				error.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
				error.Exception.StackTrace.Should().Contain(f => f.Function == HomeController.CustomSpanThrowsInternalMethodName);
				error.TraceId.Should().Be(transaction.TraceId);
				error.TransactionId.Should().Be(transaction.Id);
				error.Transaction.Type.Should().Be(ApiConstants.TypeRequest);
				error.Transaction.IsSampled.Should().BeTrue();
				error.ParentId.Should().Be(span.Id);
				error.ShouldOccurBetween(span);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task HttpCallWithResponseForbidden()
		{
			var forbidResponsePageData = SampleAppUrlPaths.ForbidHttpResponsePageDescriptionPage;
			await SendGetRequestToSampleAppAndVerifyResponse(forbidResponsePageData.RelativeUrlPath, forbidResponsePageData.StatusCode);

			await VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(forbidResponsePageData, receivedData);

				receivedData.Spans.First().Should().NotBeNull();
				receivedData.Spans.First().Context.Http.Should().NotBeNull();
				receivedData.Spans.First().Context.Http.StatusCode.Should().Be(403);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task CustomChildSpanThrowsTest()
		{
			var errorPageData = SampleAppUrlPaths.CustomChildSpanThrowsExceptionPage;
			await SendGetRequestToSampleAppAndVerifyResponse(errorPageData.RelativeUrlPath, errorPageData.StatusCode);

			await VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(errorPageData, receivedData);

				var transaction = receivedData.Transactions.First();
				transaction.Context.Request.Url.Search.Should().BeNull();
				transaction.IsSampled.Should().BeTrue();

				receivedData.Spans.Should().HaveCount(errorPageData.SpansCount);

				errorPageData.SpansCount.Repeat(i =>
				{
					var span = receivedData.Spans[i];
					span.TraceId.Should().Be(transaction.TraceId);
					span.TransactionId.Should().Be(transaction.Id);
				});

				var childSpan = receivedData.Spans[0];
				var parentSpan = receivedData.Spans[1];

				VerifySpanNameTypeSubtypeAction(childSpan, HomeController.TestChildSpanPrefix);
				childSpan.ParentId.Should().Be(parentSpan.Id);
				childSpan.ShouldOccurBetween(parentSpan);

				VerifySpanNameTypeSubtypeAction(parentSpan, HomeController.TestSpanPrefix);
				parentSpan.ParentId.Should().Be(transaction.Id);
				parentSpan.ShouldOccurBetween(transaction);

				receivedData.Errors.Should().HaveCount(errorPageData.ErrorsCount);
				errorPageData.ErrorsCount.Repeat(i =>
				{
					var error = receivedData.Errors[i];
					var span = receivedData.Spans[i];
					error.Exception.Message.Should().Be(HomeController.ExceptionMessage);
					error.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
					error.Exception.StackTrace.Should().Contain(f => f.Function == HomeController.CustomSpanThrowsInternalMethodName);
					error.TraceId.Should().Be(transaction.TraceId);
					error.TransactionId.Should().Be(transaction.Id);
					error.Transaction.Type.Should().Be(ApiConstants.TypeRequest);
					error.Transaction.IsSampled.Should().BeTrue();
					error.ParentId.Should().Be(span.Id);
					error.ShouldOccurBetween(span);
				});
			});
		}
	}
}
