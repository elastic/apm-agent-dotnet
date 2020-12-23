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
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class HttpDiagnosticListenerTests
	{
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
			var agent = new ApmAgent(new TestAgentComponents(logger));
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
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void OnNextWithStart()
		{
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger));
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
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void OnNextWithStartAndStop()
		{
			var logger = new TestLogger();
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(logger, payloadSender: payloadSender));
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
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void OnNextWithStartAndStopTwice()
		{
			var logger = new TestLogger(LogLevel.Warning);
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(logger, payloadSender: payloadSender));
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
					line => line.Contains("HttpDiagnosticListener") && line.Contains("Failed capturing request")
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
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task TestSimpleOutgoingHttpRequest()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create())
			{
				var httpClient = new HttpClient();
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

		/// <summary>
		/// Makes sure the outgoing request with URL that contains username and password is captured, but the
		/// username and the password are redacted.
		/// </summary>
		[NetCoreFact]
		public async Task TestUrlSanitization()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create())
			{
				var uri = new Uri(localServer.Uri);

				var uriBuilder = new UriBuilder(uri) { UserName = "TestUser", Password = "TestPassword" };

				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(uriBuilder.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
				payloadSender.WaitForSpans();
				var firstSpan = payloadSender.FirstSpan;
				firstSpan.Should().NotBeNull();
				firstSpan.Context.Http.Url.Should()
					.Be(uriBuilder.Uri.ToString()
						.Replace("TestUser", "[REDACTED]")
						.Replace("TestPassword", "[REDACTED]"));
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
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task TestNotSuccessfulOutgoingHttpPostRequest()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create(ctx => { ctx.Response.StatusCode = 500; }))
			{
				var httpClient = new HttpClient();
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
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task CaptureErrorOnFailingHttpCall_HttpClient()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			{
				var httpClient = new HttpClient();

				Func<Task> act = async () => await httpClient.GetAsync("http://nonexistenturl_dsfdsf.ghkdehfn");
				await act.Should().ThrowAsync<Exception>();
			}

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();
		}

		/// <summary>
		/// Passes an exception to <see cref="HttpDiagnosticListener" /> and makes sure that the exception is captured
		/// Unlike the <see cref="CaptureErrorOnFailingHttpCall_HttpClient" /> method this does not use HttpClient, instead here we
		/// call the OnNext method directly.
		/// </summary>
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public void CaptureErrorOnFailingHttpCall_DirectCall()
		{
			var (disposableListener, payloadSender, agent) = RegisterListenerAndStartTransaction();

			using (disposableListener)
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

		/// <summary>
		/// Makes sure we set the correct type and subtype for external, http spans
		/// </summary>
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task SpanTypeAndSubtype()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			payloadSender.WaitForSpans();
			payloadSender.FirstSpan.Type.Should().Be(ApiConstants.TypeExternal);
			payloadSender.FirstSpan.Subtype.Should().Be(ApiConstants.SubtypeHttp);
			payloadSender.FirstSpan.Action.Should().BeNull(); //we don't set Action for HTTP calls
		}

		/// <summary>
		/// Makes sure we generate the correct span name
		/// </summary>
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task SpanName()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			payloadSender.WaitForSpans();
			payloadSender.FirstSpan.Name.Should().Be("GET localhost");
		}

		/// <summary>
		/// Makes sure that the duration of an HTTP Request is captured by the agent
		/// </summary>
		/// <returns>The request duration.</returns>
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task HttpRequestDuration()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create(ctx =>
			{
				ctx.Response.StatusCode = 200;
				Thread.Sleep(5); //Make sure duration is really > 0
			}))
			{
				var httpClient = new HttpClient();
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

		/// <summary>
		/// Makes sure spans have an Id
		/// </summary>
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task HttpRequestSpanGuid()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create())
			{
				var httpClient = new HttpClient();
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

		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		[SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
		public async Task HttpCallAsNestedSpan()
		{
			const string topSpanName = "test_top_span";
			const string topSpanType = "test_top_span_type";
			const int numberOfHttpCalls = 3;
			var (listener, payloadSender, agent) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = LocalServer.Create())
			{
				{
					var httpClient = new HttpClient();

					var topSpan = agent.Tracer.CurrentTransaction.StartSpan(topSpanName, topSpanType);

					await numberOfHttpCalls.Repeat(async i =>
					{
						var res = await httpClient.GetAsync($"{localServer.Uri}?i={i}");
						res.IsSuccessStatusCode.Should().BeTrue();
					});
					topSpan.End();
				}

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
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			using (var localServer = LocalServer.Create())
			{
				await agent.Tracer.CaptureTransaction("TestTransaction", "TestType", async t =>
				{
					Thread.Sleep(5);

					var httpClient = new HttpClient();
					try
					{
						await httpClient.GetAsync(localServer.Uri);
					}
					catch (Exception e)
					{
						t.CaptureException(e);
					}
				});

				mockPayloadSender.WaitForTransactions();
				mockPayloadSender.Transactions.Should().NotBeEmpty();
				mockPayloadSender.WaitForSpans(TimeSpan.FromSeconds(5));
				mockPayloadSender.SpansOnFirstTransaction.Should().BeEmpty();
			}
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

			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, logger: logger));
			agent.Subscribe(new HttpDiagnosticsSubscriber());
			StartTransaction(agent);

			using (var localServer = LocalServer.Create())
			using (var httpClient = new HttpClient())
			{
				var uriBuilder = new UriBuilder(localServer.Uri) { UserName = "TestUser289421", Password = "Password973243" };

				var res = await httpClient.GetAsync(uriBuilder.Uri);
				res.IsSuccessStatusCode.Should().BeTrue();
				logger.Lines.Should().NotBeEmpty();

				logger.Lines.Should().NotContain(n => n.Contains("TestUser289421"));
				logger.Lines.Should().NotContain(n => n.Contains("Password973243"));

				// looking for lines with "localhost:8082" and asserting that those contain [REDACTED].
				foreach (var lineWithHttpLog in logger.Lines.Where(n => n.Contains($"{uriBuilder.Host}:{uriBuilder.Port}")))
					lineWithHttpLog.Should().Contain("[REDACTED]");
			}
		}

		/// <summary>
		/// Make sure HttpDiagnosticSubscriber does not report spans after its disposed
		/// </summary>
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task SubscriptionOnlyRegistersSpansDuringItsLifeTime()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
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
		[NetCoreFact] //see: https://github.com/elastic/apm-agent-dotnet/issues/516
		public async Task HttpCallWithRegisteredListener()
		{
			var mockPayloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			var subscriber = new HttpDiagnosticsSubscriber();

			using (var localServer = LocalServer.Create())
			using (agent.Subscribe(subscriber))
			{
				var url = localServer.Uri;
				await agent.Tracer.CaptureTransaction("TestTransaction", "TestType", async t =>
				{
					Thread.Sleep(5);

					var httpClient = new HttpClient();
					try
					{
						await httpClient.GetAsync(url);
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
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			var subscriber = new HttpDiagnosticsSubscriber();

			using (var localServer = LocalServer.Create())
			{
				var url = localServer.Uri;
				using (agent.Subscribe(subscriber)) //subscribe
				{
					await agent.Tracer.CaptureTransaction("TestTransaction", "TestType", async t =>
					{
						Thread.Sleep(5);

						var httpClient = new HttpClient();
						try
						{
							await httpClient.GetAsync(url);
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

					var httpClient = new HttpClient();
					try
					{
						await httpClient.GetAsync(url);
					}
					catch (Exception e)
					{
						t.CaptureException(e);
					}
				});

				mockPayloadSender.WaitForTransactions();
				mockPayloadSender.FirstTransaction.Should().NotBeNull();
				mockPayloadSender.WaitForSpans(TimeSpan.FromSeconds(5));
				mockPayloadSender.SpansOnFirstTransaction.Should().BeEmpty();
			}
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
			new ApmAgent(new TestAgentComponents(logger, new MockConfigSnapshot(serviceVersion: "1.0"))).Subscribe(new HttpDiagnosticsSubscriber());

			// No active transaction, just an HTTP request with an active agent
			try
			{
				var httpClient = new HttpClient();
				await (await httpClient.GetAsync("https://elastic.co")).Content.ReadAsStringAsync();
			}
			catch
			{
				/*ignore - result of the request does not matter */
			}

			logger.Lines.Should().NotContain(line => line.ToLower().Contains("warn"));
		}

		/// <summary>
		/// Makes sure that in case of an async outgoing http call the caller method shows up in the captured callstack
		/// </summary>
		[NetCoreFact]
		public async Task CallStackContainsCallerMethod()
		{
			var (_, payloadSender, _) = RegisterListenerAndStartTransaction();

			try
			{
				using var localServer = LocalServer.Create();
				var httpClient = new HttpClient();
				await httpClient.GetAsync(localServer.Uri);
			}
			catch (Exception)
			{
				// ignore - only thing we care about in this stack is the stack trace.
			}

			payloadSender.WaitForSpans();
			payloadSender.FirstSpan.StackTrace.Should().NotBeNull();
			payloadSender.FirstSpan.StackTrace.Should().Contain(n => n.Function.Contains(nameof(CallStackContainsCallerMethod)));
		}

		[Fact]
		public async Task HttpCallWithW3CActivityFormar()
		{
			Activity.DefaultIdFormat = ActivityIdFormat.W3C;

			var mockPayloadSender = new MockPayloadSender();
			using var localServer = LocalServer.Create();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			agent.Subscribe(new HttpDiagnosticsSubscriber());
			await agent.Tracer.CaptureTransaction("Test", "Test", async () =>
			{
				var httpClient = new HttpClient();
				try
				{
					await httpClient.GetAsync(localServer.Uri);
				}
				catch
				{
					//ignore - we don't care about the result
				}
			});

			mockPayloadSender.WaitForSpans();
			mockPayloadSender.Spans.Should().HaveCount(1);
		}

		internal static (IDisposable, MockPayloadSender, ApmAgent) RegisterListenerAndStartTransaction()
		{
			var payloadSender = new MockPayloadSender();
			var agentComponents = new TestAgentComponents(payloadSender: payloadSender,
				config: new MockConfigSnapshot(logLevel: "Debug", stackTraceLimit: "-1",
					spanFramesMinDurationInMilliseconds: "-1ms"));

			var agent = new ApmAgent(agentComponents);
			var sub = agent.Subscribe(new HttpDiagnosticsSubscriber());
			StartTransaction(agent);

			return (sub, payloadSender, agent);
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private static void StartTransaction(ApmAgent agent)
			//	=> agent.TransactionContainer.Transactions.Value =
			//		new Transaction(agent, $"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);
			=> agent.Tracer.StartTransaction($"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);
	}
}
