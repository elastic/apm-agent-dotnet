// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Testcontainers.Kafka;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.Kafka
{
	[CollectionDefinition("Kafka")]
	public sealed class KafkaCollection : ICollectionFixture<KafkaFixture> { }

	public sealed class KafkaFixture : IAsyncLifetime
	{
		private readonly KafkaContainer _container = new KafkaBuilder().Build();

		public string BootstrapServers => _container.GetBootstrapAddress();

		public Task InitializeAsync()
		{
			return _container.StartAsync();
		}

		public Task DisposeAsync()
		{
			return _container.DisposeAsync().AsTask();
		}
	}
}
