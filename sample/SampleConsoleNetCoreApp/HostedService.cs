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

namespace SampleConsoleNetCoreApp
{
	public class HostedService : IHostedService
	{
		private readonly IApmAgent _apmAgent;

		public HostedService(IApmAgent apmAgent) => _apmAgent = apmAgent;

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var response = await _apmAgent.Tracer.CaptureTransaction("Console .Net Core Example", "background", async () =>
			{
				// Make sure Agent.Tracer.CurrentTransaction is not null
				var currentTransaction = Agent.Tracer.CurrentTransaction;
				if (currentTransaction == null) throw new Exception("Agent.Tracer.CurrentTransaction returns null");

				var httpClient = new HttpClient();
				return await httpClient.GetAsync("https://elastic.co", cancellationToken);
			});
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
