// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Elastic.Apm.SqlClient.Tests
{
	// ReSharper disable once ClassNeverInstantiated.Global - it's used as a generic parameter
	public class SqlServerFixture : IAsyncDisposable, IAsyncLifetime
	{
		private readonly MsSqlTestcontainer _container;

		public SqlServerFixture()
		{
			var containerBuilder = new TestcontainersBuilder<MsSqlTestcontainer>()
				.WithDatabase(new MsSqlTestcontainerConfiguration
				{
					Password = "StrongPassword(!)!!!1"
				});

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
			await _container.DisposeAsync();
		}

#if !NET5_0_OR_GREATER
		private readonly ValueTask _completedValueTask = default;
#endif
		ValueTask IAsyncDisposable.DisposeAsync()
		{
			if (_container != null)
				return _container.DisposeAsync();
#if NET5_0_OR_GREATER
			return ValueTask.CompletedTask;
#else
			return _completedValueTask;
#endif
		}
	}
}
