using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using Elastic.Apm.Api;
using Elastic.Apm.Azure.ServiceBus.Tests.Azure;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Xunit;
using Xunit.Abstractions;
using Message = Microsoft.Azure.ServiceBus.Message;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	[Collection("AzureServiceBus")]
	public class MicrosoftAzureServiceBusDiagnosticListenerTests : IDisposable
	{
		private readonly AzureServiceBusTestEnvironment _environment;
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _sender;
		private readonly ServiceBusAdministrationClient _adminClient;

		public MicrosoftAzureServiceBusDiagnosticListenerTests(AzureServiceBusTestEnvironment environment, ITestOutputHelper output)
		{
			_environment = environment;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new MicrosoftAzureServiceBusDiagnosticsSubscriber());
			_adminClient = new ServiceBusAdministrationClient(environment.ServiceBusConnectionString);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Send_To_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);

			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendAsync(new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SEND to {scope.QueueName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("send");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.QueueName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);

			span.Context.Message.Should().NotBeNull();
			span.Context.Message.Queue.Should().NotBeNull();
			span.Context.Message.Queue.Name.Should().Be(scope.QueueName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Send_To_Topic()
		{
			await using var scope = await TopicScope.CreateWithTopic(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendAsync(new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SEND to {scope.TopicName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("send");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.TopicName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);

			span.Context.Message.Should().NotBeNull();
			span.Context.Message.Queue.Should().NotBeNull();
			span.Context.Message.Queue.Name.Should().Be(scope.TopicName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Schedule_To_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			await _agent.Tracer.CaptureTransaction("Schedule AzureServiceBus Message", "message", async () =>
			{
				await sender.ScheduleMessageAsync(
					new Message(Encoding.UTF8.GetBytes("test message")),
					DateTimeOffset.Now.AddSeconds(10)).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SCHEDULE to {scope.QueueName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("schedule");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.QueueName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);

			span.Context.Message.Should().NotBeNull();
			span.Context.Message.Queue.Should().NotBeNull();
			span.Context.Message.Queue.Name.Should().Be(scope.QueueName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Schedule_To_Topic()
		{
			await using var scope = await TopicScope.CreateWithTopic(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			await _agent.Tracer.CaptureTransaction("Schedule AzureServiceBus Message", "message", async () =>
			{
				await sender.ScheduleMessageAsync(
					new Message(Encoding.UTF8.GetBytes("test message")),
					DateTimeOffset.Now.AddSeconds(10)).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SCHEDULE to {scope.TopicName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("schedule");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.TopicName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);

			span.Context.Message.Should().NotBeNull();
			span.Context.Message.Queue.Should().NotBeNull();
			span.Context.Message.Queue.Name.Should().Be(scope.TopicName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Receive_From_Queue_Inside_Transaction()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString, scope.QueueName, ReceiveMode.PeekLock);

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			await _agent.Tracer.CaptureTransaction("Receive messages", ApiConstants.TypeMessaging, async t =>
			{
				await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans(TimeSpan.FromMinutes(2)))
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.SpansOnFirstTransaction.First();

			span.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {scope.QueueName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);

			span.Context.Message.Should().NotBeNull();
			span.Context.Message.Queue.Should().NotBeNull();
			span.Context.Message.Queue.Name.Should().Be(scope.QueueName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_Receive_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString, scope.QueueName, ReceiveMode.PeekLock);

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2)))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {scope.QueueName}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);

			transaction.Context.Message.Should().NotBeNull();
			transaction.Context.Message.Queue.Should().NotBeNull();
			transaction.Context.Message.Queue.Name.Should().Be(scope.QueueName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_Receive_From_Topic_Subscription()
		{
			await using var scope = await TopicScope.CreateWithTopicAndSubscription(_adminClient);

			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString,
				EntityNameHelper.FormatSubscriptionPath(scope.TopicName, scope.SubscriptionName));

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2)))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			var subscription = $"{scope.TopicName}/Subscriptions/{scope.SubscriptionName}";
			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {subscription}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);

			transaction.Context.Message.Should().NotBeNull();
			transaction.Context.Message.Queue.Should().NotBeNull();
			transaction.Context.Message.Queue.Name.Should().Be(subscription);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_ReceiveDeferred_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString, scope.QueueName, ReceiveMode.PeekLock);

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			var message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			await receiver.DeferAsync(message.SystemProperties.LockToken).ConfigureAwait(false);

			await receiver.ReceiveDeferredMessageAsync(message.SystemProperties.SequenceNumber).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: 2))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(2);

			var transaction = _sender.FirstTransaction;
			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {scope.QueueName}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);

			transaction.Context.Message.Should().NotBeNull();
			transaction.Context.Message.Queue.Should().NotBeNull();
			transaction.Context.Message.Queue.Name.Should().Be(scope.QueueName);

			var secondTransaction = _sender.Transactions[1];
			secondTransaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVEDEFERRED from {scope.QueueName}");
			secondTransaction.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_ReceiveDeferred_From_Topic_Subscription()
		{
			await using var scope = await TopicScope.CreateWithTopicAndSubscription(_adminClient);

			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString,
				EntityNameHelper.FormatSubscriptionPath(scope.TopicName, scope.SubscriptionName));

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			var message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			await receiver.DeferAsync(message.SystemProperties.LockToken).ConfigureAwait(false);

			await receiver.ReceiveDeferredMessageAsync(message.SystemProperties.SequenceNumber).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: 2))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(2);

			var transaction = _sender.FirstTransaction;
			var subscription = $"{scope.TopicName}/Subscriptions/{scope.SubscriptionName}";
			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {subscription}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);

			transaction.Context.Message.Should().NotBeNull();
			transaction.Context.Message.Queue.Should().NotBeNull();
			transaction.Context.Message.Queue.Name.Should().Be(subscription);

			var secondTransaction = _sender.Transactions[1];
			secondTransaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVEDEFERRED from {subscription}");
			secondTransaction.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Does_Not_Capture_Span_When_QueueName_Matches_IgnoreMessageQueues()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			_agent.ConfigurationStore.CurrentSnapshot = new MockConfiguration(ignoreMessageQueues: scope.QueueName);

			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendAsync(new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);
			});

			_sender.SignalEndSpans();
			_sender.WaitForSpans();
			_sender.Spans.Should().HaveCount(0);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_Process_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString, scope.QueueName, ReceiveMode.PeekLock);

			receiver.RegisterMessageHandler((message, token) =>
			{
				_agent.Tracer.CurrentTransaction.CaptureSpan("ProcessMessage", "process", s =>
				{
					s.SetLabel("message", Encoding.UTF8.GetString(message.Body));
				});
				return Task.CompletedTask;
			}, args =>
			{
				_agent.Tracer.CurrentTransaction.CaptureException(args.Exception);
				return Task.CompletedTask;
			});

			var messageCount = 3;
			var messages = Enumerable.Range(1, messageCount)
				.Select(i => new Message(Encoding.UTF8.GetBytes($"test message {i}")))
				.ToList();

			await sender.SendAsync(messages).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: messageCount * 2))
				throw new Exception("No transactions received in timeout");

			var transactions = _sender.Transactions;
			transactions.Should().HaveCount(messageCount * 2);
			transactions
				.Count(t => t.Name == $"{ServiceBus.SegmentName} RECEIVE from {scope.QueueName}")
				.Should().Be(messageCount);

			var processTransactions = transactions
				.Where(t => t.Name == $"{ServiceBus.SegmentName} PROCESS from {scope.QueueName}")
				.ToList();
			processTransactions.Should().HaveCount(messageCount);

			foreach (var transaction in processTransactions)
			{
				transaction.Context.Message.Should().NotBeNull();
				transaction.Context.Message.Queue.Should().NotBeNull();
				transaction.Context.Message.Queue.Name.Should().Be(scope.QueueName);

				var spans = _sender.Spans.Where(s => s.TransactionId == transaction.Id).ToList();
				spans.Should().HaveCount(1);
			}

			await receiver.CloseAsync();
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_ProcessSession_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient, new CreateQueueOptions(Guid.NewGuid().ToString("D"))
			{
				RequiresSession = true
			});
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			var client = new QueueClient(_environment.ServiceBusConnectionString, scope.QueueName, ReceiveMode.PeekLock);

			client.RegisterSessionHandler((session, message, token) =>
			{
				_agent.Tracer.CurrentTransaction.CaptureSpan("ProcessSessionMessage", "process", s =>
				{
					s.SetLabel("message", Encoding.UTF8.GetString(message.Body));
				});
				return Task.CompletedTask;
			}, args =>
			{
				_agent.Tracer.CurrentTransaction.CaptureException(args.Exception);
				return Task.CompletedTask;
			});

			var messageCount = 3;
			var messages = Enumerable.Range(1, messageCount)
				.Select(i => new Message(Encoding.UTF8.GetBytes($"test message {i}")) { SessionId = "test" })
				.ToList();

			await sender.SendAsync(messages).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: messageCount * 2))
				throw new Exception("No transactions received in timeout");

			var transactions = _sender.Transactions;
			transactions.Should().HaveCount(messageCount * 2);
			transactions
				.Count(t => t.Name == $"{ServiceBus.SegmentName} RECEIVE from {scope.QueueName}")
				.Should().Be(messageCount);

			var processTransactions = transactions
				.Where(t => t.Name == $"{ServiceBus.SegmentName} PROCESS from {scope.QueueName}")
				.ToList();
			processTransactions.Should().HaveCount(messageCount);

			foreach (var transaction in processTransactions)
			{
				transaction.Context.Message.Should().NotBeNull();
				transaction.Context.Message.Queue.Should().NotBeNull();
				transaction.Context.Message.Queue.Name.Should().Be(scope.QueueName);

				var spans = _sender.Spans.Where(s => s.TransactionId == transaction.Id).ToList();
				spans.Should().HaveCount(1);
			}

			await client.CloseAsync();
		}

		public void Dispose() => _agent.Dispose();
	}
}
