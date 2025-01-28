// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Utilities;

internal class TestHostNameDetector : IHostNameDetector
{
	private readonly string _hostName;

	public TestHostNameDetector(IConfiguration configuration) =>
		_hostName = configuration?.HostName ?? "MY_COMPUTER";

	public TestHostNameDetector(string detectedHostName) =>
		_hostName = detectedHostName;

	public string GetDetectedHostName(IApmLogger logger) => _hostName;
}
