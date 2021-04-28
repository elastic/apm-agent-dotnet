using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker.Containers.Kafka
{
    public class KafkaContainerWaiter : BaseContainerWaiter<KafkaContainer>
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        public KafkaContainerWaiter(ILogger logger = null)
            : base(logger)
        {
        }

        protected override async Task<bool> PerformCheck(KafkaContainer container, CancellationToken cancellationToken)
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = container.GetUrl(),
                })
                .Build();
            var topicName = $"{container.Name}_topic_health";
            var topics = adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(10)).Topics.Select(t => t.Topic).ToArray();
            if (!topics.Contains(topicName))
            {
                await adminClient.CreateTopicsAsync(new[]
                {
                    new TopicSpecification()
                    {
                        NumPartitions = 1,
                        Name = topicName,
                        ReplicationFactor = 1
                    },
                });
            }

            await ProduceMessage(topicName, container, cancellationToken);
            return true;
        }

        private async Task ProduceMessage(string topicName, KafkaContainer container, CancellationToken cancellationToken)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = container.GetUrl()
            };
            using var p = new ProducerBuilder<string, string>(producerConfig).Build();
            await p.ProduceAsync(topicName, new Message<string, string> { Value = "test-message", Key = null }, cancellationToken);
        }
    }
}