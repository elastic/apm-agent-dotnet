using System;
using System.Collections.Generic;
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
	public class DistributedTracingTests : TestsBase
	{
		public DistributedTracingTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task ContactPageCallsAboutPageAndExternalUrl()
		{
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(SampleAppUrlPaths.ContactPage.RelativeUrlPath,
				SampleAppUrlPaths.ContactPage.Status);

			VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(SampleAppUrlPaths.ContactPage, receivedData);

				SampleAppUrlPaths.ContactPage.TransactionsCount.Should().Be(2);
				SampleAppUrlPaths.ContactPage.SpansCount.Should().Be(2);

				var rootTxUrlPath = Consts.SampleApp.RootUrlPath + "/" + HomeController.ContactPageRelativePath;
				var rootTx = receivedData.Transactions.Single(tx => tx.Name == $"GET {rootTxUrlPath}");
				VerifyTransaction(rootTx, rootTxUrlPath, 2);

				var childTxUrlPath = Consts.SampleApp.RootUrlPath + "/" + HomeController.AboutPageRelativePath;
				var childTx = receivedData.Transactions.Single(tx => tx.Name == $"GET {childTxUrlPath}");
				VerifyTransaction(childTx, childTxUrlPath, 0);

				var spanCallToChildTx = receivedData.Spans.Single(sp => sp.Context.Http.Url == childTx.Context.Request.Url.Full);
				VerifySpan(spanCallToChildTx, childTx.Context.Request.Url.Full);
				var spanExternalCall =
					receivedData.Spans.Single(sp => sp.Context.Http.Url == HomeController.ChildHttpCallToExternalServiceUrl.ToString());
				VerifySpan(spanExternalCall, HomeController.ChildHttpCallToExternalServiceUrl.ToString());

				childTx.TraceId.Should().Be(rootTx.TraceId);
				spanCallToChildTx.TraceId.Should().Be(rootTx.TraceId);
				spanExternalCall.TraceId.Should().Be(rootTx.TraceId);

				spanCallToChildTx.ParentId.Should().Be(rootTx.Id);
				childTx.ParentId.Should().Be(spanCallToChildTx.Id);

				spanExternalCall.ParentId.Should().Be(rootTx.Id);

				spanCallToChildTx.TransactionId.Should().Be(rootTx.Id);
				spanExternalCall.TransactionId.Should().Be(rootTx.Id);

				spanCallToChildTx.ShouldOccurBetween(rootTx);
				childTx.ShouldOccurBetween(spanCallToChildTx);
				spanExternalCall.ShouldOccurBetween(rootTx);
			});

			void VerifyTransaction(TransactionDto transaction, string urlPath, int spanCount)
			{
				transaction.Context.Request.Method.ToUpperInvariant().Should().Be("GET");
				transaction.Context.Request.Url.Full.Should().Be("http://" + Consts.SampleApp.Host + urlPath);
				transaction.Context.Request.Url.PathName.Should().Be(urlPath);
				transaction.Context.Request.Url.Search.Should().BeNull();

				transaction.Context.Response.Finished.Should().BeTrue();
				var caseInsensitiveResponseHeaders =
					new Dictionary<string, string>(transaction.Context.Response.Headers, StringComparer.OrdinalIgnoreCase);
				caseInsensitiveResponseHeaders["Content-Type"].Should().Be("text/html; charset=utf-8");
				transaction.Context.Response.StatusCode.Should().Be(200);

				transaction.Context.Tags.Should().BeNull();
				transaction.Context.User.Should().BeNull();

				transaction.IsSampled.Should().BeTrue();
				transaction.Name.Should().Be($"GET {urlPath}");
				transaction.Result.Should().Be($"HTTP 2xx");
				transaction.SpanCount.Started.Should().Be(spanCount);
				transaction.SpanCount.Dropped.Should().Be(0);
			}

			void VerifySpan(SpanDto span, string url)
			{
				span.Context.Db.Should().BeNull();
				span.Context.Tags.Should().BeNull();

				span.Context.Http.Method.Should().Be("GET");
				span.Context.Http.StatusCode.Should().Be(200);
				span.Context.Http.Url.Should().Be(url);

				span.Type.Should().Be(ApiConstants.TypeExternal);
				span.Subtype.Should().Be(ApiConstants.SubtypeHttp);
				span.Action.Should().BeNull();

				span.Name.Should().Be($"GET {new Uri(url).Host}");
			}
		}
	}
}
