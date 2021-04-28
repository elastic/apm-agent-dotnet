using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace TestEnvironment.Docker.Containers.Kafka
{
    public class KafkaContainerCleaner : IContainerCleaner<KafkaContainer>
    {
        public async Task Cleanup(KafkaContainer container, CancellationToken token = default)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = container.GetUrl()
                })
                .Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(1));
            if (!metadata.Topics.Any())
            {
                return;
            }

            var topicNames = metadata.Topics.Select(t => t.Topic).ToArray();
            await adminClient.DeleteTopicsAsync(topicNames);
        }

        public Task Cleanup(TestEnvironment.Docker.Container container, CancellationToken token = default) => Cleanup((KafkaContainer)container, token);
    }
}