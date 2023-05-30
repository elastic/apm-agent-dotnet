// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on https://github.com/DataDog/dd-trace-dotnet/blob/a72b74fc8e67d8d8a6430628fe8643b3a693d2bc/tracer/test/test-applications/integrations/Samples.RabbitMQ/Program.cs
// Licensed under Apache 2.0

using System;
using System.Text;
using System.Threading;
using Elastic.Apm;
using Elastic.Apm.Api;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMqSample
{
	internal class Program
    {
		private static volatile int MessageCount = 0;
		private static readonly AutoResetEvent SendFinished = new(false);

		private const string IgnoreExchangeName = "test-ignore-exchange-name";
		private const string IgnoreQueueName = "test-ignore-queue-name";
		private const string IgnoreRoutingKey = "test-ignore-routing-key";

		private const string ExchangeName = "test-exchange-name";
		private const string RoutingKey = "test-routing-key";
		private const string QueueName = "test-queue-name";

		private static string Host() =>
			Environment.GetEnvironmentVariable("RABBITMQ_HOST") ??
			throw new ArgumentException("RABBITMQ_HOST environment variable must be specified");

		public static void Main(string[] args)
		{
			var ignoreMessageQueues = Environment.GetEnvironmentVariable("ELASTIC_APM_IGNORE_MESSAGE_QUEUES");

			// only run the ignore publish and get if the agent has been configured with the ignore exchange and queue.
			if (!string.IsNullOrEmpty(ignoreMessageQueues) &&
				ignoreMessageQueues.Contains(IgnoreExchangeName) &&
				ignoreMessageQueues.Contains(IgnoreQueueName))
				PublishAndGet("PublishAndGetIgnore", IgnoreExchangeName, IgnoreQueueName, IgnoreRoutingKey);

			PublishAndGet("PublishAndGet", ExchangeName, QueueName, RoutingKey);
			PublishAndGetDefault();

			var sendThread = new Thread(Send);
			sendThread.Start();

			var receiveThread = new Thread(Receive);
			receiveThread.Start();

			sendThread.Join();
			receiveThread.Join();

			// Allow time for the agent to send data
			Thread.Sleep(TimeSpan.FromSeconds(30));
			Console.WriteLine("finished");
        }

        private static void PublishAndGet(string name, string exchange, string queue, string routingKey)
        {
            // Configure and send to RabbitMQ queue
            var factory = new ConnectionFactory { Uri = new Uri(Host()) };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
			{
				Agent.Tracer.CaptureTransaction(name, "messaging", () =>
				{
					channel.ExchangeDeclare(exchange, "direct");
					channel.QueueDeclare(queue: queue,
						durable: false,
						exclusive: false,
						autoDelete: false,
						arguments: null);
					channel.QueueBind(queue, exchange, routingKey);
					channel.QueuePurge(queue); // Ensure there are no more messages in this queue

					// Test an empty BasicGetResult
					channel.BasicGet(queue, true);

					// Send message to the exchange
					var message = $"{name} - Message";
					var body = Encoding.UTF8.GetBytes(message);

					channel.BasicPublish(exchange: exchange,
						routingKey: routingKey,
						basicProperties: null,
						body: body);
					Console.WriteLine($"[{name}] BasicPublish - Sent message: {message}");

					var result = channel.BasicGet(queue, true);
#if RABBITMQ_6_0
					var resultMessage = Encoding.UTF8.GetString(result.Body.ToArray());
#else
					var resultMessage = Encoding.UTF8.GetString(result.Body);
#endif
					Console.WriteLine($"[{name}] BasicGet - Received message: {resultMessage}");
				});
			}
        }

        private static void PublishAndGetDefault()
        {
            // Configure and send to RabbitMQ queue
            var factory = new ConnectionFactory() { Uri = new Uri(Host()) };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                string defaultQueueName;

				Agent.Tracer.CaptureTransaction("PublishAndGetDefault", "messaging", () =>
				{
					defaultQueueName = channel.QueueDeclare().QueueName;
					channel.QueuePurge(QueueName); // Ensure there are no more messages in this queue

					// Test an empty BasicGetResult
					channel.BasicGet(defaultQueueName, true);

					// Send message to the default exchange and use new queue as the routingKey
					var message = "PublishAndGetDefault - Message";
					var body = Encoding.UTF8.GetBytes(message);
					channel.BasicPublish(exchange: "",
						routingKey: defaultQueueName,
						basicProperties: null,
						body: body);
					Console.WriteLine($"[PublishAndGetDefault] BasicPublish - Sent message: {message}");

					var result = channel.BasicGet(defaultQueueName, true);
#if RABBITMQ_6_0
					var resultMessage = Encoding.UTF8.GetString(result.Body.ToArray());
#else
					var resultMessage = Encoding.UTF8.GetString(result.Body);
#endif

					Console.WriteLine($"[PublishAndGetDefault] BasicGet - Received message: {resultMessage}");
				});
			}
        }

        private static void Send()
        {
            // Configure and send to RabbitMQ queue
            var factory = new ConnectionFactory() { Uri = new Uri(Host()) };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                        durable: false,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);
                channel.QueuePurge("hello"); // Ensure there are no more messages in this queue

                for (var i = 0; i < 3; i++)
				{
					Agent.Tracer.CaptureTransaction("PublishToConsumer", "messaging", () =>
					{
						var message = $"Send - Message #{i}";
						var body = Encoding.UTF8.GetBytes(message);

						channel.BasicPublish(exchange: "",
							routingKey: "hello",
							basicProperties: null,
							body: body);
						Console.WriteLine("[Send] - [x] Sent \"{0}\"", message);
						Interlocked.Increment(ref MessageCount);
					});
				}
            }

            SendFinished.Set();
            Console.WriteLine("[Send] Exiting Thread.");
        }

        private static void Receive()
        {
            // Let's just wait for all sending activity to finish before doing any work
            SendFinished.WaitOne();

            // Configure and listen to RabbitMQ queue
            var factory = new ConnectionFactory { Uri = new Uri(Host()) };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
					var transaction = Agent.Tracer.CurrentTransaction;
					var span = transaction?.StartSpan("Consume message", ApiConstants.TypeMessaging);

#if RABBITMQ_6_0
                    var body = ea.Body.ToArray();
#else
                    var body = ea.Body;
#endif

                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine("[Receive] - [x] Received {0}", message);

					Interlocked.Decrement(ref MessageCount);
					span?.End();
                };

                channel.BasicConsume("hello",
                                    true,
                                    consumer);

                while (MessageCount != 0)
					Thread.Sleep(1000);

				Console.WriteLine("[Receive] Exiting Thread.");
            }
        }
    }
}
