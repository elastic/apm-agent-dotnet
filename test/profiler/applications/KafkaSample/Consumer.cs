// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on Producer type in https://github.com/DataDog/dd-trace-dotnet/blob/master/tracer/test/test-applications/integrations/Samples.Kafka/Consumer.cs
// Licensed under Apache 2.0

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Elastic.Apm;
using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace KafkaSample
{
	internal class Consumer: IDisposable
    {
        private readonly string _consumerName;
        private readonly IConsumer<string, string> _consumer;

        public static int TotalAsyncMessages = 0;
        public static int TotalSyncMessages = 0;
        public static int TotalTombstones = 0;

        private Consumer(ConsumerConfig config, string topic, string consumerName)
        {
            _consumerName = consumerName;
            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _consumer.Subscribe(topic);
        }

        public bool Consume(int retries, int timeoutMilliSeconds)
        {
            try
            {
                for (var i = 0; i < retries; i++)
                {
                    try
                    {
                        // will block until a message is available
                        // on 1.5.3 this will throw if the topic doesn't exist
                        var consumeResult = _consumer.Consume(timeoutMilliSeconds);
                        if (consumeResult is null)
                        {
                            Console.WriteLine($"{_consumerName}: Null consume result");
                            return true;
                        }

                        if (consumeResult.IsPartitionEOF)
                        {
                            Console.WriteLine($"{_consumerName}: Reached EOF");
                            return true;
                        }

						HandleMessage(consumeResult);
						return true;
					}
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"Consume Exception in manual consume: {ex}");
                    }

                    Task.Delay(500);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"{_consumerName}: Cancellation requested, exiting.");
            }

            return false;
        }

        public void Consume(CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // will block until a message is available
                    var consumeResult = _consumer.Consume(cancellationToken);

                    if (consumeResult.IsPartitionEOF)
						Console.WriteLine($"{_consumerName}: Reached EOF");
					else
						HandleMessage(consumeResult);
				}
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"{_consumerName}: Cancellation requested, exiting.");
            }
        }

        public void ConsumeWithExplicitCommit(int commitEveryXMessages, CancellationToken cancellationToken = default)
        {
            ConsumeResult<string, string> consumeResult = null;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // will block until a message is available
                    consumeResult = _consumer.Consume(cancellationToken);

                    if (consumeResult.IsPartitionEOF)
						Console.WriteLine($"{_consumerName}: Reached EOF");
					else
						HandleMessage(consumeResult);

					if (consumeResult.Offset % commitEveryXMessages == 0)
                    {
                        try
                        {
                            Console.WriteLine($"{_consumerName}: committing...");
                            _consumer.Commit(consumeResult);
                        }
                        catch (KafkaException e)
                        {
                            Console.WriteLine($"{_consumerName}: commit error: {e.Error.Reason}");
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"{_consumerName}: Cancellation requested, exiting.");
            }

            // As we're doing manual commit, make sure we force a commit now
            if (consumeResult is not null)
            {
                Console.WriteLine($"{_consumerName}: committing...");
                _consumer.Commit(consumeResult);
            }
        }

        private void HandleMessage(ConsumeResult<string, string> consumeResult)
		{
			var transaction = Agent.Tracer.CurrentTransaction;

			ISpan span = null;
			if (transaction != null)
				span = transaction.StartSpan("Consume message", "kafka");

            var kafkaMessage = consumeResult.Message;
            Console.WriteLine($"{_consumerName}: Consuming {kafkaMessage.Key}, {consumeResult.TopicPartitionOffset}");

            var headers = kafkaMessage.Headers;
            var traceParent = headers.TryGetLastBytes("elasticapmtraceparent", out var traceParentHeader)
				? Encoding.UTF8.GetString(traceParentHeader)
				: null;

			var traceState = headers.TryGetLastBytes("tracestate", out var traceStateHeader)
				? Encoding.UTF8.GetString(traceStateHeader)
				: null;

            if (traceParent is null || traceState is null)
            {
                // For kafka brokers < 0.11.0, we can't inject custom headers, so context will not be propagated
				Console.WriteLine($"Error extracting trace context for {kafkaMessage.Key}, {consumeResult.TopicPartitionOffset}");
            }
            else
				Console.WriteLine($"Successfully extracted trace context from message: {traceParent}, {traceState}");


			if (string.IsNullOrEmpty(kafkaMessage.Value))
            {
                Console.WriteLine($"Received Tombstone for {kafkaMessage.Key}");
                Interlocked.Increment(ref TotalTombstones);
            }
            else
            {
                var sampleMessage = JsonConvert.DeserializeObject<SampleMessage>(kafkaMessage.Value);
                Console.WriteLine($"Received {(sampleMessage.IsProducedAsync ? "async" : "sync")}message for {kafkaMessage.Key}");
                if (sampleMessage.IsProducedAsync)
					Interlocked.Increment(ref TotalAsyncMessages);
				else
					Interlocked.Increment(ref TotalSyncMessages);
			}

			span?.End();
        }

        public void Dispose()
        {
            Console.WriteLine($"{_consumerName}: Closing consumer");
            _consumer?.Close();
            _consumer?.Dispose();
        }

        public static Consumer Create(ClientConfig clientConfig, bool enableAutoCommit, string topic, string consumerName)
        {
            Console.WriteLine($"Creating consumer '{consumerName}' and subscribing to topic {topic}");
			var config = new ConsumerConfig(clientConfig)
			{
				GroupId = "KafkaSample",
				AutoOffsetReset = AutoOffsetReset.Earliest,
				EnableAutoCommit = enableAutoCommit,
			};
            return new Consumer(config, topic, consumerName);
        }
    }
}
