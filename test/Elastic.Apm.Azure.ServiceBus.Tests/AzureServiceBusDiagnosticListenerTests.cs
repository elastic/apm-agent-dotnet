using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	// Resource name rules
	// https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
	public class AzureServiceBusDiagnosticListenerTests : IClassFixture<AzureServiceBusTestEnvironment>, IDisposable, IAsyncDisposable
	{
		private readonly AzureServiceBusTestEnvironment _environment;
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _sender;
		private readonly ServiceBusClient _client;
		private readonly ServiceBusAdministrationClient _adminClient;

		public AzureServiceBusDiagnosticListenerTests(AzureServiceBusTestEnvironment environment)
		{
			_environment = environment;

			var logger = new NoopLogger();
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new AzureServiceBusDiagnosticsSubscriber());

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

			if (!_sender.WaitForSpans(TimeSpan.FromMinutes(2)))
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

			if (!_sender.WaitForSpans(TimeSpan.FromMinutes(2)))
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

		public void Dispose() => _agent.Dispose();

		public ValueTask DisposeAsync() => _client.DisposeAsync();
	}
}
