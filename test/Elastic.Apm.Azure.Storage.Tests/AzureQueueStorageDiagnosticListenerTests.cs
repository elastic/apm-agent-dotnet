using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Storage.Tests
{
	[Collection("AzureStorage")]
	public class AzureQueueStorageDiagnosticListenerTests : IDisposable
	{
		private readonly AzureStorageTestEnvironment _environment;
		private readonly MockPayloadSender _sender;
		private readonly ApmAgent _agent;
		private readonly IDisposable _subscription;
		private readonly ITestOutputHelper _output
			;

		public AzureQueueStorageDiagnosticListenerTests(AzureStorageTestEnvironment environment, ITestOutputHelper output)
		{
			_environment = environment;
			_output = output;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_subscription = _agent.Subscribe(new AzureQueueStorageDiagnosticsSubscriber());
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Receives_From_Queue()
		{
			var queueName = Guid.NewGuid().ToString();
			var client = new QueueClient(_environment.StorageAccountConnectionString, queueName);

			var createResponse = await client.CreateAsync();
			var sendResponse = await client.SendMessageAsync(nameof(Capture_Span_When_Receives_From_Queue));
			var receiveResponse = await client.ReceiveMessagesAsync(1);

			AssertTransaction("RECEIVE", queueName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Receive_From_Queue()
		{
			var queueName = Guid.NewGuid().ToString();
			var client = new QueueClient(_environment.StorageAccountConnectionString, queueName);

			var createResponse = await client.CreateAsync();
			var sendResponse = await client.SendMessageAsync(nameof(Capture_Span_When_Receive_From_Queue));

			var receiveResponse = await client.ReceiveMessageAsync();

			AssertTransaction("RECEIVE", queueName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Send_To_Queue()
		{
			var queueName = Guid.NewGuid().ToString();
			var client = new QueueClient(_environment.StorageAccountConnectionString, queueName);
			var createResponse = await client.CreateAsync();

			await _agent.Tracer.CaptureTransaction("Send Azure Queue Message", "message", async () =>
			{
				var sendResponse = await client.SendMessageAsync(nameof(Capture_Span_When_Send_To_Queue));
			});

			AssertSpan("SEND", queueName);
		}

		private void AssertTransaction(string action, string queueName)
		{
			if (!_sender.WaitForTransactions())
			{
				_sender.SignalEndTransactions();
				throw new Exception($"No transaction received within timeout. (already received {_sender.Transactions.Count} transactions)");
			}

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"{AzureQueueStorage.SpanName} {action} from {queueName}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		private void AssertSpan(string action, string queueName)
		{
			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{AzureQueueStorage.SpanName} {action} to {queueName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(AzureQueueStorage.SubType);
			span.Action.Should().Be(action.ToLowerInvariant());
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.StorageAccountConnectionStringProperties.QueueFullyQualifiedNamespace);
			destination.Service.Name.Should().Be(AzureQueueStorage.SubType);
			destination.Service.Resource.Should().Be($"{AzureQueueStorage.SubType}/{queueName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_agent.Dispose();
		}
	}
}
