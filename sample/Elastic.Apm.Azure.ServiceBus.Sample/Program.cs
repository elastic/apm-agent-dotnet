using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Elastic.Apm.Azure.ServiceBus.Sample
{
	internal class Program
	{
		private static async Task<int> Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Error.WriteLine("An Azure Service Bus connection string must be passed as the first argument");
				return 1;
			}

			Agent.Subscribe(new AzureMessagingServiceBusDiagnosticsSubscriber());

			var connectionString = args[0];
			var adminClient = new ServiceBusAdministrationClient(connectionString);
			var client = new ServiceBusClient(connectionString);

			var queueName = Guid.NewGuid().ToString("D");

			Console.WriteLine($"Creating queue {queueName}");

			var response = await adminClient.CreateQueueAsync(queueName).ConfigureAwait(false);

			var sender = client.CreateSender(queueName);

			Console.WriteLine("Sending messages to queue");

			await Agent.Tracer.CaptureTransaction("Send AzureServiceBus Messages", "messaging", async () =>
			{
				for (var i = 0; i < 10; i++)
					await sender.SendMessageAsync(new ServiceBusMessage($"test message {i}")).ConfigureAwait(false);
			});

			var receiver = client.CreateReceiver(queueName);

			Console.WriteLine("Receiving messages from queue");

			var messages = await receiver.ReceiveMessagesAsync(9)
				.ConfigureAwait(false);

			Console.WriteLine("Receiving message from queue");

			var message = await receiver.ReceiveMessageAsync()
				.ConfigureAwait(false);

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();

			return 0;
		}
	}
}
