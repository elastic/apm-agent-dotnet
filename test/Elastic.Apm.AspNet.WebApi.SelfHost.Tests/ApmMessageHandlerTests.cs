using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using SampleHttpSelfHostApp;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNet.WebApi.SelfHost.Tests
{
	public class ApmMessageHandlerTests : IDisposable
	{
		private readonly MockPayloadSender _payloadSender;
		private readonly ApmAgent _apmAgent;

		private readonly ApmMessageHandler _handler;
		private readonly HttpMessageInvoker _messageInvoker;

		public ApmMessageHandlerTests(ITestOutputHelper testOutputHelper)
		{
			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new AgentComponents(
				new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(testOutputHelper)),
				payloadSender: _payloadSender));

			var httpConfiguration = new HttpConfiguration();

			ApiService.MapHttpRoutes(httpConfiguration);

			_handler = new ApmMessageHandler(_apmAgent)
			{
				InnerHandler = new HttpServer(httpConfiguration)
			};

			_messageInvoker = new HttpMessageInvoker(_handler);
		}

		[Fact]
		public async Task SendAsync_ShouldCaptureTransaction()
		{
			// Arrange
			var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com/api/values");

			// Act
			var result = await _messageInvoker.SendAsync(httpRequestMessage, CancellationToken.None);

			// Assert
			_payloadSender.Transactions.Should().ContainSingle();
			_payloadSender.FirstTransaction.Name.Should().Be("GET /api/values");
		}

		public void Dispose()
		{
			_payloadSender?.Clear();
			_apmAgent?.Dispose();
		}
	}
}
