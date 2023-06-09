// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Testcontainers.RabbitMq;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.RabbitMq
{
	[CollectionDefinition("RabbitMq")]
	public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture> { }

	public sealed class RabbitMqFixture : IAsyncLifetime
	{
		private readonly RabbitMqContainer _container = new RabbitMqBuilder().Build();

		public string ConnectionString => _container.GetConnectionString();

		public Task InitializeAsync() => _container.StartAsync();

		public Task DisposeAsync() => _container.DisposeAsync().AsTask();
	}
}
