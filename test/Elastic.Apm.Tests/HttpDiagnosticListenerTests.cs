using System;
using System.Collections.Generic;
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
			DispatchToImpl(listener, impl => HttpDiagnosticListenerCoreImpl.StartEventKey,
				impl => HttpDiagnosticListenerFullFrameworkImpl.StartEventKey);

		private static string StopEventKey(IDiagnosticListener listener) =>
			DispatchToImpl(listener, impl => HttpDiagnosticListenerCoreImpl.StopEventKey,
				impl => HttpDiagnosticListenerFullFrameworkImpl.StopEventKey);

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
		[Fact]
		public void OnNextWithStart()
		{
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger));
			StartTransaction(agent);
			var listener = HttpDiagnosticListener.New(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>(StartEventKey(listener), new { Request = request }));
			ProcessingRequestsCount(listener).Should().Be(1);
			GetSpanForRequest(listener, request).Context.Http.Url.Should().Be(request.RequestUri.ToString());
			GetSpanForRequest(listener, request).Context.Http.Method.Should().Be(HttpMethod.Get.ToString());
		}

		/// <summary>
		/// Simulates the complete lifecycle of an HTTP request.
		/// It builds an HttpRequestMessage and an HttpResponseMessage
		/// and passes them to the OnNext method.
		/// Makes sure that a Span with an Http context is captured.
		/// </summary>
		[Fact]
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
		}

		/// <summary>
		/// Calls OnNext with System.Net.Http.HttpRequestOut.Stop twice.
		/// Makes sure that the transaction is only captured once and the span is also only captured once.
		/// Also make sure that there is an error log.
		/// </summary>
		[Fact]
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
					line => line.Contains("HttpDiagnosticListener") && line.Contains("Failed to remove from ProcessingRequests")
						&& line.Contains(HttpMethod.Get.Method)
						&& line.Contains(request.RequestUri.AbsoluteUri));
			payloadSender.Transactions.Should().NotBeNull();
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
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>(StartEventKey(listener), myFake)); });

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
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>(StartEventKey(listener), null)); });

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
		[Fact]
		public async Task TestSimpleOutgoingHttpRequest()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
				var firstSpan = payloadSender.FirstSpan;
				firstSpan.Should().NotBeNull();
				firstSpan.Context.Http.Url.Should().Be(localServer.Uri);
				firstSpan.Context.Http.StatusCode.Should().Be(200);
				firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
			}
		}

		/// <summary>
		/// Sends a simple real HTTP POST message and the server responds with 500
		/// The test makes sure HttpDiagnosticListener captures the POST method and
		/// the response code correctly
		/// </summary>
		[Fact]
		public async Task TestNotSuccessfulOutgoingHttpPostRequest()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer(ctx => { ctx.Response.StatusCode = 500; }))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.PostAsync(localServer.Uri, new StringContent("foo"));

				res.IsSuccessStatusCode.Should().BeFalse();
				var firstSpan = payloadSender.FirstSpan;
				firstSpan.Should().NotBeNull();
				firstSpan.Context.Http.Url.Should().Be(localServer.Uri);
				firstSpan.Context.Http.StatusCode.Should().Be(500);
				firstSpan.Context.Http.Method.Should().Be(HttpMethod.Post.Method);
			}
		}

		/// <summary>
		/// Starts an HTTP call to a non existing URL and makes sure that an error is captured.
		/// This uses an HttpClient instance directly
		/// </summary>
		[Fact]
		public async Task CaptureErrorOnFailingHttpCall_HttpClient()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			{
				var httpClient = new HttpClient();

				Func<Task> act = async () => await httpClient.GetAsync("http://nonexistenturl_dsfdsf.ghkdehfn");
				await act.Should().ThrowAsync<HttpRequestException>();
			}

			payloadSender.Spans.Should().HaveCount(1);
			payloadSender.FirstSpan.Context.Http.StatusCode.Should().BeNull();
			payloadSender.Errors.Should().HaveCount(1);
			payloadSender.FirstError.Exception.Message.Should().Contain("No such host is known");
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstSpan.Id);
		}

		/// <summary>
		/// Passes an exception to <see cref="HttpDiagnosticListener" /> and makes sure that the exception is captured
		/// Unlike the <see cref="CaptureErrorOnFailingHttpCall_HttpClient" /> method this does not use HttpClient, instead here we
		/// call the OnNext method directly.
		/// </summary>
		[Fact]
		public void CaptureErrorOnFailingHttpCall_DirectCall()
		{
			var (disposableListener, payloadSender, agent) = RegisterListenerAndStartTransaction();

			using (disposableListener)
			{
				var listener = HttpDiagnosticListener.New(agent);

				var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");

				// Simulate Start - this test case is .NET Core specific (because Full Framework doesn't have _Exception_ event)
				// so we use key string from HttpDiagnosticListenerCoreImpl
				listener.OnNext(new KeyValuePair<string, object>(HttpDiagnosticListenerCoreImpl.StartEventKey, new { Request = request }));

				const string exceptionMsg = "Sample exception msg";
				var exception = new Exception(exceptionMsg);
				//Simulate Exception
				listener.OnNext(new KeyValuePair<string, object>(HttpDiagnosticListenerCoreImpl.ExceptionEventKey,
					new { Request = request, Exception = exception }));
				listener.OnNext(new KeyValuePair<string, object>(HttpDiagnosticListenerCoreImpl.StopEventKey,
					new { Request = request, Response = (HttpResponseMessage)null }));

				payloadSender.Spans.Should().HaveCount(1);
				payloadSender.FirstSpan.Context.Http.StatusCode.Should().BeNull();
				payloadSender.Errors.Should().HaveCount(1);
				payloadSender.FirstError.Exception.Message.Should().Be(exceptionMsg);
				payloadSender.FirstError.Exception.Type.Should().Be(typeof(Exception).FullName);
				payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstSpan.Id);
			}
		}

		/// <summary>
		/// Makes sure we set the correct type and subtype for external, http spans
		/// </summary>
		[Fact]
		public async Task SpanTypeAndSubtype()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			payloadSender.FirstSpan.Type.Should().Be(ApiConstants.TypeExternal);
			payloadSender.FirstSpan.Subtype.Should().Be(ApiConstants.SubtypeHttp);
			payloadSender.FirstSpan.Action.Should().BeNull(); //we don't set Action for HTTP calls
		}

		/// <summary>
		/// Makes sure we generate the correct span name
		/// </summary>
		[Fact]
		public async Task SpanName()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			payloadSender.FirstSpan.Name.Should().Be("GET localhost");
		}

		/// <summary>
		/// Makes sure that the duration of an HTTP Request is captured by the agent
		/// </summary>
		/// <returns>The request duration.</returns>
		[Fact]
		public async Task HttpRequestDuration()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer(ctx =>
			{
				ctx.Response.StatusCode = 200;
				Thread.Sleep(5); //Make sure duration is really > 0
			}))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
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
		[Fact]
		public async Task HttpRequestSpanGuid()
		{
			var (listener, payloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
				var firstSpan = payloadSender.FirstSpan;
				firstSpan.Should().NotBeNull();
				firstSpan.Context.Http.Url.Should().Be(localServer.Uri);
				firstSpan.Context.Http.StatusCode.Should().Be(200);
				firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
				firstSpan.Duration.Should().BeGreaterThan(0);
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
		public async Task HttpCallAsNestedSpan()
		{
			const string topSpanName = "test_top_span";
			const string topSpanType = "test_top_span_type";
			const int numberOfHttpCalls = 3;
			var (listener, payloadSender, agent) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
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

				payloadSender.Spans.Should().HaveCount(numberOfHttpCalls + 1);
				var topSpanSent = payloadSender.Spans.Last();
				topSpanSent.Name.Should().Be(topSpanName);
				topSpanSent.Type.Should().Be(topSpanType);
				numberOfHttpCalls.Repeat(i =>
				{
					var httpCallSpan = payloadSender.Spans[i];
					httpCallSpan.Should().NotBeNull();
					// ReSharper disable once AccessToDisposedClosure
					httpCallSpan.Context.Http.Url.Should().Be($"{localServer.Uri}?i={i}");
					httpCallSpan.Context.Http.StatusCode.Should().Be(200);
					httpCallSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
					httpCallSpan.Duration.Should().BeGreaterThan(0);
					topSpanSent.Duration.Value.Should().BeGreaterOrEqualTo(httpCallSpan.Duration.Value);
					httpCallSpan.ParentId.Should().Be(topSpanSent.Id);
				});
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

			using (var localServer = new LocalServer())
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

				mockPayloadSender.Transactions.Should().NotBeEmpty();
				mockPayloadSender.SpansOnFirstTransaction.Should().BeEmpty();
			}
		}

		/// <summary>
		/// Make sure HttpDiagnosticSubscriber does not report spans after its disposed
		/// </summary>
		[Fact]
		public async Task SubscriptionOnlyRegistersSpansDuringItsLifeTime()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			StartTransaction(agent);

			var spans = payloadSender.Spans;

			using (var localServer = new LocalServer())
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
		[Fact]
		public async Task HttpCallWithRegisteredListener()
		{
			var mockPayloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			var subscriber = new HttpDiagnosticsSubscriber();

			using (var localServer = new LocalServer())
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

				mockPayloadSender.Transactions.Should().NotBeEmpty();
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

			using (var localServer = new LocalServer())
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

				mockPayloadSender.FirstTransaction.Should().NotBeNull();
				mockPayloadSender.SpansOnFirstTransaction.Should().BeEmpty();
			}
		}

		internal static (IDisposable, MockPayloadSender, ApmAgent) RegisterListenerAndStartTransaction()
		{
			var payloadSender = new MockPayloadSender();
			var agentComponents = new TestAgentComponents(payloadSender: payloadSender,
				configurationReader: new TestAgentConfigurationReader(logLevel: "Debug", stackTraceLimit: "-1",
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
