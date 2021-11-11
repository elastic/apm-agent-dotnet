// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on Producer type in https://github.com/DataDog/dd-trace-dotnet/blob/master/tracer/test/test-applications/integrations/Samples.Kafka
// Licensed under Apache 2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace KafkaSample
{
	internal class Program
	{
		private const string IgnoreTopic = "ignore-topic";

		private static async Task Main(string[] args)
		{
			var kafkaHost = Environment.GetEnvironmentVariable("KAFKA_HOST") ??
				throw new InvalidOperationException(
					"KAFKA_HOST environment variable is empty. Kafka host must be specified with KAFKA_HOST environment variable");

			var config = new ClientConfig { BootstrapServers = kafkaHost };
			var topic = Guid.NewGuid().ToString("N");

			await CreateTopic(config, topic);
			await ConsumeAndProduceMessages(topic, config);

			await ProduceMessagesInvalidTopic(config);

			var ignoreMessageQueues = Environment.GetEnvironmentVariable("ELASTIC_APM_IGNORE_MESSAGE_QUEUES");

			// only run the ignore produce and consume if the agent has been configured with the ignore topic
			if (!string.IsNullOrEmpty(ignoreMessageQueues) && ignoreMessageQueues.Contains(IgnoreTopic))
			{
				await CreateTopic(config, IgnoreTopic);
				await ConsumeAndProduceMessages(IgnoreTopic, config, 1);
			}

			// allow time for agent to send APM data
			await Task.Delay(TimeSpan.FromSeconds(30));

			Console.WriteLine("finished");
		}

		private static async Task ConsumeAndProduceMessages(string topic, ClientConfig config, int numberOfMessagesPerProducer = 10)
        {
            var commitPeriod = 3;
			var cts = new CancellationTokenSource();

			using var consumer1 = Consumer.Create(config, true, topic, "AutoCommitConsumer1");
			using var consumer2 = Consumer.Create(config, false, topic, "ManualCommitConsumer2");

            Console.WriteLine("Starting consumers...");

            var consumeTask1 = Task.Run(() => consumer1.Consume(cts.Token));
            var consumeTask2 = Task.Run(() => consumer2.ConsumeWithExplicitCommit(commitEveryXMessages: commitPeriod, cts.Token));

            Console.WriteLine($"Producing messages");

            var messagesProduced = await ProduceMessages(topic, config, numberOfMessagesPerProducer);

            // Wait for all messages to be consumed
            // This assumes that the topic starts empty, and nothing else is producing to the topic
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (true)
            {
                var syncCount = Volatile.Read(ref Consumer.TotalSyncMessages);
                var asyncCount = Volatile.Read(ref Consumer.TotalAsyncMessages);
                var tombstoneCount = Volatile.Read(ref Consumer.TotalTombstones);

                if (syncCount >= messagesProduced.SyncMessages
                 && asyncCount >= messagesProduced.AsyncMessages
                 && tombstoneCount >= messagesProduced.TombstoneMessages)
                {
                    Console.WriteLine($"All messages produced and consumed");
                    break;
                }

                if (DateTime.UtcNow > deadline)
                {
                    Console.WriteLine($"Exiting consumer: did not consume all messages syncCount {syncCount}, asyncCount {asyncCount}");
                    break;
                }

                await Task.Delay(1000);
            }

            cts.Cancel();
            Console.WriteLine($"Waiting for graceful exit...");

            await Task.WhenAny(
                Task.WhenAll(consumeTask1, consumeTask2),
                Task.Delay(TimeSpan.FromSeconds(5)));
        }


		private static async Task ProduceMessagesInvalidTopic(ClientConfig config)
		{
			// try to produce invalid messages
			const string invalidTopic = "INVALID-TOPIC";
			Producer.Produce(invalidTopic, 1, config, true, false); // failure should be logged by delivery handler

			try
			{
				await Producer.ProduceAsync(invalidTopic, 1, config, isTombstone: false);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error producing a message to an unknown topic (expected): {ex}");
			}
		}

        private static async Task<MessagesProduced> ProduceMessages(string topic, ClientConfig config, int numberOfMessagesPerProducer)
        {
			// Send valid messages
            Producer.Produce(topic, numberOfMessagesPerProducer, config, false, false);
            Producer.Produce(topic, numberOfMessagesPerProducer, config, true, false);
            await Producer.ProduceAsync(topic, numberOfMessagesPerProducer, config, false);

            // Send tombstone messages
            Producer.Produce(topic, numberOfMessagesPerProducer, config, false, true);
            Producer.Produce(topic, numberOfMessagesPerProducer, config, true, true);
            await Producer.ProduceAsync(topic, numberOfMessagesPerProducer, config, true);

            return new MessagesProduced
            {
                SyncMessages = numberOfMessagesPerProducer * 2,
                AsyncMessages = numberOfMessagesPerProducer * 1,
                TombstoneMessages = numberOfMessagesPerProducer * 3,
            };
        }

        private struct MessagesProduced
        {
            public int SyncMessages;
            public int AsyncMessages;
            public int TombstoneMessages;
        }

		private static async Task CreateTopic(ClientConfig config, string topic)
		{
			using var adminClient = new AdminClientBuilder(config).Build();
			try
			{
				Console.WriteLine($"Creating topic {topic}...");

				await adminClient.CreateTopicsAsync(new List<TopicSpecification>
				{
					new()
					{
						Name = topic,
						NumPartitions = 1,
						ReplicationFactor = 1
					}
				});

				Console.WriteLine($"Topic created");
			}
			catch (CreateTopicsException e)
			{
				if (e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
					Console.WriteLine("Topic already exists");
				else
				{
					Console.WriteLine($"An error occured creating topic {topic}: {e.Results[0].Error.Reason}");
					throw;
				}
			}
		}
	}
}
