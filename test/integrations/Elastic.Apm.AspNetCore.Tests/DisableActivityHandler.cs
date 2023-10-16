// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Tests;

public class DisableActivityHandler : DelegatingHandler
{
	private readonly ITestOutputHelper _output;

	public DisableActivityHandler(ITestOutputHelper output)
		: base(new SocketsHttpHandler { ActivityHeadersPropagator = DistributedContextPropagator.CreateNoOutputPropagator() }) =>
		_output = output;

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		_output.WriteLine("Outgoing HTTP call to:" + request.RequestUri.ToString());
		return await base.SendAsync(request, cancellationToken);
	}
}
