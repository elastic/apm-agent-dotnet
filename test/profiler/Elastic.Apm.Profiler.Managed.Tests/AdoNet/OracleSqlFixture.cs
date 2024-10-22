// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities;
using Testcontainers.Oracle;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("Oracle")]
	public sealed class OracleCollection : ICollectionFixture<OracleSqlFixture> { }

	public sealed class OracleSqlFixture : IAsyncLifetime
	{
		private readonly OracleContainer _container;
		private readonly IMessageSink _sink;

		public string ConnectionString => _container.GetConnectionString();

		public OracleSqlFixture(IMessageSink sink)
		{
			_sink = sink;
			if (!TestEnvironment.IsWindows)
			{
				_sink.OnMessage(new DiagnosticMessage($"Skipping {nameof(OracleSqlFixture)} on non windows platforms."));
				return;
			}
			_container = new OracleBuilder().Build();
		}

		public async Task InitializeAsync()
		{
			if (!TestEnvironment.IsWindows)
				return;

			await _container.StartAsync();
			var (stdOut, stdErr) = await _container.GetLogsAsync();

			_sink.OnMessage(new DiagnosticMessage(stdOut));
			_sink.OnMessage(new DiagnosticMessage(stdErr));

		}

		public async Task DisposeAsync()
		{
			if (!TestEnvironment.IsWindows)
				return;

			var cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromMinutes(2));

			try
			{
				_sink.OnMessage(new DiagnosticMessage($"Stopping {nameof(OracleSqlFixture)}"));
				await _container.StopAsync(cts.Token);
			}
			catch (Exception e)
			{
				_sink.OnMessage(new DiagnosticMessage(e.Message));
			}

			_sink.OnMessage(new DiagnosticMessage($"Disposing {nameof(OracleSqlFixture)}"));
			await _container.DisposeAsync();
		}
	}
}
