// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.HelpersTests;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	[Collection("ActivityIdFormat")]
	[CaptureRestoreActivityIdFormat]
	public class HttpDiagnosticListenerTests
	{
		private readonly ITestOutputHelper _output;

		public HttpDiagnosticListenerTests(ITestOutputHelper output) => _output = output;

		private static TResult DispatchToImpl<TResult>(
			IDiagnosticListener listener,
			Func<HttpDiagnosticListenerCoreImpl, TResult> coreImplFunc,
			Func<HttpDiagnosticListenerFullFrameworkImpl, TResult> fullFrameworkImplFunc
		)
		{
			switch (listener)
			{
				case HttpDiagnosticListenerCoreImpl impl:
					return coreImplFunc(impl);
				case HttpDiagnosticListenerFullFrameworkImpl impl:
					return fullFrameworkImplFunc(impl);
				default:
					throw new AssertionFailedException($"Unrecognized {nameof(HttpDiagnosticListener)} implementation - {listener.GetType()}");
			}
		}

		private static string StartEventKey(IDiagnosticListener listener) =>
			DispatchToImpl(listener, impl => impl.StartEventKey, impl => impl.StartEventKey);

		private static string StopEventKey(IDiagnosticListener listener) =>
			DispatchToImpl(listener, impl => impl.StopEventKey, impl => impl.StopEventKey);

		private static int ProcessingRequestsCount(IDiagnosticListener listener) =>
			DispatchToImpl(listener, impl => impl.ProcessingRequests.Count, impl => impl.ProcessingRequests.Count);

		private static ISpan GetSpanForRequest(IDiagnosticListener listener, object request) =>
			DispatchToImpl(
				listener,
				impl => impl.ProcessingRequests[(HttpRequestMessage)request],
				impl => impl.ProcessingRequests[(HttpWebRequest)request]
			);


		/// <summary>
		/// Calls the OnError method on the HttpDiagnosticListener and makes sure that the correct error message is logged.
		/// </summary>
		[Fact]
		public void OnErrorLog()
		{
			var logger = new TestLogger();
			using var agent = new ApmAgent(new TestAgentComponents(logger));
			var listener = HttpDiagnosticListener.New(agent);

			const string exceptionMessage = "Oops, this went wrong";
			var fakeException = new Exception(exceptionMessage);
			listener.OnError(fakeException);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					"HttpDiagnosticListener",
					"in OnError",
					".cs:",
					exceptionMessage
				);
		}

		/// <summary>
		/// Builds an HttpRequestMessage and calls HttpDiagnosticListener.OnNext directly with it.
		/// Makes sure that the processingRequests dictionary captures the ongoing transaction.
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void OnNextWithStart()
		{
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger));
			agent.HttpTraceConfiguration.CaptureSpan = true;
			StartTransaction(agent);
			var listener = HttpDiagnosticListener.New(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", new { Request = request }));
			ProcessingRequestsCount(listener).Should().Be(1);
			var span = GetSpanForRequest(listener, request);
			span.Context.Http.Url.Should().Be(request.RequestUri.ToString());
			span.Context.Http.Method.Should().Be(HttpMethod.Get.ToString());
			span.End();
			span.Context.Destination.Address.Should().Be(request.RequestUri.Host);
			span.Context.Destination.Port.Should().Be(UrlUtilsTests.DefaultHttpsPort);
		}

		/// <summary>
		/// Simulates the complete lifecycle of an HTTP request.
		/// It builds an HttpRequestMessage and an HttpResponseMessage
		/// and passes them to the OnNext method.
		/// Makes sure that a Span with an Http context is captured.
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void OnNextWithStartAndStop()
		{
			var logger = new TestLogger();
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(logger, payloadSender: payloadSender));
			agent.HttpTraceConfiguration.CaptureSpan = true;
			StartTransaction(agent);
			var listener = HttpDiagnosticListener.New(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");
			var response = new HttpResponseMessage(HttpStatusCode.OK);

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>(StartEventKey(listener), new { Request = request }));
			//Simulate Stop
			listener.OnNext(new KeyValuePair<string, object>(StopEventKey(listener), new { Request = request, Response = response }));
			ProcessingRequestsCount(listener).Should().Be(0);

			var firstSpan = payloadSender.FirstSpan;
			firstSpan.Should().NotBeNull();
			firstSpan.Context.Http.Url.Should().BeEquivalentTo(request.RequestUri.AbsoluteUri);
			firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
			firstSpan.Context.Destination.Address.Should().Be(request.RequestUri.Host);
			firstSpan.Context.Destination.Port.Should().Be(UrlUtilsTests.DefaultHttpsPort);
			firstSpan.Outcome.Should().Be(Outcome.Success);
		}

		/// <summary>
		/// Calls OnNext with System.Net.Http.HttpRequestOut.Stop twice.
		/// Makes sure that the transaction is only captured once and the span is also only captured once.
		/// Also make sure that there is an error log.
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void OnNextWithStartAndStopTwice()
		{
			var logger = new TestLogger(LogLevel.Debug);
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(logger, payloadSender: payloadSender));
			agent.HttpTraceConfiguration.CaptureSpan = true;
			StartTransaction(agent);
			var listener = HttpDiagnosticListener.New(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");
			var response = new HttpResponseMessage(HttpStatusCode.OK);

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>(StartEventKey(listener), new { Request = request }));
			//Simulate Stop
			listener.OnNext(new KeyValuePair<string, object>(StopEventKey(listener), new { Request = request, Response = response }));
			//Simulate Stop again. This should not happen, still we test for this.
			listener.OnNext(new KeyValuePair<string, object>(StopEventKey(listener), new { Request = request, Response = response }));

			logger.Lines.Should().NotBeEmpty();
			logger.Lines
				.Should()
				.Contain(
					line => line.Contains("HttpDiagnosticListener") && line.Contains("Could not remove request from processing requests.")
						&& line.Contains(HttpMethod.Get.Method)
						&& line.Contains(request.RequestUri.AbsoluteUri));

			payloadSender.WaitForTransactions(TimeSpan.FromSeconds(5));
			payloadSender.Transactions.Should().NotBeNull();
			payloadSender.WaitForSpans();
			payloadSender.Spans.Should().ContainSingle();
		}

		/// <summary>
		/// Calls HttpDiagnosticListener.OnNext with types that are unknown.
		/// The test makes sure that in this case still no exception is thrown from the OnNext method.
		/// </summary>
		[Fact]
		public void UnknownObjectToOnNext()
		{
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger));
			var listener = HttpDiagnosticListener.New(agent);
			var myFake = new StringBuilder(); //just a random type that is not planned to be passed into OnNext

			var exception =
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", myFake)); });

			exception.Should().BeNull();
		}

		/// <summary>
		/// Passes null instead of a valid HttpRequestMessage into HttpDiagnosticListener.OnNext
		/// and makes sure that still no exception is thrown.
		/// </summary>
		[Fact]
		public void NullValueToOnNext()
		{
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger));
			var listener = HttpDiagnosticListener.New(agent);

			var exception =
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", null)); });

			exception.Should().BeNull();
		}

		/// <summary>
		/// Passes a null key with null value instead of a valid HttpRequestMessage into HttpDiagnosticListener.OnNext
		/// and makes sure that still no exception is thrown.
		/// </summary>
		[Fact]
		public void NullKeyValueToOnNext()
		{
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger));
			var listener = HttpDiagnosticListener.New(agent);

			var exception =
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>(null, null)); });

			exception.Should().BeNull();
		}

		/// <summary>
		/// Sends a simple real HTTP GET message and makes sure that
		/// HttpDiagnosticListener captures it.
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task TestSimpleOutgoingHttpRequest()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();
			using (agent)
			{
				using (subscriber)
				using (var localServer = LocalServer.Create())
				{
					using var httpClient = new HttpClient();
					var res = await httpClient.GetAsync(localServer.Uri);

					res.IsSuccessStatusCode.Should().BeTrue();
					payloadSender.WaitForSpans();
					var firstSpan = payloadSender.FirstSpan;
					firstSpan.Should().NotBeNull();
					firstSpan.Context.Http.Url.Should().Be(localServer.Uri);
					firstSpan.Context.Http.StatusCode.Should().Be(200);
					firstSpan.Outcome.Should().Be(Outcome.Success);
					firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
					firstSpan.Context.Destination.Address.Should().Be(new Uri(localServer.Uri).Host);
					firstSpan.Context.Destination.Port.Should().Be(new Uri(localServer.Uri).Port);
				}
			}
		}

		/// <summary>
		/// Makes sure the outgoing request with URL that contains username and password is captured, but the
		/// username and the password are redacted.
		/// </summary>
		[NetCoreAndNetFact]
		public async Task TestUrlSanitization()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			using (subscriber)
			using (var localServer = LocalServer.Create())
			{
				var uri = new Uri(localServer.Uri);

				var uriBuilder = new UriBuilder(uri) { UserName = "TestUser", Password = "TestPassword" };

				using var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(uriBuilder.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
				payloadSender.WaitForSpans();
				var firstSpan = payloadSender.FirstSpan;
				firstSpan.Should().NotBeNull();
				firstSpan.Context.Http.Url.Should()
					.Be(uriBuilder.Uri.ToString()
						.Replace("TestUser", Consts.Redacted)
						.Replace("TestPassword", Consts.Redacted));
				firstSpan.Context.Http.StatusCode.Should().Be(200);
				firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
				firstSpan.Context.Destination.Address.Should().Be(new Uri(localServer.Uri).Host);
				firstSpan.Context.Destination.Port.Should().Be(new Uri(localServer.Uri).Port);
			}
		}

		/// <summary>
		/// Sends a simple real HTTP POST message and the server responds with 500
		/// The test makes sure HttpDiagnosticListener captures the POST method and
		/// the response code correctly
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task TestNotSuccessfulOutgoingHttpPostRequest()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			using (subscriber)
			using (var localServer = LocalServer.Create(ctx => { ctx.Response.StatusCode = 500; }))
			{
				using var httpClient = new HttpClient();
				var res = await httpClient.PostAsync(localServer.Uri, new StringContent("foo"));

				res.IsSuccessStatusCode.Should().BeFalse();
				payloadSender.WaitForSpans();
				var firstSpan = payloadSender.FirstSpan;
				firstSpan.Should().NotBeNull();
				firstSpan.Context.Http.Url.Should().Be(localServer.Uri);
				firstSpan.Context.Http.StatusCode.Should().Be(500);
				firstSpan.Outcome.Should().Be(Outcome.Failure);
				firstSpan.Context.Http.Method.Should().Be(HttpMethod.Post.Method);
				firstSpan.Context.Destination.Address.Should().Be(new Uri(localServer.Uri).Host);
				firstSpan.Context.Destination.Port.Should().Be(new Uri(localServer.Uri).Port);
			}
		}

		/// <summary>
		/// Starts an HTTP call to a non existing URL and makes sure that an error is captured.
		/// This uses an HttpClient instance directly
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task CaptureErrorOnFailingHttpCall_HttpClient()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			using (subscriber)
			using (var httpClient = new HttpClient())
			{
				Func<Task> act = async () => await httpClient.GetAsync("http://nonexistenturl_dsfdsf.ghkdehfn");
				await act.Should().ThrowAsync<Exception>();

				payloadSender.WaitForErrors();
				payloadSender.Errors.Should().NotBeEmpty();
			}
		}

		/// <summary>
		/// Passes an exception to <see cref="HttpDiagnosticListener" /> and makes sure that the exception is captured
		/// Unlike the <see cref="CaptureErrorOnFailingHttpCall_HttpClient" /> method this does not use HttpClient, instead here we
		/// call the OnNext method directly.
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void CaptureErrorOnFailingHttpCall_DirectCall()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();
			agent.HttpTraceConfiguration.Subscribed = true;

			using (agent)
			using (subscriber)
			{
				var listener = HttpDiagnosticListener.New(agent);

				var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");

				//Simulate Start
				listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", new { Request = request }));

				const string exceptionMsg = "Sample exception msg";
				var exception = new Exception(exceptionMsg);
				//Simulate Exception
				listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.Exception", new { Request = request, Exception = exception }));

				payloadSender.WaitForErrors();
				payloadSender.Errors.Should().NotBeEmpty();
				payloadSender.FirstError.Exception.Message.Should().Be(exceptionMsg);
				payloadSender.FirstError.Exception.Type.Should().Be(typeof(Exception).FullName);
			}
		}

		// -----------------------------------------------------------------------
		// Issue #2466 — NTLM/Negotiate authentication
		//
		// Root cause: HttpHandlerDiagnosticListener.IsLastResponse returns true for
		// any non-redirect status code, so it fires Ex.Stop for every intermediate
		// 401 NTLM challenge *before* the final HttpWebResponse is constructed.
		// The agent was treating that Ex.Stop as the terminal event, ending the span
		// with status 401 and capturing an error.  The subsequent Stop event (fired
		// when the real 200 arrives) then found nothing in ProcessingRequests and
		// was silently dropped.
		// -----------------------------------------------------------------------

#pragma warning disable SYSLIB0014 // WebRequest.Create is the only way to get an HttpWebRequest in tests
		private static (HttpDiagnosticListenerFullFrameworkImpl Listener, HttpWebRequest Request)
			CreateFullFrameworkListenerAndRequest(ApmAgent agent)
		{
			agent.HttpTraceConfiguration.CaptureSpan = true;
			var listener = new HttpDiagnosticListenerFullFrameworkImpl(agent);
			var request = (HttpWebRequest)WebRequest.Create("http://elastic.co");
			return (listener, request);
		}
#pragma warning restore SYSLIB0014

		private static void FireExStop(HttpDiagnosticListenerFullFrameworkImpl listener, HttpWebRequest request,
			HttpStatusCode statusCode, WebHeaderCollection headers) =>
			listener.OnNext(new KeyValuePair<string, object>(
				"System.Net.Http.Desktop.HttpRequestOut.Ex.Stop",
				new { Request = request, StatusCode = statusCode, Headers = headers }));

		private static void FireStart(HttpDiagnosticListenerFullFrameworkImpl listener, HttpWebRequest request) =>
			listener.OnNext(new KeyValuePair<string, object>(
				"System.Net.Http.Desktop.HttpRequestOut.Start",
				new { Request = request }));

		private static void FireStop(HttpDiagnosticListenerFullFrameworkImpl listener, HttpWebRequest request) =>
			listener.OnNext(new KeyValuePair<string, object>(
				"System.Net.Http.Desktop.HttpRequestOut.Stop",
				new { Request = request, Response = (HttpWebResponse)null }));

		/// <summary>
		/// Reproduces https://github.com/elastic/apm-agent-dotnet/issues/2466 — single NTLM round.
		/// Proves the failure: without the fix, ProcessExceptionEvent calls CaptureError + span.End(),
		/// so payloadSender.Errors would be non-empty and the span would carry StatusCode 401.
		/// Proves the fix: the intermediate Ex.Stop is ignored; the span is only ended by the final
		/// Stop event, leaving Errors empty.
		/// </summary>
		[Fact]
		public void NtlmChallenge_SingleRound_SpanCapturedWithoutError()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			// 1. Request starts
			FireStart(listener, request);
			listener.ProcessingRequests.Should().ContainKey(request);

			// 2. Server returns 401 + NTLM challenge (intermediate handshake step).
			//    HttpHandlerDiagnosticListener fires Ex.Stop because IsLastResponse(401) == true.
			//    Before the fix: CaptureError + span.End() would be called here → span status 401.
			var ntlmHeaders = new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "NTLM" };
			FireExStop(listener, request, HttpStatusCode.Unauthorized, ntlmHeaders);

			// Span must survive the challenge — still tracked, not ended with 401
			listener.ProcessingRequests.Should().ContainKey(request,
				"span must not be prematurely ended on the intermediate NTLM 401 challenge");

			// 3. NTLM handshake succeeds; final Stop fires.
			//    (HttpWebResponse has no public ctor so Response is null — the span still ends cleanly.)
			FireStop(listener, request);
			listener.ProcessingRequests.Should().NotContainKey(request, "span must be ended by the final Stop");

			// Core proof of the fix: without it, CaptureError would have been called for the 401 challenge
			payloadSender.WaitForSpans(count: 1);
			using (new AssertionScope())
			{
				payloadSender.Errors.Should().BeEmpty("the NTLM challenge must not be recorded as an APM error");
				payloadSender.Spans.Should().HaveCount(1, "exactly one span should be sent for the whole request");
				payloadSender.FirstSpan.Context.Http.StatusCode.Should().NotBe(401,
					"the span must not carry the intermediate challenge status code");
			}
		}

		/// <summary>
		/// Full two-round NTLM handshake: the runtime fires Ex.Stop twice (type-1 and type-2 challenges)
		/// before the final Stop.  Both intermediate events must be ignored.
		/// </summary>
		[Fact]
		public void NtlmChallenge_TwoRounds_BothChallengesIgnoredSpanCapturedOnce()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			FireStart(listener, request);

			// Round 1: bare NTLM challenge
			FireExStop(listener, request, HttpStatusCode.Unauthorized,
				new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "NTLM" });
			listener.ProcessingRequests.Should().ContainKey(request, "span must survive round-1 NTLM challenge");

			// Round 2: NTLM type-2 message with server's challenge token
			FireExStop(listener, request, HttpStatusCode.Unauthorized,
				new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "NTLM dummybase64token==" });
			listener.ProcessingRequests.Should().ContainKey(request, "span must survive round-2 NTLM challenge");

			FireStop(listener, request);

			payloadSender.WaitForSpans(count: 1);
			using (new AssertionScope())
			{
				payloadSender.Errors.Should().BeEmpty();
				payloadSender.Spans.Should().HaveCount(1);
			}
		}

		/// <summary>
		/// Negotiate (SPNEGO/Kerberos) challenges use the same code path as NTLM.
		/// </summary>
		[Fact]
		public void NegotiateChallenge_SingleRound_SpanCapturedWithoutError()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			FireStart(listener, request);

			FireExStop(listener, request, HttpStatusCode.Unauthorized,
				new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "Negotiate" });
			listener.ProcessingRequests.Should().ContainKey(request, "span must survive a Negotiate challenge");

			FireStop(listener, request);
			listener.ProcessingRequests.Should().NotContainKey(request);

			payloadSender.WaitForSpans(count: 1);
			using (new AssertionScope())
			{
				payloadSender.Errors.Should().BeEmpty("Negotiate challenge must not be recorded as an APM error");
				payloadSender.Spans.Should().HaveCount(1);
				payloadSender.FirstSpan.Context.Http.StatusCode.Should().NotBe(401);
			}
		}

		/// <summary>
		/// A genuine 401 (Basic auth, no NTLM/Negotiate header) must still be recorded as a failure.
		/// Regression guard: the fix must not suppress real auth failures.
		/// </summary>
		[Fact]
		public void Genuine401_ExStopWithBasicAuthHeader_SpanEndedWithFailure()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			FireStart(listener, request);
			FireExStop(listener, request, HttpStatusCode.Unauthorized,
				new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "Basic realm=\"example\"" });

			listener.ProcessingRequests.Should().NotContainKey(request, "genuine 401 must end the span immediately");
			payloadSender.WaitForErrors();
			using (new AssertionScope())
			{
				payloadSender.Errors.Should().NotBeEmpty("genuine 401 must be reported as a failure");
				payloadSender.WaitForSpans(count: 1);
				payloadSender.FirstSpan.Context.Http.StatusCode.Should().Be(401);
			}
		}

		/// <summary>
		/// Regression guard: Ex.Stop with a null Headers payload (edge case) must still end the span as a failure.
		/// </summary>
		[Fact]
		public void ExStop_With401AndNullHeaders_SpanEndedWithFailure()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			FireStart(listener, request);
			// Headers property present but null — must not crash, must still record the 401
			FireExStop(listener, request, HttpStatusCode.Unauthorized, headers: null);

			listener.ProcessingRequests.Should().NotContainKey(request);
			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty("a 401 with null headers is still a failure");
		}

		/// <summary>
		/// Regression guard: Ex.Stop with a non-401 status code (500) must still end the span as a failure.
		/// The NTLM check is gated on 401 Unauthorized; all other codes go through the original path.
		/// </summary>
		[Fact]
		public void ExStop_WithNon401StatusCode_SpanEndedWithFailure()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			FireStart(listener, request);
			FireExStop(listener, request, HttpStatusCode.InternalServerError, new WebHeaderCollection());

			listener.ProcessingRequests.Should().NotContainKey(request, "500 must end the span immediately");
			payloadSender.WaitForErrors();
			using (new AssertionScope())
			{
				payloadSender.Errors.Should().NotBeEmpty("500 must be reported as a failure");
				payloadSender.WaitForSpans(count: 1);
				payloadSender.FirstSpan.Context.Http.StatusCode.Should().Be(500);
			}
		}

		/// <summary>
		/// Regression guard: WWW-Authenticate: Basic realm="Negotiate" must NOT be treated as NTLM/Negotiate.
		/// "Negotiate" appears in the realm value, not as the authentication scheme token.
		/// </summary>
		[Fact]
		public void Genuine401_BasicRealmContainingNegotiate_SpanEndedWithFailure()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			FireStart(listener, request);
			FireExStop(listener, request, HttpStatusCode.Unauthorized,
				new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "Basic realm=\"Negotiate\"" });

			listener.ProcessingRequests.Should().NotContainKey(request,
				"Basic realm=\"Negotiate\" is not an NTLM/Negotiate challenge and must end the span immediately");
			payloadSender.WaitForErrors();
			using (new AssertionScope())
			{
				payloadSender.Errors.Should().NotBeEmpty("a Basic 401 with Negotiate in the realm must be recorded as a failure");
				payloadSender.WaitForSpans(count: 1);
				payloadSender.FirstSpan.Context.Http.StatusCode.Should().Be(401);
			}
		}

		/// <summary>
		/// When NTLM authentication ultimately fails (wrong credentials), the server returns a terminal 401
		/// with WWW-Authenticate: NTLM.  The .NET Framework runtime fires Ex.Stop for the terminal challenge
		/// and then Stop when the request completes; the span must survive the second Ex.Stop and be ended by Stop.
		/// </summary>
		[Fact]
		public void NtlmChallenge_TerminalAuthFailure_SpanEndedByFinalStop()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);
			var (listener, request) = CreateFullFrameworkListenerAndRequest(agent);

			FireStart(listener, request);

			// Intermediate NTLM type-1 challenge
			FireExStop(listener, request, HttpStatusCode.Unauthorized,
				new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "NTLM" });
			listener.ProcessingRequests.Should().ContainKey(request, "span must survive intermediate NTLM challenge");

			// Terminal 401: credentials rejected; server re-issues NTLM challenge — Ex.Stop fires again
			FireExStop(listener, request, HttpStatusCode.Unauthorized,
				new WebHeaderCollection { [HttpResponseHeader.WwwAuthenticate] = "NTLM" });
			listener.ProcessingRequests.Should().ContainKey(request,
				"span must survive terminal NTLM 401 until the final Stop arrives");

			// Stop fires when the request completes — this must end the span
			FireStop(listener, request);
			listener.ProcessingRequests.Should().NotContainKey(request, "span must be ended by the final Stop");

			payloadSender.WaitForSpans(count: 1);
			payloadSender.Spans.Should().HaveCount(1, "exactly one span must be sent for the whole request");
		}

		/// <summary>
		/// Makes sure we set the correct type and subtype for external, http spans
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task SpanTypeAndSubtype()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			using (subscriber)
			using (var localServer = LocalServer.Create())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
				payloadSender.WaitForSpans();
				payloadSender.FirstSpan.Type.Should().Be(ApiConstants.TypeExternal);
				payloadSender.FirstSpan.Subtype.Should().Be(ApiConstants.SubtypeHttp);
				payloadSender.FirstSpan.Action.Should().BeNull(); //we don't set Action for HTTP calls
			}
		}

		/// <summary>
		/// Makes sure we generate the correct span name
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task SpanName()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			{
				using (subscriber)
				using (var localServer = LocalServer.Create())
				{
					using var httpClient = new HttpClient();
					var res = await httpClient.GetAsync(localServer.Uri);
					res.IsSuccessStatusCode.Should().BeTrue();
				}

				payloadSender.WaitForSpans();
				payloadSender.FirstSpan.Name.Should().Be("GET localhost");
			}
		}

		/// <summary>
		/// Makes sure that the duration of an HTTP Request is captured by the agent
		/// </summary>
		/// <returns>The request duration.</returns>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task HttpRequestDuration()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			{
				using (subscriber)
				using (var localServer = LocalServer.Create(ctx =>
				{
					ctx.Response.StatusCode = 200;
					Thread.Sleep(5); //Make sure duration is really > 0
				}))
				{
					using var httpClient = new HttpClient();
					var res = await httpClient.GetAsync(localServer.Uri);

					res.IsSuccessStatusCode.Should().BeTrue();

					payloadSender.WaitForSpans();
					var firstSpan = payloadSender.FirstSpan;
					firstSpan.Should().NotBeNull();
					firstSpan.Context.Http.Url.Should().Be(localServer.Uri);
					firstSpan.Context.Http.StatusCode.Should().Be(200);
					firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
					firstSpan.Duration.Should().BeGreaterThan(0);
				}
			}
		}

		/// <summary>
		/// Makes sure spans have an Id
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task HttpRequestSpanGuid()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			using (subscriber)
			using (var localServer = LocalServer.Create())
			{
				using var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();

				payloadSender.WaitForSpans();
				var firstSpan = payloadSender.FirstSpan;
				firstSpan.Should().NotBeNull();
				firstSpan.Context.Http.Url.Should().Be(localServer.Uri);
				firstSpan.Context.Http.StatusCode.Should().Be(200);
				firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
				firstSpan.Duration.Should().BeGreaterThan(0);
			}
		}

		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		[SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
		public async Task HttpCallAsNestedSpan()
		{
			const string topSpanName = "test_top_span";
			const string topSpanType = "test_top_span_type";
			const int numberOfHttpCalls = 3;
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			{
				using (subscriber)
				using (var localServer = LocalServer.Create())
				{

					using var httpClient = new HttpClient();
					var topSpan = agent.Tracer.CurrentTransaction.StartSpan(topSpanName, topSpanType);

					await numberOfHttpCalls.Repeat(async i =>
					{
						var res = await httpClient.GetAsync($"{localServer.Uri}?i={i}");
						res.IsSuccessStatusCode.Should().BeTrue();
					});
					topSpan.End();

					payloadSender.WaitForSpans();
					payloadSender.Spans.Should().HaveCount(numberOfHttpCalls + 1);
					var topSpanSent = payloadSender.Spans.Last();
					topSpanSent.Name.Should().Be(topSpanName);
					topSpanSent.Type.Should().Be(topSpanType);
					// ReSharper disable AccessToDisposedClosure
					numberOfHttpCalls.Repeat(i =>
					{
						var httpCallSpan = payloadSender.Spans[i];
						httpCallSpan.Should().NotBeNull();
						httpCallSpan.Context.Http.Url.Should().Be($"{localServer.Uri}?i={i}");
						httpCallSpan.Context.Http.StatusCode.Should().Be(200);
						httpCallSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
						httpCallSpan.Context.Destination.Address.Should().Be(new Uri(localServer.Uri).Host);
						httpCallSpan.Context.Destination.Port.Should().Be(new Uri(localServer.Uri).Port);
						httpCallSpan.Duration.Should().BeGreaterThan(0);
						topSpanSent.Duration.Value.Should().BeGreaterOrEqualTo(httpCallSpan.Duration.Value);
						httpCallSpan.ParentId.Should().Be(topSpanSent.Id);
					});
					// ReSharper restore AccessToDisposedClosure
				}
			}
		}

		/// <summary>
		/// Creates an HTTP call without registering the <see cref="HttpDiagnosticsSubscriber" />.
		/// This is something like having a console application and just referencing the agent library.
		/// By default the agent does not subscribe in that scenario to any diagnostic source.
		/// Makes sure that no HTTP call is captured.
		/// </summary>
		[Fact]
		public async Task HttpCallWithoutRegisteredListener()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			using var localServer = LocalServer.Create();

			await agent.Tracer.CaptureTransaction("TestTransaction", "TestType", async t =>
			{
				Thread.Sleep(5);

				var httpClient = new HttpClient();
				try
				{
					var response = await httpClient.GetAsync(localServer.Uri);
					response.IsSuccessStatusCode.Should().BeTrue();
				}
				catch (Exception e)
				{
					t.CaptureException(e);
				}
			});

			mockPayloadSender.WaitForTransactions();
			mockPayloadSender.Transactions.Should().NotBeEmpty();
			mockPayloadSender.SignalEndSpans();
			mockPayloadSender.WaitForSpans();
			mockPayloadSender.SpansOnFirstTransaction.Should().BeEmpty();
		}

		/// <summary>
		/// Makes sure that in case of outgoing HTTP requests with a URL containing username and password those
		/// are not showing up in the agent logs.
		/// </summary>
		[Fact]
		public async Task NoUserNameAndPasswordInLogsForHttp()
		{
			var payloadSender = new NoopPayloadSender();
			var logger = new TestLogger(LogLevel.Trace);

			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, logger: logger));
			using (agent.Subscribe(new HttpDiagnosticsSubscriber()))
			{
				StartTransaction(agent);

				using var localServer = LocalServer.Create();
				using var httpClient = new HttpClient();
				var uriBuilder = new UriBuilder(localServer.Uri) { UserName = "TestUser289421", Password = "Password973243" };

				var res = await httpClient.GetAsync(uriBuilder.Uri);
				res.IsSuccessStatusCode.Should().BeTrue();
				logger.Lines.Should().NotBeEmpty();

				logger.Lines.Should().NotContain(n => n.Contains("TestUser289421"));
				logger.Lines.Should().NotContain(n => n.Contains("Password973243"));

				// looking for lines with "localhost:8082" and asserting that those contain [REDACTED].
				foreach (var lineWithHttpLog in logger.Lines.Where(n => n.Contains($"{uriBuilder.Host}:{uriBuilder.Port}")))
					lineWithHttpLog.Should().Contain(Consts.Redacted);
			}
		}

		/// <summary>
		/// Make sure HttpDiagnosticSubscriber does not report spans after its disposed
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task SubscriptionOnlyRegistersSpansDuringItsLifeTime()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);

			var spans = payloadSender.Spans;

			using (var localServer = LocalServer.Create())
			using (var httpClient = new HttpClient())
			{
				spans.Should().BeEmpty();
				using (agent.Subscribe(new HttpDiagnosticsSubscriber()))
				{
					var res = await httpClient.GetAsync(localServer.Uri);
					res.IsSuccessStatusCode.Should().BeTrue();
					res = await httpClient.GetAsync(localServer.Uri);
					res.IsSuccessStatusCode.Should().BeTrue();
				}

				payloadSender.WaitForSpans();
				spans = payloadSender.Spans;
				spans.Should().NotBeEmpty().And.HaveCount(2);
				foreach (var _ in Enumerable.Range(0, 10))
					await httpClient.GetAsync(localServer.Uri);

				localServer.SeenRequests.Should()
					.BeGreaterOrEqualTo(10,
						"Make sure we actually performed more than 1 request to our local server");
			}
			spans.Should().HaveCount(2);
		}

		/// <summary>
		/// Same as <see cref="HttpCallWithoutRegisteredListener" /> but this one registers
		/// <see cref="HttpDiagnosticsSubscriber" />.
		/// Makes sure that the outgoing web request is captured.
		/// </summary>
		[NetCoreAndNetFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task HttpCallWithRegisteredListener()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			var subscriber = new HttpDiagnosticsSubscriber();

			using var localServer = LocalServer.Create();
			using (agent.Subscribe(subscriber))
			{
				var url = localServer.Uri;
				await agent.Tracer.CaptureTransaction("TestTransaction", "TestType", async t =>
				{
					Thread.Sleep(5);

					var httpClient = new HttpClient();
					try
					{
						var response = await httpClient.GetAsync(url);
						response.IsSuccessStatusCode.Should().BeTrue();
					}
					catch (Exception e)
					{
						t.CaptureException(e);
					}
				});

				mockPayloadSender.WaitForTransactions();
				mockPayloadSender.Transactions.Should().NotBeEmpty();
				mockPayloadSender.WaitForSpans();
				mockPayloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

				mockPayloadSender.SpansOnFirstTransaction[0].Context.Http.Should().NotBeNull();
				mockPayloadSender.SpansOnFirstTransaction[0].Context.Http.Url.Should().Be(url);
			}
		}

		/// <summary>
		/// Subscribes to diagnostic events then unsubscribes.
		/// Makes sure unsubscribing worked.
		/// </summary>
		[Fact]
		public async Task SubscribeUnsubscribe()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			var subscriber = new HttpDiagnosticsSubscriber();
			using var httpClient = new HttpClient();

			using var localServer = LocalServer.Create();
			var url = localServer.Uri;
			using (agent.Subscribe(subscriber)) //subscribe
			{
				await agent.Tracer.CaptureTransaction("TestTransaction", "TestType", async t =>
				{
					Thread.Sleep(5);
					try
					{
						var response = await httpClient.GetAsync(url);
						response.IsSuccessStatusCode.Should().BeTrue();
					}
					catch (Exception e)
					{
						t.CaptureException(e);
					}
				});
			} //and then unsubscribe

			mockPayloadSender.WaitForAny();
			mockPayloadSender.Clear();

			await agent.Tracer.CaptureTransaction("TestTransaction", "TestType", async t =>
			{
				Thread.Sleep(5);
				try
				{
					var response = await httpClient.GetAsync(url);
					response.IsSuccessStatusCode.Should().BeTrue();
				}
				catch (Exception e)
				{
					t.CaptureException(e);
				}
			});

			mockPayloadSender.WaitForTransactions();
			mockPayloadSender.FirstTransaction.Should().NotBeNull();
			mockPayloadSender.SignalEndSpans();
			mockPayloadSender.WaitForSpans();
			mockPayloadSender.SpansOnFirstTransaction.Should().BeEmpty();
		}

		/// <summary>
		/// Makes sure that in case of an HTTP request without an active transaction the agent does not print warnings.
		/// Covers https://github.com/elastic/apm-agent-dotnet/issues/734
		/// </summary>
		[Fact]
		public async Task NoWarningWithNoTransaction()
		{
			var logger = new TestLogger(LogLevel.Trace);

			// ServiceVersion is set, otherwise in the xUnit context it'd log a warning, since it can be auto discovered
			using var agent = new ApmAgent(new TestAgentComponents(logger, new MockConfiguration(serviceVersion: "1.0")))
				.Subscribe(new HttpDiagnosticsSubscriber());

			// No active transaction, just an HTTP request with an active agent
			try
			{
				using var httpClient = new HttpClient();
				var response = await httpClient.GetAsync("https://elastic.co");
				(await response.Content.ReadAsStringAsync()).Should().NotBeNullOrEmpty();
			}
			catch
			{
				/*ignore - result of the request does not matter */
			}

			logger.Lines.Should().NotContain(line => line.ToLower().Contains("warn:"));
		}

		/// <summary>
		/// Makes sure that in case of an async outgoing http call the caller method shows up in the captured callstack
		/// </summary>
		[NetCoreAndNetFact]
		public async Task CallStackContainsCallerMethod()
		{
			var (subscriber, payloadSender, agent) = RegisterSubscriberAndStartTransaction();

			using (agent)
			using (subscriber)
			{
				try
				{
					using var localServer = LocalServer.Create();
					using var httpClient = new HttpClient();
					var response = await httpClient.GetAsync(localServer.Uri);
					response.IsSuccessStatusCode.Should().BeTrue();
				}
				catch (Exception)
				{
					// ignore - only thing we care about in this stack is the stack trace.
				}

				payloadSender.WaitForSpans();
				payloadSender.FirstSpan.StackTrace.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().Contain(n => n.Function.Contains(nameof(CallStackContainsCallerMethod)));
			}
		}

		// Don't run this test on NET462 as it can intermittently fail-
		// NET Framework's instrumentation to capture HTTP client calls in HttpHandlerDiagnosticListener is achieved through reflection to replace
		// the ServicePointManager.s_ServicePointTable static non-public field, in order to provide own implementations of the ServicePointHashtable,
		// ConnectionGroupHashtable, ConnectionArrayList and HttpWebRequestArrayList, such that diagnostic source events can be raised when new
		// HttpWebRequest are added and removed. See https://github.com/dotnet/runtime/blob/7565d60891e43415f5e81b59e50c52dba46ee0d7/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/HttpHandlerDiagnosticListener.cs#L64-L87
		//
		// A problem with this approach is that operating with ServicePointManager.s_ServicePointTable using
		// reflection bypasses locking on ServicePointManager.s_ServicePointTable field performed when ServicePointManager.FindServicePoint(...)
		// is called: https://referencesource.microsoft.com/#system/net/system/Net/ServicePointManager.cs,766
		//
		// which happens when configuring Central Config and PayloadSender, and also when an endpoint is used for the first time. Since
		// ServicePointManager.s_ServicePointTable is a static non-public field, and tests run concurrently, a race condition can occur whereby
		// the setup of HttpHandlerDiagnosticListener to track HttpWebRequest clashes with ServicePointManager.FindServicePoint(...), resulting
		// in HttpHandlerDiagnosticListener's instrumentation not wrapping the original Hashtable to which the ServicePoint for localServer is added,
		// and not raising diganostic events and not capturing a span for it.
		//
		// This problem is unlikely to occur in production usage of HttpHandlerDiagnosticListener.
		[NetCoreAndNetFact]
		public async Task HttpCallWithW3CActivityFormat()
		{
			Activity.DefaultIdFormat = ActivityIdFormat.W3C;

			var mockPayloadSender = new MockPayloadSender();
			var logger = new XUnitLogger(LogLevel.Trace, _output, nameof(HttpCallWithW3CActivityFormat));
			using var agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: mockPayloadSender));

			using var localServer = LocalServer.Create();
			using var subscriber = agent.Subscribe(new HttpDiagnosticsSubscriber());

			await agent.Tracer.CaptureTransaction("Test", "Test", async () =>
			{
				using var httpClient = new HttpClient();
				try
				{
					var response = await httpClient.GetAsync(localServer.Uri);
					response.IsSuccessStatusCode.Should().BeTrue();
				}
				catch (Exception e)
				{
					_output.WriteLine($"Unsuccessful request: {e}");
				}
			});

			localServer.SeenRequests.Should().Be(1);
			mockPayloadSender.WaitForSpans();
			mockPayloadSender.Spans.Should().HaveCount(1);
		}

		internal static (IDisposable, MockPayloadSender, ApmAgent) RegisterSubscriberAndStartTransaction()
		{
			var payloadSender = new MockPayloadSender();
			var agentComponents = new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(logLevel: "Debug", stackTraceLimit: "-1",
					spanFramesMinDurationInMilliseconds: "-1ms"));

			var agent = new ApmAgent(agentComponents);
			var subscriber = agent.Subscribe(new HttpDiagnosticsSubscriber());
			StartTransaction(agent);

			return (subscriber, payloadSender, agent);
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private static void StartTransaction(ApmAgent agent)
			//	=> agent.TransactionContainer.Transactions.Value =
			//		new Transaction(agent, $"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);
			=> agent.Tracer.StartTransaction($"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);
	}
}
