﻿using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
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
	public class AzureQueueStorageDiagnosticListenerTests
	{
		private readonly AzureStorageTestEnvironment _environment;
		private readonly MockPayloadSender _sender;
		private readonly ApmAgent _agent;

		public AzureQueueStorageDiagnosticListenerTests(AzureStorageTestEnvironment environment, ITestOutputHelper output)
		{
			_environment = environment;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new AzureQueueStorageDiagnosticsSubscriber());

		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Receives_From_Queue()
		{
			var queueName = Guid.NewGuid().ToString();
			var client = new QueueClient(_environment.StorageAccountConnectionString, queueName);

			var createResponse = await client.CreateAsync();
			var sendResponse = await client.SendMessageAsync(nameof(Capture_Span_When_Receives_From_Queue));

			var receiveResponse = await client.ReceiveMessagesAsync(1);

			if (!_sender.WaitForTransactions())
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"AzureQueue RECEIVE from {queueName}");
			transaction.Type.Should().Be("messaging");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Receive_From_Queue()
		{
			var queueName = Guid.NewGuid().ToString();
			var client = new QueueClient(_environment.StorageAccountConnectionString, queueName);

			var createResponse = await client.CreateAsync();
			var sendResponse = await client.SendMessageAsync(nameof(Capture_Span_When_Receive_From_Queue));

			var receiveResponse = await client.ReceiveMessageAsync();

			if (!_sender.WaitForTransactions())
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"AzureQueue RECEIVE from {queueName}");
			transaction.Type.Should().Be("messaging");
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

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"AzureQueue SEND to {queueName}");
			span.Type.Should().Be("messaging");
			span.Subtype.Should().Be("azurequeue");
			span.Action.Should().Be("send");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.StorageAccountConnectionStringProperties.QueueUrl);
			destination.Service.Name.Should().Be("azurequeue");
			destination.Service.Resource.Should().Be($"azurequeue/{queueName}");
			destination.Service.Type.Should().Be("messaging");
		}
	}
}
