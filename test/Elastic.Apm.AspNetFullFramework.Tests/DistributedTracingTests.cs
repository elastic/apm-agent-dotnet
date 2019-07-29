using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
	public class DistributedTracingTests : TestsBase
	{
		public DistributedTracingTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task ContactPageCallsAboutPageAndExternalUrl()
		{
			var rootTxData = SampleAppUrlPaths.ContactPage;
			var childTxData = SampleAppUrlPaths.AboutPage;

			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(rootTxData.RelativeUrlPath, rootTxData.StatusCode);

			VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(rootTxData, receivedData);

				VerifyRootChildTransactions(receivedData, rootTxData, childTxData, out var rootTx, out _);

				var spanExternalCall =
					receivedData.Spans.Single(sp => sp.Context.Http.Url == HomeController.ChildHttpCallToExternalServiceUrl.ToString());
				VerifySpan(spanExternalCall, HomeController.ChildHttpCallToExternalServiceUrl.ToString(), 200);

				spanExternalCall.TraceId.Should().Be(rootTx.TraceId);
				spanExternalCall.ParentId.Should().Be(rootTx.Id);
				spanExternalCall.TransactionId.Should().Be(rootTx.Id);

				spanExternalCall.ShouldOccurBetween(rootTx);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task CallReturnBadRequestTest()
		{
			var rootTxData = SampleAppUrlPaths.CallReturnBadRequestPage;
			var childTxData = SampleAppUrlPaths.ReturnBadRequestPage;

			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(rootTxData.RelativeUrlPath, rootTxData.StatusCode);

			VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(rootTxData, receivedData);

				VerifyRootChildTransactions(receivedData, rootTxData, childTxData, out _, out _);
			});
		}

		private void VerifyRootChildTransactions(
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
			VerifySpan(spanCallToChildTx, childTx.Context.Request.Url.Full, childTxData.StatusCode);

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

		private TransactionDto FindAndVerifyTransaction(ReceivedData receivedData, SampleAppUrlPathData txData)
		{
			var txUrlPath = Consts.SampleApp.RootUrlPath + "/" + txData.RelativeUrlPath;
			var transaction = receivedData.Transactions.Single(tx => tx.Name == $"GET {txUrlPath}");
			transaction.Context.Request.Method.ToUpperInvariant().Should().Be("GET");
			transaction.Context.Request.Url.Full.Should().Be("http://" + Consts.SampleApp.Host + txUrlPath);
			transaction.Context.Request.Url.PathName.Should().Be(txUrlPath);
			transaction.Context.Request.Url.Search.Should().BeNull();

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
			transaction.Name.Should().Be($"GET {txUrlPath}");
			transaction.SpanCount.Started.Should().Be(txData.SpansCount);
			transaction.SpanCount.Dropped.Should().Be(0);

			return transaction;
		}

		private void VerifySpan(SpanDto span, string url, int statusCode)
		{
			span.Context.Db.Should().BeNull();
			span.Context.Labels.Should().BeNull();

			span.Context.Http.Method.Should().Be("GET");
			span.Context.Http.StatusCode.Should().Be(statusCode);
			span.Context.Http.Url.Should().Be(url);

			span.Type.Should().Be(ApiConstants.TypeExternal);
			span.Subtype.Should().Be(ApiConstants.SubtypeHttp);
			span.Action.Should().BeNull();

			span.Name.Should().Be($"GET {new Uri(url).Host}");
		}
	}
}
