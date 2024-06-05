// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.SqlClient.Tests
{
	// ReSharper disable once ClassNeverInstantiated.Global - it's used as a generic parameter
	public sealed class SqlServerFixture : IAsyncLifetime
	{
		private readonly MsSqlContainer _container;
		private readonly IMessageSink _sink;

		public string ConnectionString => _container.GetConnectionString();

		public SqlServerFixture(IMessageSink sink)
		{
			_sink = sink;
			_container = new MsSqlBuilder()
				.Build();
		}

		public async Task InitializeAsync()
		{
			await _container.StartAsync();
			var (stdOut, stdErr) = await _container.GetLogsAsync();

			_sink.OnMessage(new DiagnosticMessage(stdOut));
			_sink.OnMessage(new DiagnosticMessage(stdErr));
		}

		public async Task DisposeAsync() => await _container.DisposeAsync();
	}
}
