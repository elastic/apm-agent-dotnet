using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using IP = System.Net.IPAddress;

namespace TestEnvironment.Docker.Containers.Kafka
{
    public class KafkaContainer : TestEnvironment.Docker.Container
    {
        public KafkaContainer(DockerClient dockerClient, string name, string imageName = "johnnypark/kafka-zookeeper", string tag = "latest", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool isDockerInDocker = false, bool reuseContainer = false, ILogger logger = null)
            : base(
                dockerClient,
                name,
                imageName,
                tag,
                new Dictionary<string, string>
                {
                    ["ADVERTISED_HOST"] = "localhost",
                }.MergeDictionaries(environmentVariables),
                new Dictionary<ushort, ushort>
                {
                    [9092] = 9092,
                    [2181] = 2181,
                }.MergeDictionaries(ports),
                isDockerInDocker,
                reuseContainer,
                new KafkaContainerWaiter(logger),
                new KafkaContainerCleaner(),
                logger)
        {
        }

        public string GetUrl() => IsDockerInDocker ? $"{IPAddress}:9092" : $"{IP.Loopback}:{Ports[9092]}";
    }
}