using System.Collections.Generic;

namespace TestEnvironment.Docker.Containers.Kafka
{
    public static class DockerEnvironmentBuilderExtensions
    {
        public static IDockerEnvironmentBuilder AddKafkaContainer(this IDockerEnvironmentBuilder builder, string name, string imageName = "johnnypark/kafka-zookeeper", string tag = "latest", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool reuseContainer = false) =>
            builder.AddDependency(new KafkaContainer(builder.DockerClient, name.GetContainerName(builder.EnvironmentName), imageName, tag, environmentVariables, ports, builder.IsDockerInDocker, reuseContainer, builder.Logger));
    }
}