// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Elastic.Apm.Tests.Utilities;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.RabbitMq
{
	[CollectionDefinition("RabbitMq")]
	public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
	{
	}

	public class RabbitMqFixture : IAsyncLifetime
	{
		private readonly RabbitMqTestcontainer _builder;

		public RabbitMqFixture() =>
			_builder = new TestcontainersBuilder<RabbitMqTestcontainer>()
				.WithMessageBroker(new RabbitMqTestcontainerConfiguration { Username = "rabbitmq", Password = "rabbitmq" })
				.Build();


		public async Task InitializeAsync()
		{
			await _builder.StartAsync();

			ConnectionString = _builder.ConnectionString;
		}

		public string ConnectionString { get; private set; }

		public async Task DisposeAsync() => await _builder.DisposeAsync();
	}
}
