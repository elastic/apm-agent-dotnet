// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on Producer type in https://github.com/DataDog/dd-trace-dotnet/blob/master/tracer/test/test-applications/integrations/Samples.Kafka/Producer.cs
// Licensed under Apache 2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Elastic.Apm;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KafkaSample
{
    internal static class Producer
    {
        // Flush every x messages
        private const int FlushInterval = 3;
        private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(5);
        private static int MessageNumber = 0;

		public static async Task ProduceAsync(string topic, int numMessages, ClientConfig config, bool isTombstone) =>
			await Agent.Tracer.CaptureTransaction($"Produce Messages Async {topic}", ApiConstants.TypeMessaging, async () =>
			{
				using var producer = new ProducerBuilder<string, string>(config).Build();
				for (var i = 0; i < numMessages; ++i)
				{
					var messageNumber = Interlocked.Increment(ref MessageNumber);
					var key = $"{messageNumber}-Async{(isTombstone ? "-tombstone" : "")}";
					var value = isTombstone ? null : GetMessage(i, isProducedAsync: true);
					var message = new Message<string, string> { Key = key, Value = value };

					Console.WriteLine($"Producing record {i}: {key}...");

					try
					{
						var deliveryResult = await producer.ProduceAsync(topic, message);
						Console.WriteLine($"Produced message to: {deliveryResult.TopicPartitionOffset}");

					}
					catch (ProduceException<string, string> ex)
					{
						Console.WriteLine($"Failed to deliver message: {ex.Error.Reason}");
					}
				}

				Flush(producer);
				Console.WriteLine($"Finished producing {numMessages} messages to topic {topic}");
			});

		private static void Flush(IProducer<string, string> producer)
        {
            var queueLength = 1;
            while (queueLength > 0)
				queueLength = producer.Flush(FlushTimeout);
		}

        public static void Produce(string topic, int numMessages, ClientConfig config, bool handleDelivery, bool isTombstone) =>
			Produce(topic, numMessages, config, handleDelivery ? HandleDelivery : null, isTombstone);

		private static void Produce(string topic, int numMessages, ClientConfig config, Action<DeliveryReport<string, string>> deliveryHandler, bool isTombstone) =>
			Agent.Tracer.CaptureTransaction($"Produce Messages Sync {topic}", ApiConstants.TypeMessaging, () =>
			{
				using (var producer = new ProducerBuilder<string, string>(config).Build())
				{
					for (var i = 0; i < numMessages; ++i)
					{
						var messageNumber = Interlocked.Increment(ref MessageNumber);
						var hasHandler = deliveryHandler is not null;
						var key = $"{messageNumber}-Sync-{hasHandler}{(isTombstone ? "-tombstone" : "")}";
						var value = isTombstone ? null : GetMessage(i, isProducedAsync: false);
						var message = new Message<string, string> { Key = key, Value = value };

						Console.WriteLine($"Producing record {i}: {message.Key}...");

						producer.Produce(topic, message, deliveryHandler);

						if (numMessages % FlushInterval == 0)
							producer.Flush(FlushTimeout);
					}
					Flush(producer);

					Console.WriteLine($"Finished producing {numMessages} messages to topic {topic}");
				}
			});

		private static void HandleDelivery(DeliveryReport<string, string> deliveryReport) =>
			Console.WriteLine(deliveryReport.Error.Code != ErrorCode.NoError
				? $"Failed to deliver message: {deliveryReport.Error.Reason}"
				: $"Produced message to: {deliveryReport.TopicPartitionOffset}");

		private static string GetMessage(int iteration, bool isProducedAsync)
        {
            var message = new SampleMessage("fruit", iteration, isProducedAsync);
            return JObject.FromObject(message).ToString(Formatting.None);
        }
    }
}
