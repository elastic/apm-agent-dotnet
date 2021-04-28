using System.Collections.Generic;

namespace TestEnvironment.Docker.Containers.Elasticsearch
{
    public static class DockerEnvironmentBuilderExtensions
    {
        public static IDockerEnvironmentBuilder AddElasticsearchContainer(this IDockerEnvironmentBuilder builder, string name, string imageName = "docker.elastic.co/elasticsearch/elasticsearch-oss", string tag = "7.0.1", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool reuseContainer = false) =>
            builder.AddDependency(new ElasticsearchContainer(builder.DockerClient, name.GetContainerName(builder.EnvironmentName), imageName, tag, environmentVariables, ports, builder.IsDockerInDocker, reuseContainer, builder.Logger));
    }
}
