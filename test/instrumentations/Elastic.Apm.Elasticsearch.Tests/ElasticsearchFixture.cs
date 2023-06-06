// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Testcontainers.Elasticsearch;
using Xunit;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public sealed class ElasticsearchFixture : IAsyncLifetime
	{
		private readonly ElasticsearchContainer _container = new ElasticsearchBuilder().Build();

		public string ConnectionString => _container.GetConnectionString();

		public Task InitializeAsync() => _container.StartAsync();

		public Task DisposeAsync() => _container.DisposeAsync().AsTask();
	}
}
