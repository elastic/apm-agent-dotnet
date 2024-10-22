// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.Kafka;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Profiler.Managed.Tests.Kafka
{
	[CollectionDefinition("Kafka")]
	public sealed class KafkaCollection : ICollectionFixture<KafkaFixture> { }

	public sealed class KafkaFixture(IMessageSink sink) : IAsyncLifetime
	{
		private readonly KafkaContainer _container = new KafkaBuilder().Build();

		public string BootstrapServers => _container.GetBootstrapAddress();

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
				sink.OnMessage(new DiagnosticMessage($"Stopping {nameof(KafkaFixture)}"));
				await _container.StopAsync(cts.Token);
			}
			catch (Exception e)
			{
				sink.OnMessage(new DiagnosticMessage(e.Message));
			}

			sink.OnMessage(new DiagnosticMessage($"Disposing {nameof(KafkaFixture)}"));
			await _container.DisposeAsync();
		}
	}
}
