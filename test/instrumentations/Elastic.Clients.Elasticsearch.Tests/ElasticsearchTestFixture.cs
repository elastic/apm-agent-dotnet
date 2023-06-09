// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Testcontainers.Containers;
using Elastic.Transport;
using Testcontainers.Elasticsearch;
using Xunit;

namespace Elastic.Clients.Elasticsearch.Tests;

public sealed class ElasticsearchTestFixture : IAsyncLifetime
{
	public ElasticsearchContainer Container { get; } = new ElasticsearchBuilder().Build();

	public ElasticsearchClient? Client { get; private set; }

	public async Task InitializeAsync()
	{
		await Container.StartAsync();

		var settings = new ElasticsearchClientSettings(new Uri(Container.GetConnectionString()));
		settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

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
}
