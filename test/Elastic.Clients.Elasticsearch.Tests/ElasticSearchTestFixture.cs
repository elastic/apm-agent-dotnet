// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Elastic.Transport;
using Xunit;

namespace Elastic.Clients.Elasticsearch.Tests;

public class ElasticSearchTestFixture : IAsyncDisposable, IAsyncLifetime
{
	// private readonly ElasticsearchTestcontainer _container;

	public ElasticsearchTestcontainer Container { get; }
	public ElasticsearchClient? Cleint { get; private set; }

	private readonly TestcontainerDatabaseConfiguration configuration = new ElasticsearchTestcontainerConfiguration { Password = "secret" };

	public ElasticSearchTestFixture() =>
		Container = new TestcontainersBuilder<ElasticsearchTestcontainer>()
			.WithDatabase(configuration)
			.Build();

	public async Task InitializeAsync()
	{
		await Container.StartAsync();


		var settings = new ElasticsearchClientSettings(new Uri(Container.ConnectionString))
			// .DefaultIndex("default-index")
			// .DefaultMappingFor<ElasticSearchTests.Person>(m => m
			// 	.DisableIdInference()
			// 	.IndexName("people")
			// 	.IdProperty(id => id.SecondaryId)
			// 	.RoutingProperty(id => id.SecondaryId)
			// 	.RelationName("relation"))
			// //.DefaultFieldNameInferrer(s => $"{s}_2")
			.ServerCertificateValidationCallback(CertificateValidations.AllowAll)
			.Authentication(new BasicAuthentication(Container.Username, Container.Password));

		Cleint = new ElasticsearchClient(settings);
	}

	async Task IAsyncLifetime.DisposeAsync()
	{
		await Container.StopAsync();
		await Container.DisposeAsync();
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		await Container.StopAsync();
		await Container.DisposeAsync();
	}
}
