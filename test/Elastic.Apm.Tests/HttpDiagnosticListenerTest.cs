using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

			Assert.Equal($"Error {listener.Name}: Exception in OnError, Exception-type:{nameof(Exception)}, Message:{exceptionMessage}",
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
			var agent = new ApmAgent(new TestAgentComponents(logger));
			StartTransaction(agent);
			var listener = new HttpDiagnosticListener(agent);

			var request = new HttpRequestMessage(HttpMethod.Get, "https://elastic.co");
			var response = new HttpResponseMessage(HttpStatusCode.OK);

			//Simulate Start
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", new { Request = request }));
			//Simulate Stop
			listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Stop", new { Request = request, Response = response }));
			Assert.Empty(listener.ProcessingRequests);

			Assert.Equal(request.RequestUri.ToString(), TransactionContainer.Transactions.Value.Spans[0].Context.Http.Url);
			Assert.Equal(HttpMethod.Get.ToString(), TransactionContainer.Transactions.Value.Spans[0].Context.Http.Method);
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
				$"{LogLevel.Warning} {listener.Name}: Failed capturing request '{HttpMethod.Get} {request.RequestUri}' in System.Net.Http.HttpRequestOut.Stop. This Span will be skipped in case it wasn't captured before.",
				logger.Lines[0]);
			Assert.NotNull(TransactionContainer.Transactions.Value);
			Assert.Single(TransactionContainer.Transactions.Value.Spans);
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
			var agent = new ApmAgent(new TestAgentComponents());
			RegisterListenerAndStartTransaction(agent);

			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, TransactionContainer.Transactions.Value.Spans[0].Context.Http.Url);
			}

			Assert.Equal(200, TransactionContainer.Transactions.Value.Spans[0].Context.Http.StatusCode);
			Assert.Equal(HttpMethod.Get.ToString(), TransactionContainer.Transactions.Value.Spans[0].Context.Http.Method);
		}

		/// <summary>
		/// Sends a simple real HTTP POST message and the server responds with 500
		/// The test makes sure HttpDiagnosticListener captures the POST method and
		/// the response code correctly
		/// </summary>
		[Fact]
		public async Task TestNotSuccesfulOutgoingHttpPostRequest()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			RegisterListenerAndStartTransaction(agent);

			using (var localServer = new LocalServer(ctx => { ctx.Response.StatusCode = 500; }))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.PostAsync(localServer.Uri, new StringContent("foo"));

				Assert.False(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, TransactionContainer.Transactions.Value.Spans[0].Context.Http.Url);
			}

			Assert.Equal(500, TransactionContainer.Transactions.Value.Spans[0].Context.Http.StatusCode);
			Assert.Equal(HttpMethod.Post.ToString(), TransactionContainer.Transactions.Value.Spans[0].Context.Http.Method);
		}

		/// <summary>
		/// Starts an HTTP call to a non existing URL and makes sure that an error is captured.
		/// This uses an HttpClient instance directly
		/// </summary>
		[Fact]
		public async Task CaptureErrorOnFailingHttpCall_HttpClient()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			RegisterListenerAndStartTransaction(agent);

			var httpClient = new HttpClient();
			try
			{
				var res = await httpClient.GetAsync("http://nonexistenturl_dsfdsf.ghkdehfn");
				Assert.True(false); //Make it fail if no exception is thrown
			}
			catch (Exception e)
			{
				Assert.NotNull(e);
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
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			RegisterListenerAndStartTransaction(agent);

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

		/// <summary>
		/// Makes sure we set the correct type and subtype for external, http spans
		/// </summary>
		[Fact]
		public async Task SpanTypeAndSubtype()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			RegisterListenerAndStartTransaction(agent);

			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
			}

			Assert.Equal(Span.TypeExternal, TransactionContainer.Transactions.Value.Spans[0].Type);
			Assert.Equal(Span.SubtypeHttp, TransactionContainer.Transactions.Value.Spans[0].Subtype);
			Assert.Null(TransactionContainer.Transactions.Value.Spans[0].Action); //we don't set Action for HTTP calls
		}

		/// <summary>
		/// Makes sure we generate the correct span name
		/// </summary>
		[Fact]
		public async Task SpanName()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			RegisterListenerAndStartTransaction(agent);

			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
			}

			Assert.Equal("GET localhost", TransactionContainer.Transactions.Value.Spans[0].Name);
		}

		/// <summary>
		/// Makes sure that the duration of an HTTP Request is captured by the agent
		/// </summary>
		/// <returns>The request duration.</returns>
		[Fact]
		public async Task HttpRequestDuration()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			RegisterListenerAndStartTransaction(agent);

			using (var localServer = new LocalServer(ctx =>
			{
				ctx.Response.StatusCode = 200;
				Thread.Sleep(5); //Make sure duration is really > 0
			}))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, TransactionContainer.Transactions.Value.Spans[0].Context.Http.Url);
			}

			Assert.True(TransactionContainer.Transactions.Value.Spans[0].Duration > 0);
		}

		/// <summary>
		/// Makes sure spans have an Id
		/// </summary>
		[Fact]
		public async Task HttpRequestSpanGuid()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			RegisterListenerAndStartTransaction(agent);

			using (var localServer = new LocalServer())
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				Assert.True(res.IsSuccessStatusCode);
				Assert.Equal(localServer.Uri, TransactionContainer.Transactions.Value.Spans[0].Context.Http.Url);
			}

			Assert.True(TransactionContainer.Transactions.Value.Spans[0].Id > 0);
		}

		private void RegisterListenerAndStartTransaction(ApmAgent agent)
		{
			agent = agent ?? new ApmAgent(new TestAgentComponents());
			new HttpDiagnosticsSubscriber().Subscribe(agent);
			StartTransaction(agent);
		}


		private void StartTransaction(ApmAgent agent)
			=> TransactionContainer.Transactions.Value =
				new Transaction(agent, $"{nameof(TestSimpleOutgoingHttpRequest)}", Transaction.TypeRequest);
	}
}
