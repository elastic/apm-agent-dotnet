// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleConsoleNetCoreApp
{
	public class HostedService : IHostedService
	{
		private readonly IApmAgent _apmAgent;
		private readonly ILogger _logger;

		public HostedService(IApmAgent apmAgent, ILogger<HostedService> logger) => (_apmAgent, _logger) = (apmAgent,logger);

		public async Task StartAsync(CancellationToken cancellationToken) =>
			await _apmAgent.Tracer.CaptureTransaction("Console .Net Core Example", "background", async () =>
			{
				Console.WriteLine("HostedService running");

				_logger.LogError("This is a sample error log message, with a sample value: {intParam}", 42 );

				// Make sure Agent.Tracer.CurrentTransaction is not null
				var currentTransaction = Agent.Tracer.CurrentTransaction;
				if (currentTransaction == null) throw new Exception("Agent.Tracer.CurrentTransaction returns null");

				var httpClient = new HttpClient();
				return await httpClient.GetAsync("https://elastic.co", cancellationToken);
			});

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
