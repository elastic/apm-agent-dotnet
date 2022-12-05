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
	public ElasticsearchTestcontainer Container { get; }
	public ElasticsearchClient? Cleint { get; private set; }

	private readonly TestcontainerDatabaseConfiguration _configuration = new ElasticsearchTestcontainerConfiguration { Password = "secret" };

	public ElasticSearchTestFixture() =>
		Container = new TestcontainersBuilder<ElasticsearchTestcontainer>()
			.WithDatabase(_configuration)
			.Build();

	public async Task InitializeAsync()
	{
		await Container.StartAsync();


		var settings = new ElasticsearchClientSettings(new Uri(Container.ConnectionString))
			.ServerCertificateValidationCallback(CertificateValidations.AllowAll)
			.Authentication(new BasicAuthentication(Container.Username, Container.Password));

		Cleint = new ElasticsearchClient(settings);
		if (Cleint == null)
			throw new Exception("`new ElasticsearchClient(settings)` returned `null`");
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