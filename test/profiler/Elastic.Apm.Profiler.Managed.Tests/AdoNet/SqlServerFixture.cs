// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("SqlServer")]
	public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }

	public sealed class SqlServerFixture : IAsyncLifetime
	{
		private readonly IMessageSink _sink;
		private readonly MsSqlContainer _container;

		public SqlServerFixture(IMessageSink sink)
		{
			_sink = sink;
			// see: https://blog.rufer.be/2024/09/22/workaround-fix-testcontainers-sql-error-docker-dotnet-dockerapiexception-docker-api-responded-with-status-codeconflict/
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				_container = new MsSqlBuilder()
					.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
					.Build();
			}
			else
			{
				_container = new MsSqlBuilder()
					.Build();
			}
		}

		public string ConnectionString => _container.GetConnectionString();

		public async Task InitializeAsync()
		{
			await _container.StartAsync();

			var (stdOut, stdErr) = await _container.GetLogsAsync();

			_sink.OnMessage(new DiagnosticMessage(stdOut));
			_sink.OnMessage(new DiagnosticMessage(stdErr));
		}

		public async Task DisposeAsync()
		{
			var cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromMinutes(2));

			try
			{
				_sink.OnMessage(new DiagnosticMessage($"Stopping {nameof(SqlServerFixture)}"));
				await _container.StopAsync(cts.Token);
			}
			catch (Exception e)
			{
				_sink.OnMessage(new DiagnosticMessage(e.Message));
			}

			_sink.OnMessage(new DiagnosticMessage($"Disposing {nameof(SqlServerFixture)}"));
			await _container.DisposeAsync();
		}
	}
}
