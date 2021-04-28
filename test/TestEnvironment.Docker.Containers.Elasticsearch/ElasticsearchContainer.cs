using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using IP = System.Net.IPAddress;

namespace TestEnvironment.Docker.Containers.Elasticsearch
{
    public class ElasticsearchContainer : Container
    {
        public ElasticsearchContainer(DockerClient dockerClient, string name, string imageName = "docker.elastic.co/elasticsearch/elasticsearch-oss", string tag = "7.0.1", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool isDockerInDocker = false, bool reuseContainer = false, ILogger logger = null)
            : base(
                  dockerClient,
                  name,
                  imageName,
                  tag,
                  new Dictionary<string, string> { ["discovery.type"] = "single-node" }.MergeDictionaries(environmentVariables),
                  ports,
                  isDockerInDocker,
                  reuseContainer,
                  new ElasticsearchContainerWaiter(logger),
                  new ElasticsearchContainerCleaner(),
                  logger)
        {
        }

        public string GetUrl() => IsDockerInDocker ? $"http://{IPAddress}:9200" : $"http://{IP.Loopback}:{Ports[9200]}";
    }
}
