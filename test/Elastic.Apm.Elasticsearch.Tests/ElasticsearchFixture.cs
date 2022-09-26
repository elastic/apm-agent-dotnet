// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Xunit;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public class ElasticsearchFixture : IAsyncDisposable, IAsyncLifetime
	{
		private readonly ElasticsearchTestContainer _container;

		public ElasticsearchFixture()
		{
			var containerBuilder = new TestcontainersBuilder<ElasticsearchTestContainer>()
				.WithElasticsearch(new ElasticsearchTestContainerConfiguration());

			_container = containerBuilder.Build();
		}

		public string ConnectionString { get; private set; }

		public async Task InitializeAsync()
		{
			await _container.StartAsync();
			ConnectionString = _container.ConnectionString;
		}

		public async Task DisposeAsync()
		{
			await _container.StopAsync();
			try
			{
				await _container.DisposeAsync();
			}
			catch
			{
				//ignore
			}
		}

		ValueTask IAsyncDisposable.DisposeAsync()
		{
			if (_container != null)
				return _container.DisposeAsync();

			return default;
		}
	}
}
