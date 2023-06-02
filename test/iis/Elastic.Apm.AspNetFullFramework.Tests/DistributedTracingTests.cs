// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.MockApmServer;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class DistributedTracingTests : TestsBase
	{
		public DistributedTracingTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task ContactPageCallsAboutPageAndExternalUrl()
		{
			var rootTxData = SampleAppUrlPaths.ContactPage;
			var childTxData = SampleAppUrlPaths.AboutPage;

			await SendGetRequestToSampleAppAndVerifyResponse(rootTxData.Uri, rootTxData.StatusCode, addTraceContextHeaders: true);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(rootTxData, receivedData);

				VerifyRootChildTransactions(receivedData, rootTxData, childTxData, out var rootTx, out _);

				receivedData.Transactions.All(n => n.Context.Request.Headers.ContainsKey("tracestate") && n.Context.Request.Headers["tracestate"] == "rojo=00f067aa0ba902b7,congo=t61rcWkgMzE").Should().BeTrue();

				var spanExternalCall =
					receivedData.Spans.Single(sp => sp.Context.Http.Url == HomeController.ChildHttpCallToExternalServiceUrl.ToString());
				VerifyHttpCallSpan(spanExternalCall, HomeController.ChildHttpCallToExternalServiceUrl, 200);

				spanExternalCall.TraceId.Should().Be(rootTx.TraceId);
				spanExternalCall.ParentId.Should().Be(rootTx.Id);
				spanExternalCall.TransactionId.Should().Be(rootTx.Id);

				spanExternalCall.ShouldOccurBetween(rootTx);
			});
		}

		[AspNetFullFrameworkFact]
		[SuppressMessage("ReSharper", "NullConditionalAssertion")]
		public async Task ContactPageCallsAboutPageAndExternalUrlWithWrappingControllerActionSpan()
		{
			const string queryString = "?" + HomeController.CaptureControllerActionAsSpanQueryStringKey + "=true";
			var rootTxData = SampleAppUrlPaths.ContactPage.Clone(
				$"{SampleAppUrlPaths.ContactPage.RelativePath}{queryString}",
				spansCount: SampleAppUrlPaths.ContactPage.SpansCount + 1);
			var childTxData = SampleAppUrlPaths.AboutPage.Clone($"{SampleAppUrlPaths.AboutPage.RelativePath}{queryString}");

			await SendGetRequestToSampleAppAndVerifyResponse(rootTxData.Uri, rootTxData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(rootTxData, receivedData);

				var rootTx = FindAndVerifyTransaction(receivedData, rootTxData);
				var childTx = FindAndVerifyTransaction(receivedData, childTxData);

				var rootTxControllerActionSpan = receivedData.Spans.Last();
				var spanCallToChildTx = receivedData.Spans.Single(sp => sp.Context?.Http?.Url == childTx.Context.Request.Url.Full);
				var spanExternalCall =
					receivedData.Spans.Single(sp => sp.Context?.Http?.Url == HomeController.ChildHttpCallToExternalServiceUrl.ToString());

				VerifySpanNameTypeSubtypeAction(rootTxControllerActionSpan, HomeController.ContactSpanPrefix);
				VerifyHttpCallSpan(spanCallToChildTx, new Uri(childTx.Context.Request.Url.Full), childTxData.StatusCode);
				VerifyHttpCallSpan(spanExternalCall, HomeController.ChildHttpCallToExternalServiceUrl, 200);

				rootTxControllerActionSpan.TraceId.Should().Be(rootTx.TraceId);
				childTx.TraceId.Should().Be(rootTx.TraceId);
				spanCallToChildTx.TraceId.Should().Be(rootTx.TraceId);
				spanExternalCall.TraceId.Should().Be(rootTx.TraceId);

				rootTxControllerActionSpan.ParentId.Should().Be(rootTx.Id);
				spanCallToChildTx.ParentId.Should().Be(rootTxControllerActionSpan.Id);
				childTx.ParentId.Should().Be(spanCallToChildTx.Id);
				spanExternalCall.ParentId.Should().Be(rootTxControllerActionSpan.Id);

				rootTxControllerActionSpan.TransactionId.Should().Be(rootTx.Id);
				spanCallToChildTx.TransactionId.Should().Be(rootTx.Id);
				spanExternalCall.TransactionId.Should().Be(rootTx.Id);

				rootTxControllerActionSpan.ShouldOccurBetween(rootTx);
				spanCallToChildTx.ShouldOccurBetween(rootTxControllerActionSpan);
				childTx.ShouldOccurBetween(spanCallToChildTx);
				spanExternalCall.ShouldOccurBetween(rootTxControllerActionSpan);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task CallReturnBadRequestTest()
		{
			var rootTxData = SampleAppUrlPaths.CallReturnBadRequestPage;
			var childTxData = SampleAppUrlPaths.ReturnBadRequestPage;

			await SendGetRequestToSampleAppAndVerifyResponse(rootTxData.Uri, rootTxData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(rootTxData, receivedData);

				VerifyRootChildTransactions(receivedData, rootTxData, childTxData, out _, out _);
			});
		}

		private static void VerifyRootChildTransactions(
			ReceivedData receivedData,
			SampleAppUrlPathData rootTxData,
			SampleAppUrlPathData childTxData,
			out TransactionDto rootTxOut,
			out TransactionDto childTxOut
		)
		{
			var rootTx = FindAndVerifyTransaction(receivedData, rootTxData);
			var childTx = FindAndVerifyTransaction(receivedData, childTxData);

			var spanCallToChildTx = receivedData.Spans.Single(sp => sp.Context.Http.Url == childTx.Context.Request.Url.Full);
			VerifyHttpCallSpan(spanCallToChildTx, new Uri(childTx.Context.Request.Url.Full), childTxData.StatusCode);

			childTx.TraceId.Should().Be(rootTx.TraceId);
			spanCallToChildTx.TraceId.Should().Be(rootTx.TraceId);

			spanCallToChildTx.ParentId.Should().Be(rootTx.Id);
			childTx.ParentId.Should().Be(spanCallToChildTx.Id);

			spanCallToChildTx.TransactionId.Should().Be(rootTx.Id);

			spanCallToChildTx.ShouldOccurBetween(rootTx);
			childTx.ShouldOccurBetween(spanCallToChildTx);

			rootTxOut = rootTx;
			childTxOut = childTx;
		}

		private static TransactionDto FindAndVerifyTransaction(ReceivedData receivedData, SampleAppUrlPathData txData)
		{
			var queryString = txData.Uri.Query;
			if (queryString.IsEmpty())
			{
				// Uri.Query returns empty string both when query string is empty ("http://host/path?") and
				// when there's no query string at all ("http://host/path") so we need a way to distinguish between these cases
				if (txData.Uri.ToString().IndexOf('?') == -1)
					queryString = null;
			}
			else if (queryString[0] == '?')
				queryString = queryString.Substring(1, queryString.Length - 1);

			var transaction = receivedData.Transactions.Single(tx => tx.Context.Request.Url.PathName == txData.Uri.AbsolutePath);
			transaction.Context.Request.Method.ToUpperInvariant().Should().Be("GET");
			transaction.Context.Request.Url.Full.Should().Be(txData.Uri.ToString());
			transaction.Context.Request.Url.PathName.Should().Be(txData.Uri.AbsolutePath);
			transaction.Context.Request.Url.Search.Should().Be(queryString);

			transaction.Context.Response.Finished.Should().BeTrue();
			transaction.Context.Response.StatusCode.Should().Be(txData.StatusCode);
			if (txData.StatusCode == (int)HttpStatusCode.OK)
			{
				var caseInsensitiveResponseHeaders =
					new Dictionary<string, string>(transaction.Context.Response.Headers, StringComparer.OrdinalIgnoreCase);
				caseInsensitiveResponseHeaders["Content-Type"].Should().Be("text/html; charset=utf-8");
			}

			transaction.Context.Labels.Should().BeNull();
			transaction.Context.User.Should().BeNull();

			transaction.IsSampled.Should().BeTrue();
			transaction.Name.Should().Be($"GET {txData.RelativePath}");
			transaction.SpanCount.Started.Should().Be(txData.SpansCount);
			transaction.SpanCount.Dropped.Should().Be(0);

			return transaction;
		}

		private static void VerifyHttpCallSpan(SpanDto span, Uri url, int statusCode)
		{
			span.Context.Db.Should().BeNull();
			span.Context.Labels.Should().BeNull();

			span.Context.Http.Method.Should().Be("GET");
			span.Context.Http.StatusCode.Should().Be(statusCode);
			span.Context.Http.Url.Should().Be(url.ToString());

			span.Type.Should().Be(ApiConstants.TypeExternal);
			span.Subtype.Should().Be(ApiConstants.SubtypeHttp);
			span.Action.Should().BeNull();

			span.Name.Should().Be($"GET {url.Host}");

			span.Context.Destination.Address.Should().Be(url.Host);
			span.Context.Destination.Port.Should().Be(url.Port);
		}
	}
}
