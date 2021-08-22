// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Client;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Abstractions;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Containers.WaitStrategies;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public static class TestcontainersBuilderExtensions
	{
		public static ITestcontainersBuilder<ElasticsearchTestContainer> WithElasticsearch(
			this ITestcontainersBuilder<ElasticsearchTestContainer> builder,
			ElasticsearchTestContainerConfiguration configuration
		)
		{
			builder = configuration.Environments.Aggregate(builder, (current, environment) =>
				current.WithEnvironment(environment.Key, environment.Value));

			return builder
				.WithImage(configuration.Image)
				.WithPortBinding(configuration.Port, configuration.DefaultPort)
				.WithWaitStrategy(configuration.WaitStrategy)
				.ConfigureContainer(container =>
				{
					container.Port = configuration.DefaultPort;
				});
		}
	}

	public class ElasticsearchTestContainerConfiguration : HostedServiceConfiguration
	{
		private const int ElasticsearchDefaultPort = 9200;
		private const string ElasticsearchImageVersion = "7.12.1";

		public ElasticsearchTestContainerConfiguration()
			: this($"docker.elastic.co/elasticsearch/elasticsearch:{ElasticsearchImageVersion}") { }

		public ElasticsearchTestContainerConfiguration(string image) : base(image, ElasticsearchDefaultPort)
		{
			Environments["discovery.type"] = "single-node";
			WaitStrategy = Wait.UntilBashCommandsAreCompleted("curl -s -k http://localhost:9200/_cluster/health | grep -vq '\"status\":\"\\(^red\\)\"'");
		}

		public override IWaitUntil WaitStrategy { get; }
	}
}
