// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.TestHelpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class ErrorsTests : TestsBase
	{
		public ErrorsTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task CustomSpanThrowsTest()
		{
			var errorPageData = SampleAppUrlPaths.CustomSpanThrowsExceptionPage;
			await SendGetRequestToSampleAppAndVerifyResponse(errorPageData.Uri, errorPageData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(errorPageData, receivedData);

				var transaction = receivedData.Transactions.First();
				transaction.Context.Request.Url.Search.Should().BeNull();
				transaction.IsSampled.Should().BeTrue();
				transaction.Outcome.Should().Be(Outcome.Failure);

				var span = receivedData.Spans.First();
				VerifySpanNameTypeSubtypeAction(span, HomeController.TestSpanPrefix);
				span.TraceId.Should().Be(transaction.TraceId);
				span.TransactionId.Should().Be(transaction.Id);
				span.ParentId.Should().Be(transaction.Id);
				span.ShouldOccurBetween(transaction);

				receivedData.Errors.Count.Should().Be(2);
				receivedData.Errors.ForEach(error =>
				{
					error.Exception.Message.Should().Be(HomeController.ExceptionMessage);
					error.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
					error.Exception.StackTrace.Should().Contain(f => f.Function == HomeController.CustomSpanThrowsInternalMethodName);
					VerifyErrorShared(error, transaction);
				});

				VerifySpanError(receivedData.Errors.First(), span, transaction);

				VerifyTransactionError(receivedData.Errors.Skip(1).First(), transaction);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task HttpCallWithResponseForbidden()
		{
			var pageData = SampleAppUrlPaths.ChildHttpSpanWithResponseForbiddenPage;
			await SendGetRequestToSampleAppAndVerifyResponse(pageData.Uri, pageData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(pageData, receivedData);

				receivedData.Spans.First().Should().NotBeNull();
				receivedData.Spans.First().Context.Http.Should().NotBeNull();
				receivedData.Spans.First().Context.Http.StatusCode.Should().Be(403);
				receivedData.Spans.First().Context.Http.Method.Should().Be("GET");
				receivedData.Spans.First().Context.Http.Url.Should().Be(HomeController.ChildHttpSpanWithResponseForbiddenUrl.ToString());
				receivedData.Spans.First().Context.Destination.Address.Should().Be(HomeController.ChildHttpSpanWithResponseForbiddenUrl.Host);
				receivedData.Spans.First().Context.Destination.Port.Should().Be(HomeController.ChildHttpSpanWithResponseForbiddenUrl.Port);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task CustomChildSpanThrowsTest()
		{
			var errorPageData = SampleAppUrlPaths.CustomChildSpanThrowsExceptionPage;
			await SendGetRequestToSampleAppAndVerifyResponse(errorPageData.Uri, errorPageData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(errorPageData, receivedData);

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
				receivedData.Errors.ForEach(error =>
				{
					error.Exception.Message.Should().Be(HomeController.ExceptionMessage);
					error.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
					error.Exception.StackTrace.Should().Contain(f => f.Function == HomeController.CustomSpanThrowsInternalMethodName);
					VerifyErrorShared(error, transaction);
				});

				// all the errors except the last one are the exception that propagated out of spans
				// the last error is the exception that propagated out of transaction
				receivedData.Errors.Take(errorPageData.ErrorsCount - 1)
					.ForEachIndexed((error, i) => { VerifySpanError(error, receivedData.Spans[i], transaction); });

				var lastError = receivedData.Errors.Skip(errorPageData.ErrorsCount - 1).First();
				VerifyTransactionError(lastError, transaction);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task PageThatDoesNotExit_test()
		{
			var pageData = SampleAppUrlPaths.PageThatDoesNotExist;
			await SendGetRequestToSampleAppAndVerifyResponse(pageData.Uri, pageData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(pageData, receivedData);

				var transaction = receivedData.Transactions.First();
				var error = receivedData.Errors.First();

				VerifyTransactionError(error, transaction);

				error.Exception.Type.Should().Be("System.Web.HttpException");
				error.Exception.Message.Should().ContainAll(pageData.Uri.PathAndQuery, "not found");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task InnerException_Captured_When_HttpUnhandledException_Thrown()
		{
			var errorPageData = SampleAppUrlPaths.WebformsExceptionPage;
			await SendGetRequestToSampleAppAndVerifyResponse(errorPageData.Uri, errorPageData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(errorPageData, receivedData);

				var transaction = receivedData.Transactions.First();
				transaction.Name.Should().Be("GET " + errorPageData.Uri.AbsolutePath);
				transaction.Context.Request.Url.Search.Should().BeNull();
				transaction.IsSampled.Should().BeTrue();
				transaction.Outcome.Should().Be(Outcome.Failure);

				receivedData.Errors.Count.Should().Be(1);
				var error = receivedData.Errors.First();
				error.Exception.Type.Should().Be(typeof(DivideByZeroException).FullName);
				error.Exception.Message.Should().Be("Attempted to divide by zero.");
				// happens in System.Web.UI.Page.Render
				error.Exception.StackTrace.Should().Contain(f => f.Function == "Render");
				VerifyErrorShared(error, transaction);
			});
		}

		private static void VerifyErrorShared(ErrorDto error, TransactionDto transaction)
		{
			error.TraceId.Should().Be(transaction.TraceId);
			error.TransactionId.Should().Be(transaction.Id);
			error.Transaction.Type.Should().Be(transaction.Type);
			error.Transaction.IsSampled.Should().Be(transaction.IsSampled);
		}

		private static void VerifyTransactionError(ErrorDto error, TransactionDto transaction)
		{
			VerifyErrorShared(error, transaction);

			error.ParentId.Should().Be(transaction.Id);
			error.ShouldOccurBetween(transaction);
		}

		private static void VerifySpanError(ErrorDto error, SpanDto span, TransactionDto transaction)
		{
			VerifyErrorShared(error, transaction);

			error.ParentId.Should().Be(span.Id);
			error.ShouldOccurBetween(span);
		}
	}
}
