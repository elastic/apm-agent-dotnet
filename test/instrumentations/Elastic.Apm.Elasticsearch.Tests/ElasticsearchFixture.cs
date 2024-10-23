// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.Elasticsearch;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Elasticsearch.Tests
{
	public sealed class ElasticsearchFixture(IMessageSink sink) : IAsyncLifetime
	{
		private readonly ElasticsearchContainer _container = new ElasticsearchBuilder().Build();

	public string ConnectionString => _container.GetConnectionString();

	public async Task InitializeAsync()
	{
		await _container.StartAsync();

		var (stdOut, stdErr) = await _container.GetLogsAsync();

		sink.OnMessage(new DiagnosticMessage(stdOut));
		sink.OnMessage(new DiagnosticMessage(stdErr));
	}

	public async Task DisposeAsync()
	{
		var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromMinutes(2));

		try
		{
			sink.OnMessage(new DiagnosticMessage($"Stopping {nameof(ElasticsearchFixture)}"));
			await _container.StopAsync(cts.Token);
		}
		catch (Exception e)
		{
			sink.OnMessage(new DiagnosticMessage(e.Message));
		}

		sink.OnMessage(new DiagnosticMessage($"Disposing {nameof(ElasticsearchFixture)}"));
		await _container.DisposeAsync();
	}
}
}
