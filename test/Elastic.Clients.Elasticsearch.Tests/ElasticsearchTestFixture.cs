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

public class ElasticsearchTestFixture : IAsyncDisposable, IAsyncLifetime
{
	public ElasticsearchTestcontainer Container { get; }
	public ElasticsearchClient? Client { get; private set; }

	private readonly TestcontainerDatabaseConfiguration _configuration = new ElasticsearchTestcontainerConfiguration { Password = "secret" };

	public ElasticsearchTestFixture() =>
		Container = new TestcontainersBuilder<ElasticsearchTestcontainer>()
			.WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.5")
			.WithDatabase(_configuration)
			.Build();

	public async Task InitializeAsync()
	{
		await Container.StartAsync();

		var settings = new ElasticsearchClientSettings(new Uri(Container.ConnectionString))
			.ServerCertificateValidationCallback(CertificateValidations.AllowAll)
			.Authentication(new BasicAuthentication(Container.Username, Container.Password));

		Client = new ElasticsearchClient(settings);
		if (Client == null)
			throw new Exception("`new ElasticsearchClient(settings)` returned `null`");
	}

	async Task IAsyncLifetime.DisposeAsync()
	{
		if (Container.State == TestcontainersStates.Running)
		{
			await Container.StopAsync();
			await Container.DisposeAsync();
		}
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if (Container.State == TestcontainersStates.Running)
		{
			await Container.StopAsync();
			await Container.DisposeAsync();
		}
	}
}
