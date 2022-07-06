using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Elastic.Apm.Azure.ServiceBus.Sample
{
	internal class Program
	{
		private static ServiceBusClient _client;
		private static string _queueName;

		private static async Task<int> Main(string[] args)
		{
			Environment.SetEnvironmentVariable("ELASTIC_APM_LOG_LEVEL", "Error");

			if (args.Length == 0)
			{
				Console.Error.WriteLine("An Azure Service Bus connection string must be passed as the first argument");
				return 1;
			}

			Agent.Subscribe(new AzureMessagingServiceBusDiagnosticsSubscriber());

			var connectionString = args[0];
			_client = new ServiceBusClient(connectionString);

			_queueName = "foo"; //Guid.NewGuid().ToString("D");

			await ReadMessages();

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();

			return 0;
		}


		private static async Task ReadMessages()
		{
			var receiver = _client.CreateReceiver(_queueName,
				new ServiceBusReceiverOptions() { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

			Console.WriteLine("Receiving messages from queue");

			var messages = await receiver.ReceiveMessagesAsync(15)
				.ConfigureAwait(false);

			Console.WriteLine("Printing messages:");
			foreach (var item in messages)
			{
				Console.Write($"Body: {item.Body} - ");
				Console.WriteLine($"CorrelationId: {item.CorrelationId}");
			}
			Console.WriteLine($"Message count: {messages.Count}");
			Console.WriteLine("Receiving message from queue");
		}

		private static async Task SendMessages()
		{
			var sender = _client.CreateSender(_queueName);

			await Agent.Tracer.CaptureTransaction("Send AzureServiceBus Messages", "messaging", async () =>
			{
				for (var i = 0; i < 10; i++) await sender.SendMessageAsync(new ServiceBusMessage($"test message {i}")).ConfigureAwait(false);
			});

			Console.WriteLine("Messages sent");
		}

		private static async Task SendBatchMessages()
		{
			var sender = _client.CreateSender(_queueName);

			Console.WriteLine($"Sending multiple messages to queue with {nameof(sender.SendMessagesAsync)}");

			await Agent.Tracer.CaptureTransaction("Send AzureServiceBus Messages in Batch", "messaging", async () =>
			{
				await sender.SendMessagesAsync(new List<ServiceBusMessage> { new("test message 1"), new("test message 2"), new("test message 3") })
					.ConfigureAwait((false));
			});

			Console.WriteLine("Batch Messages sent");
		}
	}
}
