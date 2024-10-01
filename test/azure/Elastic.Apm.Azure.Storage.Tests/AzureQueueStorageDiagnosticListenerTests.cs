using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Elastic.Apm.Api;
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
		private readonly ITestOutputHelper _output;

#if NET
		static AzureQueueStorageDiagnosticListenerTests()
		{
			var listener = new ActivityListener
			{
				ShouldListenTo = a => a.Name == "Elastic.Apm",
				Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
				ActivityStarted = activity => { },
				ActivityStopped = activity => { }
			};

			ActivitySource.AddActivityListener(listener);
		}
#endif

		public AzureQueueStorageDiagnosticListenerTests(AzureStorageTestEnvironment environment, ITestOutputHelper output)
		{
			var logger = new XUnitLogger(LogLevel.Trace, output);

			_environment = environment;
			_output = output;
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_subscription = _agent.Subscribe(new AzureQueueStorageDiagnosticsSubscriber());
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_Receive_Messages_From_Queue()
		{
			var queueName = Guid.NewGuid().ToString();
			var client = new QueueClient(_environment.StorageAccountConnectionString, queueName);

			var createResponse = await client.CreateAsync();
			var sendResponse = await client.SendMessageAsync(nameof(Capture_Transaction_When_Receive_Messages_From_Queue));
			var receiveResponse = await client.ReceiveMessagesAsync(1);

			AssertTransaction("RECEIVE", queueName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_Receive_Message_From_Queue()
		{
			var queueName = Guid.NewGuid().ToString();
			var client = new QueueClient(_environment.StorageAccountConnectionString, queueName);

			var createResponse = await client.CreateAsync();
			var sendResponse = await client.SendMessageAsync(nameof(Capture_Transaction_When_Receive_Message_From_Queue));
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
				throw new Exception($"No transaction received within timeout. (already received {_sender.Transactions.Count} transactions)");

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

			span.Context.Service.Target.Should().NotBeNull();
			span.Context.Service.Target.Type.Should().Be(AzureQueueStorage.SubType);
			span.Context.Service.Target.Name.Should().Be(queueName);

			span.Context.Destination.Should().NotBeNull();
			span.Context.Destination.Address.Should().Be(_environment.StorageAccountConnectionStringProperties.QueueFullyQualifiedNamespace);
			span.Context.Destination.Service.Resource.Should().Be($"{AzureQueueStorage.SubType}/{queueName}");
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_agent.Dispose();
		}
	}
}
