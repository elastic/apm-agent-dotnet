using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	public class HttpDiagnosticListenerTests
	{
		private const string DummyExceptionMessage = "Dummy exception message";
		private readonly IApmLogger _logger;

		public HttpDiagnosticListenerTests(ITestOutputHelper xUnitOutputHelper) =>
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(HttpDiagnosticListenerTests));

		internal abstract class SimulationHelper
		{
			internal abstract IDiagnosticListener CreateListener(IApmAgent agent);

			internal abstract KeyValuePair<string, object> CreateStartEvent(HttpMethod method, Uri url, out object request,
				bool castPropertiesToObject = false
			);

			internal abstract KeyValuePair<string, object> CreateStartEventWithPayload(object payload);

			internal abstract KeyValuePair<string, object> CreateStopEvent(object request, HttpStatusCode statusCode,
				bool castPropertiesToObject = false
			);

			internal abstract KeyValuePair<string, object> CreateStopEventWithNullResponse<TRequest>(TRequest request,
				bool castPropertiesToObject = false
			);

			internal abstract KeyValuePair<string, object> CreateStopEventWithPayload(object payload);

			internal int ProcessingRequestsCount(IDiagnosticListener listener) =>
				((HttpDiagnosticListenerImplBase)listener).ProcessingRequests.Count;

			internal ISpan GetSpanForRequest(IDiagnosticListener listener, object request) =>
				((HttpDiagnosticListenerImplBase)listener).ProcessingRequests[request];

			internal KeyValuePair<string, object> CreateStartEventWithRequest<TRequest>(TRequest request, bool castPropertiesToObject = false) =>
				castPropertiesToObject
					? CreateStartEventWithPayload(new { Request = (object)request })
					: CreateStartEventWithPayload(new { Request = request });

			internal KeyValuePair<string, object> CreateStopEventWithResponse<TRequest, TResponse>(TRequest request, TResponse response,
				bool castPropertiesToObject = false
			) =>
				castPropertiesToObject
					? CreateStopEventWithPayload(new { Request = (object)request, Response = (object)response })
					: CreateStopEventWithPayload(new { Request = request, Response = response });
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData SimulationHelpersToTestImpls => new TheoryData<SimulationHelper>
		{
			new SimulationHelperCoreImpl(), new SimulationHelperFullFrameworkImpl()
		};

		/// <summary>
		/// Calls the OnError method on the HttpDiagnosticListener and makes sure that the correct error message is logged.
		/// </summary>
		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void OnError_call_should_be_logged(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			const string exceptionMessage = "Oops, this went wrong";
			var fakeException = new Exception(exceptionMessage);
			listener.OnError(fakeException);

			mockLogger.Lines.Should().NotBeEmpty();
			mockLogger.Lines.Should()
				.Contain(line => line.Contains("HttpDiagnosticListener") && line.Contains("in OnError") && line.Contains(".cs:"));
			mockLogger.Lines.Should().Contain(line => line.Contains(exceptionMessage));

			mockPayloadSender.IsEmpty.Should().BeTrue();
		}

		[Fact]
		public void InitializationFailed_event_should_be_logged()
		{
			var simulationHelper = new SimulationHelperFullFrameworkImpl();
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			const string exceptionMessage = "Oops, this went wrong";
			listener.OnNext(new KeyValuePair<string, object>(HttpDiagnosticListenerFullFrameworkImpl.InitializationFailedEventKey,
				new { Exception = new Exception(exceptionMessage) }));

			mockLogger.Lines.Should().Contain(line => line.Contains("HttpDiagnosticListener"));
			mockLogger.Lines.Should().Contain(line => line.Contains(HttpDiagnosticListenerFullFrameworkImpl.InitializationFailedEventKey));
			mockLogger.Lines.Should().Contain(line => line.Contains(exceptionMessage));

			mockPayloadSender.IsEmpty.Should().BeTrue();
		}

		/// <summary>
		/// Builds an HttpRequestMessage and calls HttpDiagnosticListener.OnNext directly with it.
		/// Makes sure that the processingRequests dictionary captures the ongoing transaction.
		/// </summary>
		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void OnNextWithStart(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper);

			var url = new Uri("https://elastic.co");

			//Simulate Start
			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request));
			simulationHelper.ProcessingRequestsCount(listener).Should().Be(1);
			simulationHelper.GetSpanForRequest(listener, request).Context.Http.Url.Should().Be(url.ToString());
			simulationHelper.GetSpanForRequest(listener, request).Context.Http.Method.Should().Be(HttpMethod.Get.ToString());

			mockPayloadSender.IsEmpty.Should().BeTrue();
		}

		/// <summary>
		/// Simulates the complete lifecycle of an HTTP request.
		/// It builds an HttpRequestMessage and an HttpResponseMessage
		/// and passes them to the OnNext method.
		/// Makes sure that a Span with an Http context is captured.
		/// </summary>
		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void OnNextWithStartAndStop(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper);

			var url = new Uri("https://some-random-site.com/some/path?query=1#fragment_B");

			//Simulate Start
			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request));
			//Simulate Stop
			listener.OnNext(simulationHelper.CreateStopEvent(request, HttpStatusCode.OK));
			simulationHelper.ProcessingRequestsCount(listener).Should().Be(0);

			var firstSpan = mockPayloadSender.FirstSpan;
			firstSpan.Should().NotBeNull();
			firstSpan.Context.Http.Url.Should().BeEquivalentTo(url.AbsoluteUri);
			firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
		}

		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void PayloadStaticPropertyTypeDoesntMatter(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper);

			var url = new Uri("https://some-random-site.com/some/path?query=1#fragment_B");

			//Simulate Start
			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request, /* castPropertiesToObject: */ true));
			//Simulate Stop
			listener.OnNext(simulationHelper.CreateStopEvent(request, HttpStatusCode.OK, /* castPropertiesToObject: */ true));
			simulationHelper.ProcessingRequestsCount(listener).Should().Be(0);

			var firstSpan = mockPayloadSender.FirstSpan;
			firstSpan.Should().NotBeNull();
			firstSpan.Context.Http.Url.Should().BeEquivalentTo(url.ToString());
			firstSpan.Context.Http.Method.Should().Be(HttpMethod.Get.Method);
		}

		/// <summary>
		/// Calls OnNext with System.Net.Http.HttpRequestOut.Stop twice.
		/// Makes sure that the transaction is only captured once and the span is also only captured once.
		/// Also make sure that there is an error log.
		/// </summary>
		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void OnNextWithStartAndStopTwice(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			var url = new Uri("https://elastic.co");

			//Simulate Start
			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request));
			//Simulate Stop
			listener.OnNext(simulationHelper.CreateStopEvent(request, HttpStatusCode.OK));
			//Simulate Stop again. This should not happen, still we test for this.
			listener.OnNext(simulationHelper.CreateStopEvent(request, HttpStatusCode.OK));

			mockLogger.Lines.Should().NotBeEmpty();
			mockLogger.Lines
				.Should()
				.Contain(
					line => line.Contains("HttpDiagnosticListener")
						&& line.Contains("Warning")
						&& line.Contains("Failed to remove from ProcessingRequests")
						&& line.Contains(HttpMethod.Get.Method)
						&& line.Contains(url.AbsoluteUri));

			mockPayloadSender.Spans.Should().ContainSingle();
			mockPayloadSender.FirstSpan.Context.Http.Url.Should().BeEquivalentTo(url.ToString());
		}

		/// <summary>
		/// Calls HttpDiagnosticListener.OnNext with types that are unknown.
		/// The test makes sure that in this case still no exception is thrown from the OnNext method.
		/// </summary>
		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void UnexpectedPayloadInStartEvent(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper);

			var myFake = new StringBuilder(); //just a random type that is not planned to be passed into OnNext

			var exception =
				Record.Exception(() => { listener.OnNext(simulationHelper.CreateStartEventWithPayload(myFake)); });

			exception.Should().BeNull();

			mockPayloadSender.IsEmpty.Should().BeTrue();
		}

		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void UnexpectedRequestValueInStartEvent(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper);
			var myFake = new StringBuilder(); //just a random type that is not planned to be passed into OnNext

			var exception =
				Record.Exception(() => { listener.OnNext(simulationHelper.CreateStartEventWithRequest(myFake)); });

			exception.Should().BeNull();

			mockPayloadSender.IsEmpty.Should().BeTrue();
		}

		/// <summary>
		/// Passes null instead of a valid HttpRequestMessage into HttpDiagnosticListener.OnNext
		/// and makes sure that still no exception is thrown.
		/// </summary>
		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void NullPayloadInStartEvent(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			listener.OnNext(simulationHelper.CreateStartEventWithPayload(null));

			simulationHelper.ProcessingRequestsCount(listener).Should().Be(0);

			mockPayloadSender.IsEmpty.Should().BeTrue();

			mockLogger.Lines.Should().Contain(line => line.Contains("HttpDiagnosticListener") && line.Contains("null"));
		}

		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void NullPayloadInStopEvent(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			var url = new Uri($"https://some-random-site.com/some/path/?query=1#{GetCurrentMethodName()}");

			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request));
			listener.OnNext(simulationHelper.CreateStopEventWithPayload(null));

			simulationHelper.ProcessingRequestsCount(listener).Should().Be(1);
			simulationHelper.GetSpanForRequest(listener, request).Context.Http.Url.Should().Be(url.ToString());

			mockPayloadSender.IsEmpty.Should().BeTrue();

			mockLogger.Lines.Should().Contain(line => line.Contains("HttpDiagnosticListener") && line.Contains("null"));
		}

		/// <summary>
		/// Passes a null key with null value instead of a valid HttpRequestMessage into HttpDiagnosticListener.OnNext
		/// and makes sure that still no exception is thrown.
		/// </summary>
		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void NullKeyValueToOnNext(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			var exception =
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>(null, null)); });

			exception.Should().BeNull();

			mockPayloadSender.IsEmpty.Should().BeTrue();

			mockLogger.Lines.Should().Contain(line => line.Contains("HttpDiagnosticListener") && line.Contains("null"));
		}

		/// <summary>
		/// Sends a simple real HTTP GET message and makes sure that
		/// HttpDiagnosticListener captures it.
		/// </summary>
		[Fact]
		public async Task TestSimpleOutgoingHttpRequest()
		{
			var (listener, mockPayloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
				var firstSpan = mockPayloadSender.FirstSpan;
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
			var (listener, mockPayloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer(ctx => { ctx.Response.StatusCode = 500; }))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.PostAsync(localServer.Uri, new StringContent("foo"));

				res.IsSuccessStatusCode.Should().BeFalse();
				var firstSpan = mockPayloadSender.FirstSpan;
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
			var (listener, mockPayloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			{
				var httpClient = new HttpClient();

				Func<Task> act = async () => await httpClient.GetAsync("http://nonexistenturl_dsfdsf.ghkdehfn");
				await act.Should().ThrowAsync<HttpRequestException>();
			}

			mockPayloadSender.Spans.Should().HaveCount(1);
			mockPayloadSender.FirstSpan.Context.Http.StatusCode.Should().BeNull();
			mockPayloadSender.Errors.Should().HaveCount(1);
			mockPayloadSender.FirstError.Exception.Message.Should().Contain("No such host is known");
			mockPayloadSender.FirstError.ParentId.Should().Be(mockPayloadSender.FirstSpan.Id);
		}

		/// <summary>
		/// Passes an exception to <see cref="HttpDiagnosticListener" /> and makes sure that the exception is captured
		/// Unlike the <see cref="CaptureErrorOnFailingHttpCall_HttpClient" /> method this does not use HttpClient, instead here we
		/// call the OnNext method directly.
		/// </summary>
		[Fact]
		public void CaptureErrorOnFailingHttpCall_DirectCall()
		{
			var simulationHelper = new SimulationHelperCoreImpl();

			var (disposableListener, mockPayloadSender, agent) = RegisterListenerAndStartTransaction();

			using (disposableListener)
			{
				var listener = simulationHelper.CreateListener(agent);

				var url = new Uri("https://elastic.co");

				// Simulate Start - this test case is .NET Core specific (because Full Framework doesn't have _Exception_ event)
				// so we use key string from HttpDiagnosticListenerCoreImpl
				listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request));

				const string exceptionMsg = "Sample exception msg";
				var exception = new Exception(exceptionMsg);
				//Simulate Exception
				listener.OnNext(new KeyValuePair<string, object>(HttpDiagnosticListenerCoreImpl.ExceptionEventKey,
					new { Request = request, Exception = exception }));

				//Simulate Stop
				listener.OnNext(simulationHelper.CreateStopEventWithResponse(request, (HttpResponseMessage)null));

				mockPayloadSender.Spans.Should().HaveCount(1);
				mockPayloadSender.FirstSpan.Context.Http.StatusCode.Should().BeNull();
				mockPayloadSender.Errors.Should().HaveCount(1);
				mockPayloadSender.FirstError.Exception.Message.Should().Be(exceptionMsg);
				mockPayloadSender.FirstError.Exception.Type.Should().Be(typeof(Exception).FullName);
				mockPayloadSender.FirstError.ParentId.Should().Be(mockPayloadSender.FirstSpan.Id);
			}
		}

		/// <summary>
		/// Makes sure we set the correct type and subtype for external, http spans
		/// </summary>
		[Fact]
		public async Task SpanTypeAndSubtype()
		{
			var (listener, mockPayloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			mockPayloadSender.FirstSpan.Type.Should().Be(ApiConstants.TypeExternal);
			mockPayloadSender.FirstSpan.Subtype.Should().Be(ApiConstants.SubtypeHttp);
			mockPayloadSender.FirstSpan.Action.Should().BeNull(); //we don't set Action for HTTP calls
		}

		/// <summary>
		/// Makes sure we generate the correct span name
		/// </summary>
		[Fact]
		public async Task SpanName()
		{
			var (listener, mockPayloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			mockPayloadSender.FirstSpan.Name.Should().Be("GET localhost");
		}

		/// <summary>
		/// Makes sure that the duration of an HTTP Request is captured by the agent
		/// </summary>
		/// <returns>The request duration.</returns>
		[Fact]
		public async Task HttpRequestDuration()
		{
			var (listener, mockPayloadSender, _) = RegisterListenerAndStartTransaction();

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
				var firstSpan = mockPayloadSender.FirstSpan;
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
			var (listener, mockPayloadSender, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
				var firstSpan = mockPayloadSender.FirstSpan;
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
			var (listener, mockPayloadSender, agent) = RegisterListenerAndStartTransaction();

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

				mockPayloadSender.Spans.Should().HaveCount(numberOfHttpCalls + 1);
				var topSpanSent = mockPayloadSender.Spans.Last();
				topSpanSent.Name.Should().Be(topSpanName);
				topSpanSent.Type.Should().Be(topSpanType);
				numberOfHttpCalls.Repeat(i =>
				{
					var httpCallSpan = mockPayloadSender.Spans[i];
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
			var mockPayloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));
			StartTransaction(agent);

			var spans = mockPayloadSender.Spans;

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
				spans = mockPayloadSender.Spans;
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

		[Theory]
		[InlineData(typeof(ThrowingFullFrameworkImpl))]
		[InlineData(typeof(ThrowingCoreImpl))]
		public void NoExceptionEscapesFromOnNext(Type throwingImplType)
		{
			var mockLogger = new TestLogger();
			var mockPayloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(new SplittingLogger(mockLogger, _logger), payloadSender: mockPayloadSender));
			var args = new object[] { agent };
			var listener = (HttpDiagnosticListenerImplBase)Activator.CreateInstance(throwingImplType,
				// ReSharper disable RedundantCast
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, (Binder)null, args,
				(CultureInfo)null
				// ReSharper restore RedundantCast
			);

			listener.OnNext(new KeyValuePair<string, object>("dummy event key", new { Value = "dummy event value" }));

			mockPayloadSender.IsEmpty.Should().BeTrue();

			mockLogger.Lines.Should().Contain(line => line.Contains("HttpDiagnosticListener") && line.Contains("OnNext"));
			mockLogger.Lines.Should().Contain(line => line.Contains(DummyExceptionMessage));
		}

		private static string GetCurrentMethodName([CallerMemberName] string callerMemberName = "") => callerMemberName;

		[Theory]
		[InlineData(HttpStatusCode.Accepted, false)]
		[InlineData(HttpStatusCode.Accepted, true)]
		[InlineData(HttpStatusCode.Redirect, false)]
		[InlineData(HttpStatusCode.BadRequest, true)]
		[InlineData(HttpStatusCode.Forbidden, false)]
		[InlineData(HttpStatusCode.InternalServerError, true)]
		[InlineData(HttpStatusCode.ServiceUnavailable, true)]
		internal void StopEx_event(HttpStatusCode statusCode, bool castPropertiesToObject)
		{
			var simulationHelper = new SimulationHelperFullFrameworkImpl();
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper);

			var url = new Uri($"https://some-random-site.com/some/path/{GetCurrentMethodName()}?query=1#fragment_B");

			// Simulate Start
			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Post, url, out var request));

			// Simulate StopEx - this test case is Full Framework specific (because .NET Core doesn't have _StopEx_ event)
			listener.OnNext(SimulationHelperFullFrameworkImpl.CreateStopExEvent(request, statusCode, castPropertiesToObject));

			mockPayloadSender.Spans.Should().HaveCount(1);
			mockPayloadSender.FirstSpan.Context.Http.Url.Should().Be(url.ToString());
			mockPayloadSender.FirstSpan.Context.Http.StatusCode.Should().Be((int)statusCode);
			mockPayloadSender.Errors.Should().BeEmpty();
		}

		[Fact]
		internal void StopEx_event_null_payload()
		{
			var simulationHelper = new SimulationHelperFullFrameworkImpl();
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper);

			var url = new Uri($"https://some-random-site.com/some/path/?query=1#{GetCurrentMethodName()}");

			//Simulate Start
			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request));

			listener.OnNext(SimulationHelperFullFrameworkImpl.CreateStopExEventWithPayload(null));

			simulationHelper.ProcessingRequestsCount(listener).Should().Be(1);
			simulationHelper.GetSpanForRequest(listener, request).Context.Http.Url.Should().Be(url.ToString());

			mockPayloadSender.IsEmpty.Should().BeTrue();
		}

		[Theory]
		[InlineData((object)null)]
		[InlineData("")]
		internal void StopEx_event_unexpected_StatusCode_property_type<TStatusCode>(TStatusCode statusCode)
		{
			var simulationHelper = new SimulationHelperFullFrameworkImpl();
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			var url = new Uri($"https://some-random-site.com/some/path/?query=1#{GetCurrentMethodName()}");

			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Get, url, out var request));
			listener.OnNext(SimulationHelperFullFrameworkImpl.CreateStopExEventWithPayload(new { Request = request, StatusCode = statusCode }));

			simulationHelper.ProcessingRequestsCount(listener).Should().Be(0);

			mockPayloadSender.Spans.Should().HaveCount(1);
			mockPayloadSender.FirstSpan.Context.Http.Url.Should().Be(url.ToString());
			mockPayloadSender.FirstSpan.Context.Http.StatusCode.Should().Be(null);
			mockPayloadSender.Errors.Should().BeEmpty();

			mockLogger.Lines.Should().Contain(line => line.Contains("HttpDiagnosticListener") && line.Contains("StatusCode"));
			mockLogger.Lines.Should().Contain(line => line.Contains("HTTP status code") && line.Contains("null"));
		}

		[Theory]
		[MemberData(nameof(SimulationHelpersToTestImpls))]
		internal void Stop_event_null_Response(SimulationHelper simulationHelper)
		{
			var (listener, mockPayloadSender, mockLogger) = PrepareSimulationEnvironmentWithLogger(simulationHelper);

			var url = new Uri($"https://some-random-site.com/some/path/{GetCurrentMethodName()}?query=1#fragment_B");

			// Simulate Start
			listener.OnNext(simulationHelper.CreateStartEvent(HttpMethod.Post, url, out var request));

			// Simulate Stop
			listener.OnNext(simulationHelper.CreateStopEventWithNullResponse(request));

			mockPayloadSender.Spans.Should().ContainSingle();
			mockPayloadSender.FirstSpan.Context.Http.StatusCode.Should().BeNull();
			mockPayloadSender.Errors.Should().BeEmpty();

			mockLogger.Lines.Should().Contain(line => line.Contains(nameof(HttpDiagnosticListenerImplBase)));
			mockLogger.Lines.Should().Contain(line => line.Contains(url.ToString()));
			mockLogger.Lines.Should().NotContain(line => line.Contains(nameof(HttpDiagnosticListenerImplBase.FailedToExtractPropertyException)));
		}

		private (IDiagnosticListener, MockPayloadSender, TestLogger) PrepareSimulationEnvironmentWithLogger(SimulationHelper simulationHelper)
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var (listener, mockPayloadSender) = PrepareSimulationEnvironment(simulationHelper, new SplittingLogger(mockLogger, _logger));
			return (listener, mockPayloadSender, mockLogger);
		}

		private (IDiagnosticListener, MockPayloadSender) PrepareSimulationEnvironment(SimulationHelper simulationHelper, IApmLogger logger = null)
		{
			var mockPayloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(logger ?? _logger, payloadSender: mockPayloadSender));
			var listener = simulationHelper.CreateListener(agent);
			StartTransaction(agent);

			return (listener, mockPayloadSender);
		}

		private static (IDisposable, MockPayloadSender, ApmAgent) RegisterListenerAndStartTransaction()
		{
			var mockPayloadSender = new MockPayloadSender();
			var agentComponents = new TestAgentComponents(payloadSender: mockPayloadSender,
				configurationReader: new TestAgentConfigurationReader(logLevel: "Debug", stackTraceLimit: "-1",
					spanFramesMinDurationInMilliseconds: "-1ms"));

			var agent = new ApmAgent(agentComponents);
			var sub = agent.Subscribe(new HttpDiagnosticsSubscriber());
			StartTransaction(agent);

			return (sub, mockPayloadSender, agent);
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private static void StartTransaction(ApmAgent agent)
			//	=> agent.TransactionContainer.Transactions.Value =
			//		new Transaction(agent, $"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);
			=> agent.Tracer.StartTransaction($"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);

		private class SimulationHelperCoreImpl : SimulationHelper
		{
			internal override IDiagnosticListener CreateListener(IApmAgent agent) => new HttpDiagnosticListenerCoreImpl(agent);

			internal override KeyValuePair<string, object> CreateStartEventWithPayload(object payload) =>
				new KeyValuePair<string, object>(HttpDiagnosticListenerCoreImpl.StartEventKey, payload);

			internal override KeyValuePair<string, object> CreateStartEvent(HttpMethod method, Uri url, out object request,
				bool castPropertiesToObject = false
			)
			{
				var httpRequestMessage = new HttpRequestMessage(method, url);
				request = httpRequestMessage;
				return CreateStartEventWithRequest(httpRequestMessage, castPropertiesToObject);
			}

			internal override KeyValuePair<string, object> CreateStopEventWithPayload(object payload) =>
				new KeyValuePair<string, object>(HttpDiagnosticListenerCoreImpl.StopEventKey, payload);

			internal override KeyValuePair<string, object> CreateStopEvent(object request, HttpStatusCode statusCode,
				bool castPropertiesToObject = false
			) =>
				castPropertiesToObject
					? CreateStopEventWithResponse(request, (object)new HttpResponseMessage(statusCode))
					: CreateStopEventWithResponse((HttpRequestMessage)request, new HttpResponseMessage(statusCode));

			internal override KeyValuePair<string, object> CreateStopEventWithNullResponse<TRequest>(TRequest request,
				bool castPropertiesToObject = false
			) => CreateStopEventWithResponse(request, (HttpResponseMessage)null, castPropertiesToObject);
		}

		private class SimulationHelperFullFrameworkImpl : SimulationHelper
		{
			internal override IDiagnosticListener CreateListener(IApmAgent agent) => new HttpDiagnosticListenerFullFrameworkImpl(agent);

			internal override KeyValuePair<string, object> CreateStartEventWithPayload(object payload) =>
				new KeyValuePair<string, object>(HttpDiagnosticListenerFullFrameworkImpl.StartEventKey, payload);

			internal override KeyValuePair<string, object> CreateStartEvent(HttpMethod method, Uri url, out object request,
				bool castPropertiesToObject = false
			)
			{
				var httpWebRequest = WebRequest.CreateHttp(url);
				httpWebRequest.Method = method.Method;
				request = httpWebRequest;
				return CreateStartEventWithRequest(httpWebRequest, castPropertiesToObject);
			}

			internal override KeyValuePair<string, object> CreateStopEventWithPayload(object payload) =>
				new KeyValuePair<string, object>(HttpDiagnosticListenerFullFrameworkImpl.StopEventKey, payload);

			internal override KeyValuePair<string, object> CreateStopEvent(object request, HttpStatusCode statusCode,
				bool castPropertiesToObject = false
			) =>
				castPropertiesToObject
					? CreateStopEventWithResponse(request, (object)new MockHttpWebResponse { MockStatusCode = statusCode })
					: CreateStopEventWithResponse((HttpWebRequest)request, new MockHttpWebResponse { MockStatusCode = statusCode });

			internal static KeyValuePair<string, object> CreateStopExEventWithPayload(object payload) =>
				new KeyValuePair<string, object>(HttpDiagnosticListenerFullFrameworkImpl.StopExEventKey, payload);

			internal static KeyValuePair<string, object> CreateStopExEvent(object request, HttpStatusCode statusCode,
				bool castPropertiesToObject = false
			) =>
				castPropertiesToObject
					? CreateStopExEventWithPayload(new { Request = request, StatusCode = statusCode })
					: CreateStopExEventWithPayload(new { Request = (HttpWebRequest)request, StatusCode = statusCode });

			internal override KeyValuePair<string, object> CreateStopEventWithNullResponse<TRequest>(TRequest request,
				bool castPropertiesToObject = false
			) => CreateStopEventWithResponse(request, (HttpWebResponse)null, castPropertiesToObject);
		}

		private class MockHttpWebResponse : HttpWebResponse
		{
			// ReSharper disable once MemberCanBePrivate.Local
			internal HttpStatusCode MockStatusCode { get; set; }
			public override HttpStatusCode StatusCode => MockStatusCode;
		}

		private class ThrowingFullFrameworkImpl : HttpDiagnosticListenerFullFrameworkImpl
		{
			internal ThrowingFullFrameworkImpl(IApmAgent agent) : base(agent) { }

			protected override bool DispatchEventProcessing(KeyValuePair<string, object> _) => throw new Exception(DummyExceptionMessage);
		}

		private class ThrowingCoreImpl : HttpDiagnosticListenerCoreImpl
		{
			internal ThrowingCoreImpl(IApmAgent agent) : base(agent) { }

			protected override bool DispatchEventProcessing(KeyValuePair<string, object> _) => throw new Exception(DummyExceptionMessage);
		}
	}
}
