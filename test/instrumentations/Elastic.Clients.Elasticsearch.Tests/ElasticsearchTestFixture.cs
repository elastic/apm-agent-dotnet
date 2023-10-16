// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Testcontainers.Elasticsearch;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Elastic.Clients.Elasticsearch.Tests;

public sealed class ElasticsearchTestFixture : IAsyncLifetime
{
	private readonly IMessageSink _sink;
	public ElasticsearchContainer Container { get; }

	public ElasticsearchClient? Client { get; private set; }

	public ElasticsearchTestFixture(IMessageSink sink)
	{
		_sink = sink;
		Container = new ElasticsearchBuilder().Build();
	}


	public async Task InitializeAsync()
	{
		await Container.StartAsync();

		var (stdOut, stdErr) = await Container.GetLogsAsync();

		_sink.OnMessage(new DiagnosticMessage(stdOut));
		_sink.OnMessage(new DiagnosticMessage(stdErr));

		var settings = new ElasticsearchClientSettings(new Uri(Container.GetConnectionString()));
		settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

		Client = new ElasticsearchClient(settings);
		if (Client == null)
			throw new Exception("`new ElasticsearchClient(settings)` returned `null`");
	}

	async Task IAsyncLifetime.DisposeAsync()
	{
		if (Container.State == TestcontainersStates.Running)
		{
			await Container.StopAsync();
			await Container.DisposeAsync();
		}
	}

	private class MessageSinkLogger : ILogger
	{
		private readonly IMessageSink _messageSink;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public MessageSinkLogger(IMessageSink sink)
		{
			_messageSink = sink;
			_messageSink.OnMessage(new DiagnosticMessage($"Started {nameof(MessageSinkLogger)}"));
		}

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
			_messageSink.OnMessage(new DiagnosticMessage(formatter(state, exception)));
	}
}
