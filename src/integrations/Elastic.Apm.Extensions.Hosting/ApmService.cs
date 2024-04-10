// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Elastic.Apm.NetCoreAll;

/// <summary>
/// When registered into the DI container, this ensures that an instance of <see cref="IApmAgent"/> is
/// created by invoking the implementation factory.
/// </summary>
internal sealed class ApmService : IHostedService
{
#pragma warning disable IDE0052 // Remove unread private members
	private readonly IApmAgent _agent;
#pragma warning restore IDE0052 // Remove unread private members

	public ApmService(IApmAgent agent) => _agent = agent;

	public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
