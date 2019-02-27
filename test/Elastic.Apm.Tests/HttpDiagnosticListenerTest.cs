using System;
using System.Collections.Generic;
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
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class HttpDiagnosticListenerTest
	{
		/// <summary>
		/// Calls the OnError method on the HttpDiagnosticListener and makes sure that the correct error message is logged.
		/// </summary>
		[Fact]
		public void OnErrorLog()
		{
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger));
			var listener = new HttpDiagnosticListener(agent);

			var exceptionMessage = "Ooops, this went wrong";
			var fakeException = new Exception(exceptionMessage);
			listener.OnError(fakeException);

			Assert.Equal($"{{{nameof(HttpDiagnosticListener)}}} {nameof(Exception)} in OnError ({nameof(HttpDiagnosticListener)}.cs:38): {exceptionMessage}",
				logger.Lines?.FirstOrDefault());
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
			var listener = new HttpDiagnosticListener(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", new { Request = request }));
			Assert.Single(listener.ProcessingRequests);
			Assert.Equal(request.RequestUri.ToString(), listener.ProcessingRequests[request].Context.Http.Url);
			Assert.Equal(HttpMethod.Get.ToString(), listener.ProcessingRequests[request].Context.Http.Method);
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
			var listener = new HttpDiagnosticListener(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");
			var response = new HttpResponseMessage(HttpStatusCode.OK);

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", new { Request = request }));
			//Simulate Stop
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Stop", new { Request = request, Response = response }));
			Assert.Empty(listener.ProcessingRequests);

			Assert.Equal(request.RequestUri.ToString(), (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Url);
			Assert.Equal(HttpMethod.Get.ToString(), (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Method);
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
			var agent = new ApmAgent(new TestAgentComponents(logger));
			StartTransaction(agent);
			var listener = new HttpDiagnosticListener(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");
			var response = new HttpResponseMessage(HttpStatusCode.OK);

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", new { Request = request }));
			//Simulate Stop
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Stop", new { Request = request, Response = response }));
			//Simulate Stop again. This should not happen, still we test for this.
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Stop", new { Request = request, Response = response }));

			Assert.Equal(
				$"{{{nameof(HttpDiagnosticListener)}}} Failed capturing request '{HttpMethod.Get} {request.RequestUri}' in System.Net.Http.HttpRequestOut.Stop. This Span will be skipped in case it wasn't captured before.",
				logger.Lines[0]);
			Assert.NotNull(Agent.TransactionContainer.Transactions.Value);
			Assert.Single(Agent.TransactionContainer.Transactions.Value.Spans);
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
			var listener = new HttpDiagnosticListener(agent);
			var myFake = new StringBuilder(); //just a random type that is not planned to be passed into OnNext

			var exception =
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", myFake)); });

			Assert.Null(exception);
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
			var listener = new HttpDiagnosticListener(agent);

			var exception =
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", null)); });

			Assert.Null(exception);
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
			var listener = new HttpDiagnosticListener(agent);

			var exception =
				Record.Exception(() => { listener.OnNext(new KeyValuePair<string, object>(null, null)); });

			Assert.Null(exception);
		}

		/// <summary>
		/// Sends a simple real HTTP GET message and makes sure that
		/// HttpDiagnosticListener captures it.
		/// </summary>
		[Fact]
		public async Task TestSimpleOutgoingHttpRequest()
		{
			var (listener, _, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Url);
			}

			Assert.Equal(200, (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.StatusCode);
			Assert.Equal(HttpMethod.Get.ToString(), (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Method);
		}

		/// <summary>
		/// Sends a simple real HTTP POST message and the server responds with 500
		/// The test makes sure HttpDiagnosticListener captures the POST method and
		/// the response code correctly
		/// </summary>
		[Fact]
		public async Task TestNotSuccessfulOutgoingHttpPostRequest()
		{
			var (listener, _, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer(ctx => { ctx.Response.StatusCode = 500; }))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.PostAsync(localServer.Uri, new StringContent("foo"));

				Assert.False(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Url);
			}

			Assert.Equal(500, (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.StatusCode);
			Assert.Equal(HttpMethod.Post.ToString(), (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Method);
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
				try
				{
					await httpClient.GetAsync("http://nonexistenturl_dsfdsf.ghkdehfn");
					Assert.True(false); //Make it fail if no exception is thrown
				}
				catch (Exception e)
				{
					Assert.NotNull(e);
				}
				finally
				{
					listener.Dispose();
				}
			}

			Assert.NotEmpty(payloadSender.Errors);
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
				var listener = new HttpDiagnosticListener(agent);

				var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");

				//Simulate Start
				listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", new { Request = request }));

				var exceptionMsg = "Sample exception msg";
				var exception = new Exception(exceptionMsg);
				//Simulate Exception
				listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.Exception", new { Request = request, Exception = exception }));

				Assert.NotEmpty(payloadSender.Errors);
				Assert.Equal(exceptionMsg, payloadSender.Errors[0].Errors[0].Exception.Message);
				Assert.Equal(typeof(Exception).FullName, payloadSender.Errors[0].Errors[0].Exception.Type);
			}
		}

		/// <summary>
		/// Makes sure we set the correct type and subtype for external, http spans
		/// </summary>
		[Fact]
		public async Task SpanTypeAndSubtype()
		{
			var (listener, _, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
			}

			Assert.Equal(ApiConstants.TypeExternal, Agent.TransactionContainer.Transactions.Value.Spans[0].Type);
			Assert.Equal(ApiConstants.SubtypeHttp, Agent.TransactionContainer.Transactions.Value.Spans[0].Subtype);
			Assert.Null(Agent.TransactionContainer.Transactions.Value.Spans[0].Action); //we don't set Action for HTTP calls
		}

		/// <summary>
		/// Makes sure we generate the correct span name
		/// </summary>
		[Fact]
		public async Task SpanName()
		{
			var (listener, _, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
			}

			Assert.Equal("GET localhost", Agent.TransactionContainer.Transactions.Value.Spans[0].Name);
		}

		/// <summary>
		/// Makes sure that the duration of an HTTP Request is captured by the agent
		/// </summary>
		/// <returns>The request duration.</returns>
		[Fact]
		public async Task HttpRequestDuration()
		{
			var (listener, _, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer(ctx =>
			{
				ctx.Response.StatusCode = 200;
				Thread.Sleep(5); //Make sure duration is really > 0
			}))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Url);
			}

			Assert.True(Agent.TransactionContainer.Transactions.Value.Spans[0].Duration > 0);
		}

		/// <summary>
		/// Makes sure spans have an Id
		/// </summary>
		[Fact]
		public async Task HttpRequestSpanGuid()
		{
			var (listener, _, _) = RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.Context.Http.Url);
			}

			Assert.True(Agent.TransactionContainer.Transactions.Value.Spans[0].Id > 0);
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

				Assert.NotEmpty(mockPayloadSender.Payloads[0].Transactions);
				Assert.Empty(mockPayloadSender.SpansOnFirstTransaction);

			}
		}

		/// <summary>
		/// Make sure HttpDiagnosticSubscriber does not report spans after its disposed
		/// </summary>
		[Fact]
		public async Task SubscriptionOnlyRegistersSpansDuringItsLifeTime()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			StartTransaction(agent);

			var spans = Agent.TransactionContainer.Transactions.Value.Spans;

			using (var localServer = new LocalServer())
			using (var httpClient = new HttpClient())
			{
				Assert.True(spans.Length == 0, $"Expected 0 spans has count: {spans.Length}");
				using (agent.Subscribe(new HttpDiagnosticsSubscriber()))
				{
					var res = await httpClient.GetAsync(localServer.Uri);
					Assert.True(res.IsSuccessStatusCode);
					res = await httpClient.GetAsync(localServer.Uri);
					Assert.True(res.IsSuccessStatusCode);
				}
				spans = Agent.TransactionContainer.Transactions.Value.Spans;
				Assert.True(spans.Length == 2, $"Expected 2 but spans has count: {spans.Length}");
				foreach (var _ in Enumerable.Range(0, 10))
					await httpClient.GetAsync(localServer.Uri);

				Assert.True(localServer.SeenRequests > 10, "Make sure we actually performed more than 1 request to our local server");
			}
			Assert.True(spans.Length == 2, $"Expected 1 span because the listener is disposed but spans has count: {spans.Length}");
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

				Assert.NotEmpty(mockPayloadSender.Payloads[0].Transactions);
				Assert.NotEmpty(mockPayloadSender.SpansOnFirstTransaction);

				Assert.NotNull(mockPayloadSender.SpansOnFirstTransaction[0].Context.Http);
				Assert.Equal(url, mockPayloadSender.SpansOnFirstTransaction[0].Context.Http.Url);
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

				mockPayloadSender.Payloads.Clear();

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

				Assert.NotNull(mockPayloadSender.Payloads[0].Transactions[0]);
				Assert.Empty(mockPayloadSender.SpansOnFirstTransaction);
			}
		}

		internal static (IDisposable, MockPayloadSender, ApmAgent) RegisterListenerAndStartTransaction()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var sub = agent.Subscribe(new HttpDiagnosticsSubscriber());
			StartTransaction(agent);

			return (sub, payloadSender, agent);
		}

		private static void StartTransaction(ApmAgent agent)
			//	=> agent.TransactionContainer.Transactions.Value =
			//		new Transaction(agent, $"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);
			=> agent.Tracer.StartTransaction($"{nameof(TestSimpleOutgoingHttpRequest)}", ApiConstants.TypeRequest);
	}
}
