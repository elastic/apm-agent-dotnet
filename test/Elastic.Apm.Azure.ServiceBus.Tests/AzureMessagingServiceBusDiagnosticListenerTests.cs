using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elastic.Apm.Azure.ServiceBus.Tests.Azure;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	[Collection("AzureServiceBus")]
	public class AzureMessagingServiceBusDiagnosticListenerTests : IDisposable, IAsyncDisposable
	{
		private readonly AzureServiceBusTestEnvironment _environment;
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _sender;
		private readonly ServiceBusClient _client;
		private readonly ServiceBusAdministrationClient _adminClient;

		public AzureMessagingServiceBusDiagnosticListenerTests(AzureServiceBusTestEnvironment environment, ITestOutputHelper output)
		{
			_environment = environment;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new AzureMessagingServiceBusDiagnosticsSubscriber());

			_adminClient = new ServiceBusAdministrationClient(environment.ServiceBusConnectionString);
			_client = new ServiceBusClient(environment.ServiceBusConnectionString);
		}

		[Fact]
		public async Task Capture_Span_When_Send_To_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = _client.CreateSender(scope.QueueName);
			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendMessageAsync(new ServiceBusMessage("test message")).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"AzureServiceBus SEND to {scope.QueueName}");
			span.Type.Should().Be("messaging");
			span.Subtype.Should().Be("azureservicebus");
			span.Action.Should().Be("send");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace);
			destination.Service.Name.Should().Be("azureservicebus");
			destination.Service.Resource.Should().Be($"azureservicebus/{scope.QueueName}");
			destination.Service.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Capture_Span_When_Send_To_Topic()
		{
			await using var scope = await TopicScope.CreateWithTopic(_adminClient);
			var sender = _client.CreateSender(scope.TopicName);
			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendMessageAsync(new ServiceBusMessage("test message")).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"AzureServiceBus SEND to {scope.TopicName}");
			span.Type.Should().Be("messaging");
			span.Subtype.Should().Be("azureservicebus");
			span.Action.Should().Be("send");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace);
			destination.Service.Name.Should().Be("azureservicebus");
			destination.Service.Resource.Should().Be($"azureservicebus/{scope.TopicName}");
			destination.Service.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Capture_Span_When_Schedule_To_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = _client.CreateSender(scope.QueueName);
			await _agent.Tracer.CaptureTransaction("Schedule AzureServiceBus Message", "message", async () =>
			{
				await sender.ScheduleMessageAsync(
					new ServiceBusMessage("test message"),
					DateTimeOffset.Now.AddSeconds(10)).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"AzureServiceBus SCHEDULE to {scope.QueueName}");
			span.Type.Should().Be("messaging");
			span.Subtype.Should().Be("azureservicebus");
			span.Action.Should().Be("schedule");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace);
			destination.Service.Name.Should().Be("azureservicebus");
			destination.Service.Resource.Should().Be($"azureservicebus/{scope.QueueName}");
			destination.Service.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Capture_Span_When_Schedule_To_Topic()
		{
			await using var scope = await TopicScope.CreateWithTopic(_adminClient);
			var sender = _client.CreateSender(scope.TopicName);
			await _agent.Tracer.CaptureTransaction("Schedule AzureServiceBus Message", "message", async () =>
			{
				await sender.ScheduleMessageAsync(
					new ServiceBusMessage("test message"),
					DateTimeOffset.Now.AddSeconds(10)).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"AzureServiceBus SCHEDULE to {scope.TopicName}");
			span.Type.Should().Be("messaging");
			span.Subtype.Should().Be("azureservicebus");
			span.Action.Should().Be("schedule");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace);
			destination.Service.Name.Should().Be("azureservicebus");
			destination.Service.Resource.Should().Be($"azureservicebus/{scope.TopicName}");
			destination.Service.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Capture_Transaction_When_Receive_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = _client.CreateSender(scope.QueueName);
			var receiver = _client.CreateReceiver(scope.QueueName);

			await sender.SendMessageAsync(
				new ServiceBusMessage("test message")).ConfigureAwait(false);

			await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2)))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"AzureServiceBus RECEIVE from {scope.QueueName}");
			transaction.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Capture_Transaction_When_Receive_From_Topic_Subscription()
		{
			await using var scope = await TopicScope.CreateWithTopicAndSubscription(_adminClient);

			var sender = _client.CreateSender(scope.TopicName);
			var receiver = _client.CreateReceiver(scope.TopicName, scope.SubscriptionName);

			await sender.SendMessageAsync(
				new ServiceBusMessage("test message")).ConfigureAwait(false);

			await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2)))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"AzureServiceBus RECEIVE from {scope.TopicName}/Subscriptions/{scope.SubscriptionName}");
			transaction.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Capture_Transaction_When_ReceiveDeferred_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = _client.CreateSender(scope.QueueName);
			var receiver = _client.CreateReceiver(scope.QueueName);

			await sender.SendMessageAsync(
				new ServiceBusMessage("test message")).ConfigureAwait(false);


			var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			await receiver.DeferMessageAsync(message).ConfigureAwait(false);


			await receiver.ReceiveDeferredMessageAsync(message.SequenceNumber).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: 2))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(2);

			var transaction = _sender.FirstTransaction;
			transaction.Name.Should().Be($"AzureServiceBus RECEIVE from {scope.QueueName}");
			transaction.Type.Should().Be("messaging");

			var secondTransaction = _sender.Transactions[1];
			secondTransaction.Name.Should().Be($"AzureServiceBus RECEIVEDEFERRED from {scope.QueueName}");
			secondTransaction.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Capture_Transaction_When_ReceiveDeferred_From_Topic_Subscription()
		{
			await using var scope = await TopicScope.CreateWithTopicAndSubscription(_adminClient);

			var sender = _client.CreateSender(scope.TopicName);
			var receiver = _client.CreateReceiver(scope.TopicName, scope.SubscriptionName);

			await sender.SendMessageAsync(
				new ServiceBusMessage("test message")).ConfigureAwait(false);

			var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			await receiver.DeferMessageAsync(message).ConfigureAwait(false);

			await receiver.ReceiveDeferredMessageAsync(message.SequenceNumber).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: 2))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(2);

			var transaction = _sender.FirstTransaction;
			transaction.Name.Should().Be($"AzureServiceBus RECEIVE from {scope.TopicName}/Subscriptions/{scope.SubscriptionName}");
			transaction.Type.Should().Be("messaging");

			var secondTransaction = _sender.Transactions[1];
			secondTransaction.Name.Should().Be($"AzureServiceBus RECEIVEDEFERRED from {scope.TopicName}/Subscriptions/{scope.SubscriptionName}");
			secondTransaction.Type.Should().Be("messaging");
		}

		[Fact]
		public async Task Does_Not_Capture_Span_When_QueueName_Matches_IgnoreMessageQueues()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = _client.CreateSender(scope.QueueName);
			_agent.ConfigStore.CurrentSnapshot = new MockConfigSnapshot(ignoreMessageQueues: scope.QueueName);

			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendMessageAsync(new ServiceBusMessage("test message")).ConfigureAwait(false);
			});

			_sender.SignalEndSpans();
			_sender.WaitForSpans();
			_sender.Spans.Should().HaveCount(0);
		}

		public void Dispose() => _agent.Dispose();

		public ValueTask DisposeAsync() => _client.DisposeAsync();
	}
}
