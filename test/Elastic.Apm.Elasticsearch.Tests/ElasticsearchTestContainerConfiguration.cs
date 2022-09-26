// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public static class TestContainersBuilderExtensions
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
				.WithHostname("localhost")
				.WithPortBinding(configuration.Port, configuration.DefaultPort)
				.WithWaitStrategy(configuration.WaitStrategy)
				.WithExposedPort(configuration.DefaultPort);
		}
	}

	public sealed class ElasticsearchTestContainerConfiguration : TestcontainerDatabaseConfiguration
	{
		private const int ElasticsearchDefaultPort = 9200;
		private const string ElasticsearchImageVersion = "7.12.1";

		public ElasticsearchTestContainerConfiguration()
			: this($"docker.elastic.co/elasticsearch/elasticsearch:{ElasticsearchImageVersion}") { }

		public ElasticsearchTestContainerConfiguration(string image) : base(image, ElasticsearchDefaultPort, ElasticsearchDefaultPort)
		{
			Environments.Add("discovery.type", "single-node");
			WaitStrategy.UntilCommandIsCompleted("curl -s -k http://localhost:9200/_cluster/health | grep -vq '\"status\":\"\\(^red\\)\"'");
		}
	}
}
